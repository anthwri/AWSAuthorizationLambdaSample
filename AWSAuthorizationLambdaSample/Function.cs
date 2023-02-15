using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSAuthorizationLambdaSample;

public class Function
{
    public class SecurityConstants
    {
        public const string Issuer = "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_hom5BedaB";
    }

    public class WildRydesScopes
    {
        public const string CustomizeUnicorns = "WildRydes/CustomizeUnicorn";
        public const string PartnerAdmin = "WildRydes/ManagePartners";
    }

    private static List<PemKey>? m_pems;

    public Function()
    {
        //Like the original javascript, this caches the pems keys locally
        if (m_pems == null)
        {
            using HttpClient client = new();
            var jsonString = client.GetStringAsync("https://cognito-idp.us-east-1.amazonaws.com/us-east-1_hom5BedaB/.well-known/jwks.json").Result;
            var jwksObject = JsonConvert.DeserializeObject<JwksObject>(jsonString);
            if (jwksObject == null)
            {
                throw new UnauthorizedAccessException();
            }
            LambdaLogger.Log("\nJwksObject: " + JsonConvert.SerializeObject(jwksObject) + "\n");
            m_pems = jwksObject.keys;
        }
    }

    public async Task<APIGatewayCustomAuthorizerResponse> ValidateToken(APIGatewayCustomAuthorizerRequest apigAuthRequest, ILambdaContext context)
    {
        LambdaLogger.Log("\nEVENT: " + JsonConvert.SerializeObject(apigAuthRequest) + "\n");
        LambdaLogger.Log("\nCONTEXT: " + JsonConvert.SerializeObject(context) + "\n");

        var authToken = apigAuthRequest.AuthorizationToken;
        if (string.IsNullOrWhiteSpace(authToken))
        {
            throw new UnauthorizedAccessException();
        }
        if (authToken.StartsWith("Bearer ", StringComparison.InvariantCultureIgnoreCase))
        {
            authToken = authToken.Substring(7);
        }
        LambdaLogger.Log("\nauthToken:" + authToken + "\n");

        var handler = new JwtSecurityTokenHandler();

        var jsonToken = handler.ReadJwtToken(authToken);
        if (jsonToken == null)
        {
            throw new UnauthorizedAccessException("Null token");
        }
        LambdaLogger.Log("\nJsonToken: " + JsonConvert.SerializeObject(jsonToken) + "\n");

        var kid = (string)jsonToken.Header["kid"];
        LambdaLogger.Log("\nkid: " + kid + "\n");

        //Get the pems key that matches the key id in the token         
        var cognitoPublicKey = m_pems?.FirstOrDefault(k => k.kid == kid);
        if (cognitoPublicKey == null)
        {
            throw new UnauthorizedAccessException("No public key");
        }

        var tokenValidationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = SecurityConstants.Issuer,
            ValidateAudience = false, //validating audience doesn't work for some unknown reason
            ValidAudience = SecurityConstants.Issuer,
            IssuerSigningKey = new RsaSecurityKey(new RSAParameters()
            {
                //RSA only requires the modulus and exponent from the public key to decrypt the token
                Modulus = Base64UrlEncoder.DecodeBytes(cognitoPublicKey.n),
                Exponent = Base64UrlEncoder.DecodeBytes(cognitoPublicKey.e)
            }),
            ClockSkew = TimeSpan.FromMinutes(5),
            ValidateIssuerSigningKey = true
        };

        LambdaLogger.Log("tokenValidationParams:\n" + JsonConvert.SerializeObject(tokenValidationParams));

        var hasPartnerScope = false;
        var hasCustomizeUnicornsScope = false;
        var clientId = string.Empty;

        try
        {
            var tokenValidationResult = await handler.ValidateTokenAsync(authToken, tokenValidationParams);
            if (!tokenValidationResult.IsValid)
            {
                throw new UnauthorizedAccessException("Invalid token");
            }

            var scope = (string?)tokenValidationResult?.Claims["scope"];
            if (scope != null)
            {
                hasPartnerScope = scope.Contains(WildRydesScopes.PartnerAdmin);
                hasCustomizeUnicornsScope = scope.Contains(WildRydesScopes.CustomizeUnicorns);
            }

            clientId = (string?)tokenValidationResult?.Claims["client_id"];
            LambdaLogger.Log($"Client_id:{clientId}");
            //if (clientId == null)
            //{
            //    throw new UnauthorizedAccessException();
            //}
        }
        catch (Exception ex)
        {
            LambdaLogger.Log($"Error occurred validating token: {ex.Message}");
            throw new UnauthorizedAccessException();
        }

        var policy = new APIGatewayCustomAuthorizerPolicy
        {
            Version = "2012-10-17",
            Statement = new List<APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement>()
        };
        var contextOutput = new APIGatewayCustomAuthorizerContextOutput();

        // string MethodArn = "arn:aws:execute-api:us-east-1:123456789012:example/prod/POST/{proxy+}";

        var resourceRoot = GetResourceRoot(apigAuthRequest.MethodArn);

        var policyStatement = new APIGatewayCustomAuthorizerPolicy.IAMPolicyStatement
        {
            Action = new HashSet<string>(new string[] { "execute-api:Invoke" }),
            Effect = "Allow",
            Resource = new HashSet<string>()
        };

        // Start Policy Statements

        // 1. Any authenticated clients can list customisation options
        policyStatement.Resource.Add(resourceRoot + "/GET/horns");
        policyStatement.Resource.Add(resourceRoot + "/GET/socks");
        policyStatement.Resource.Add(resourceRoot + "/GET/glasses");
        policyStatement.Resource.Add(resourceRoot + "/GET/capes");

        // 2. When the scope matches the Partner Admin scope, then allow partner methods
        if (hasPartnerScope == true)
        {
            policyStatement.Resource.Add(resourceRoot + "/GET/partner*");
            policyStatement.Resource.Add(resourceRoot + "/POST/partner*");
            policyStatement.Resource.Add(resourceRoot + "/DELETE/partner*");
        }

        // 3. When the scope matches the unicorn customisations scope, retrieve the company id from the dynamo database
        //    otherwise it's not authorised
        if (hasCustomizeUnicornsScope == true)
        {
            policyStatement.Resource.Add(resourceRoot + "/GET/customizations*");
            policyStatement.Resource.Add(resourceRoot + "/POST/customizations*");
            policyStatement.Resource.Add(resourceRoot + "/DELETE/customizations*");

            // this is for the right to add customisations to the database.
            // a company can only add customisations to their own set of unicorns
            if (clientId != null)
            {
                var companyId = await GetCompanyIdForClientAsync(clientId);
                contextOutput["CompanyID"] = companyId;
            }
        }

        policy.Statement.Add(policyStatement);

        // End Policy Statements

        LambdaLogger.Log("Success");

        return new APIGatewayCustomAuthorizerResponse
        {
            PolicyDocument = policy,
            Context = contextOutput,
            UsageIdentifierKey = clientId
        };

    }

    public async Task<int?> GetCompanyIdForClientAsync(string clientId)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dbContext = new DynamoDBContext(client);
        var result = await dbContext.LoadAsync<CustomizeUnicorns_WildRydePartners>(clientId);
        if (result != null)
        {
            return result.CompanyID;
        }
        return null;
    }

    private string GetResourceRoot(string methodArn)
    {
        var tmp = methodArn.Split(':');
        var apiGatewayArnTmp = tmp[5].Split('/');
        return $"{tmp[0]}:{tmp[1]}:{tmp[2]}:{tmp[3]}:{tmp[4]}:{apiGatewayArnTmp[0]}/{apiGatewayArnTmp[1]}";
    }

}
