using Amazon;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Data;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambdaCustomizeUnicorns;

public class Function
{

    public async Task<APIGatewayProxyResponse> ListCustomUnicorns(APIGatewayProxyRequest apigAuthRequest, ILambdaContext context)
    {
        LambdaLogger.Log("\nEVENT: " + JsonConvert.SerializeObject(apigAuthRequest) + "\n");
        LambdaLogger.Log("\nCONTEXT: " + JsonConvert.SerializeObject(context) + "\n");

        var customUnicorns = new List<Unicorn>();

        var connectionDetails = await GetSecretAsync("secure-serverless-db-secret", "us-east-1");
        LambdaLogger.Log("Secret:" + connectionDetails);
        if (connectionDetails == null)
        {
            return Unauthorised("Secret unable to be retrieved");
        }
        var dbUser = JsonConvert.DeserializeObject<DatabaseUser>(connectionDetails);
        if (dbUser == null)
        {
            return Unauthorised("No database connection details");
        }
        string connectionString;// = string.Format("Server={0};Database={2};Uid={3};Pwd={4};",dbUser.host, dbUser.port, "unicorn_customization",dbUser.username,dbUser.password);

        var cb = new MySqlConnectionStringBuilder();
        cb.Server = dbUser.host;
        cb.Database = "unicorn_customization";
        cb.UserID = dbUser.username;
        cb.Password = dbUser.password;
        cb.Port = (uint)dbUser.port;
        connectionString = cb.ConnectionString;
        LambdaLogger.Log($"\nConnectionString:{connectionString}");

        int? companyId = null;
        var companyIdObject = apigAuthRequest.RequestContext.Authorizer["CompanyID"];
        if (companyIdObject != null)
        {
            var companyIdJsonString = companyIdObject.ToString();
            LambdaLogger.Log(companyIdJsonString);
            if (companyIdJsonString != null)
            {
                companyId = int.Parse(companyIdJsonString);
            }
        }

        using var cn = new MySqlConnection(connectionString);
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Custom_Unicorns";
        if (companyId != null)
        {
            cmd.CommandText += $" WHERE COMPANY={companyId}";
            LambdaLogger.Log("Query:" + cmd.CommandText);
        }

        try
        {
            await cn.OpenAsync();
            var reader = await cmd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    var item = new Unicorn
                    {
                        ID = reader.GetInt32(0),
                        NAME = reader.GetString(1),
                        COMPANY = reader.GetInt32(2),
                        IMAGEURL = reader.GetString(3),
                        SOCK = reader.GetInt32(4),
                        HORN = reader.GetInt32(5),
                        GLASSES = reader.GetInt32(6),
                        CAPE = reader.GetInt32(7)
                    };
                    customUnicorns.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            LambdaLogger.Log("\nException:" + ex.Message + "\n" + ex.StackTrace);
            if (ex.InnerException != null)
            {
                LambdaLogger.Log("\nInner Exception:" + ex.InnerException.Message + "\n" + ex.StackTrace);
            }
        }
        finally
        {
            if (cn.State != ConnectionState.Closed)
            {
                await cn.CloseAsync();
            }
        }

        var responseBody = JsonConvert.SerializeObject(customUnicorns.ToArray());
        LambdaLogger.Log("\nResponse Body:" + responseBody);

        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = responseBody
        };
        return response;

    }

    public async Task<APIGatewayProxyResponse> GetCustomUnicornById(APIGatewayProxyRequest apigAuthRequest, ILambdaContext context)
    {
        LambdaLogger.Log("\nEVENT: " + JsonConvert.SerializeObject(apigAuthRequest) + "\n");
        LambdaLogger.Log("\nCONTEXT: " + JsonConvert.SerializeObject(context) + "\n");

        var customUnicornId = apigAuthRequest.PathParameters["id"];

        var connectionDetails = await GetSecretAsync("secure-serverless-db-secret", "us-east-1");
        LambdaLogger.Log("Secret:" + connectionDetails);
        if (connectionDetails == null)
        {
            return Unauthorised("Secret unable to be retrieved");
        }
        var dbUser = JsonConvert.DeserializeObject<DatabaseUser>(connectionDetails);
        if (dbUser == null)
        {
            return Unauthorised("No database connection details");
        }
        string connectionString;// = string.Format("Server={0};Database={2};Uid={3};Pwd={4};",dbUser.host, dbUser.port, "unicorn_customization",dbUser.username,dbUser.password);

        var cb = new MySqlConnectionStringBuilder();
        cb.Server = dbUser.host;
        cb.Database = "unicorn_customization";
        cb.UserID = dbUser.username;
        cb.Password = dbUser.password;
        cb.Port = (uint)dbUser.port;
        connectionString = cb.ConnectionString;
        LambdaLogger.Log($"\nConnectionString:{connectionString}");

        int? companyId = null;
        var companyIdObject = apigAuthRequest.RequestContext.Authorizer["CompanyID"];
        if (companyIdObject != null)
        {
            var companyIdJsonString = companyIdObject.ToString();
            LambdaLogger.Log(companyIdJsonString);
            if (companyIdJsonString != null)
            {
                companyId = int.Parse(companyIdJsonString);
            }
        }

        using var cn = new MySqlConnection(connectionString);
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Custom_Unicorns";
        if (companyId != null)
        {
            cmd.CommandText += $" WHERE COMPANY={companyId} AND ID={customUnicornId}";
            LambdaLogger.Log("Query:" + cmd.CommandText);
        }

        var customUnicorns = new List<Unicorn>();
        try
        {
            await cn.OpenAsync();
            var reader = await cmd.ExecuteReaderAsync();
            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    var item = new Unicorn
                    {
                        ID = reader.GetInt32(0),
                        NAME = reader.GetString(1),
                        COMPANY = reader.GetInt32(2),
                        IMAGEURL = reader.GetString(3),
                        SOCK = reader.GetInt32(4),
                        HORN = reader.GetInt32(5),
                        GLASSES = reader.GetInt32(6),
                        CAPE = reader.GetInt32(7)
                    };
                    customUnicorns.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            LambdaLogger.Log("\nException:" + ex.Message + "\n" + ex.StackTrace);
            if (ex.InnerException != null)
            {
                LambdaLogger.Log("\nInner Exception:" + ex.InnerException.Message + "\n" + ex.StackTrace);
            }
        }
        finally
        {
            if (cn.State != ConnectionState.Closed)
            {
                await cn.CloseAsync();
            }
        }

        string? responseBody;
        var foundUnicorn = customUnicorns.FirstOrDefault();
        if (foundUnicorn != null)
        {
            responseBody = JsonConvert.SerializeObject(foundUnicorn);
        }
        else
        {
            responseBody = "{ }";
        }
        LambdaLogger.Log("\nResponse Body:" + responseBody);

        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = responseBody
        };
        return response;

    }


    public async Task<APIGatewayProxyResponse> CreateCustomUnicorn(APIGatewayProxyRequest apigAuthRequest, ILambdaContext context)
    {
        LambdaLogger.Log("\nEVENT: " + JsonConvert.SerializeObject(apigAuthRequest) + "\n");
        LambdaLogger.Log("\nCONTEXT: " + JsonConvert.SerializeObject(context) + "\n");

        var newUnicorn = JsonConvert.DeserializeObject<Unicorn>(apigAuthRequest.Body);
        if (newUnicorn==null)
        {
            return Unauthorised("No valid unicorn specified");
        }

        var connectionDetails = await GetSecretAsync("secure-serverless-db-secret", "us-east-1");
        LambdaLogger.Log("Secret:" + connectionDetails);
        if (connectionDetails == null)
        {
            return Unauthorised("Secret unable to be retrieved");
        }
        var dbUser = JsonConvert.DeserializeObject<DatabaseUser>(connectionDetails);
        if (dbUser == null)
        {
            return Unauthorised("No database connection details");
        }
        string connectionString;// = string.Format("Server={0};Database={2};Uid={3};Pwd={4};",dbUser.host, dbUser.port, "unicorn_customization",dbUser.username,dbUser.password);

        var cb = new MySqlConnectionStringBuilder();
        cb.Server = dbUser.host;
        cb.Database = "unicorn_customization";
        cb.UserID = dbUser.username;
        cb.Password = dbUser.password;
        cb.Port = (uint)dbUser.port;
        connectionString = cb.ConnectionString;
        LambdaLogger.Log($"\nConnectionString:{connectionString}");

        int? companyId = null;
        var companyIdObject = apigAuthRequest.RequestContext.Authorizer["CompanyID"];
        if (companyIdObject != null)
        {
            var companyIdJsonString = companyIdObject.ToString();
            LambdaLogger.Log(companyIdJsonString);
            if (companyIdJsonString != null)
            {
                companyId = int.Parse(companyIdJsonString);
            }
        }

        using var cn = new MySqlConnection(connectionString);
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "INSERT INTO Custom_Unicorns (NAME, COMPANY, IMAGEURL, SOCK, HORN, GLASSES, CAPE) ";
        cmd.CommandText += $"VALUES('{newUnicorn.NAME}',{companyId},'{newUnicorn.IMAGEURL}',{newUnicorn.SOCK},{newUnicorn.HORN},{newUnicorn.GLASSES},{newUnicorn.CAPE});";
        LambdaLogger.Log("Query:" + cmd.CommandText);

        long? customUnicornId = null;
        try
        {
            await cn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
            customUnicornId = cmd.LastInsertedId;
        }
        catch (Exception ex)
        {
            LambdaLogger.Log("\nException:" + ex.Message + "\n" + ex.StackTrace);
            if (ex.InnerException != null)
            {
                LambdaLogger.Log("\nInner Exception:" + ex.InnerException.Message + "\n" + ex.StackTrace);
            }
        }
        finally
        {
            if (cn.State != ConnectionState.Closed)
            {
                await cn.CloseAsync();
            }
        }

        string responseBody;
        if (customUnicornId != null)
        {
            responseBody = "{ \"customUnicornId\": " + customUnicornId.ToString() + " }";
        }
        else
        {
            responseBody = "{}";
        }
        LambdaLogger.Log("\nResponse Body:" + responseBody);

        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = responseBody
        };
        return response;

    }


    public async Task<APIGatewayProxyResponse> DeleteCustomUnicornById(APIGatewayProxyRequest apigAuthRequest, ILambdaContext context)
    {
        LambdaLogger.Log("\nEVENT: " + JsonConvert.SerializeObject(apigAuthRequest) + "\n");
        LambdaLogger.Log("\nCONTEXT: " + JsonConvert.SerializeObject(context) + "\n");

        var customUnicornId = apigAuthRequest.PathParameters["id"];

        var connectionDetails = await GetSecretAsync("secure-serverless-db-secret", "us-east-1");
        LambdaLogger.Log("Secret:" + connectionDetails);
        if (connectionDetails == null)
        {
            return Unauthorised("Secret unable to be retrieved");
        }
        var dbUser = JsonConvert.DeserializeObject<DatabaseUser>(connectionDetails);
        if (dbUser == null)
        {
            return Unauthorised("No database connection details");
        }
        string connectionString;// = string.Format("Server={0};Database={2};Uid={3};Pwd={4};",dbUser.host, dbUser.port, "unicorn_customization",dbUser.username,dbUser.password);

        var cb = new MySqlConnectionStringBuilder();
        cb.Server = dbUser.host;
        cb.Database = "unicorn_customization";
        cb.UserID = dbUser.username;
        cb.Password = dbUser.password;
        cb.Port = (uint)dbUser.port;
        connectionString = cb.ConnectionString;
        LambdaLogger.Log($"\nConnectionString:{connectionString}");

        int? companyId = null;
        var companyIdObject = apigAuthRequest.RequestContext.Authorizer["CompanyID"];
        if (companyIdObject != null)
        {
            var companyIdJsonString = companyIdObject.ToString();
            LambdaLogger.Log(companyIdJsonString);
            if (companyIdJsonString != null)
            {
                companyId = int.Parse(companyIdJsonString);
            }
        }
        LambdaLogger.Log("CompanyId:" + companyId.ToString());

        using var cn = new MySqlConnection(connectionString);
        using var cmd = cn.CreateCommand();
        cmd.CommandText = "DELETE FROM Custom_Unicorns";
        if (companyId != null)
        {
            cmd.CommandText += $" WHERE COMPANY={companyId} AND ID={customUnicornId}";
            LambdaLogger.Log("Query:" + cmd.CommandText);
        }
        else
        {
            return Unauthorised("No company/id specified");
        }

        int affectedRows = 0;
        try
        {
            await cn.OpenAsync();
            affectedRows = await cmd.ExecuteNonQueryAsync();

        }
        catch (Exception ex)
        {
            LambdaLogger.Log("\nException:" + ex.Message + "\n" + ex.StackTrace);
            if (ex.InnerException != null)
            {
                LambdaLogger.Log("\nInner Exception:" + ex.InnerException.Message + "\n" + ex.StackTrace);
            }
        }
        finally
        {
            if (cn.State != ConnectionState.Closed)
            {
                await cn.CloseAsync();
            }
        }

        string? responseBody;
        if (affectedRows == 1)
        {
            responseBody = "{ \"id\": \"" + customUnicornId.ToString() + "\"}";
        }
        else
        {
            responseBody = "{ }";
        }
        LambdaLogger.Log("\nResponse Body:" + responseBody);

        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = responseBody
        };
        return response;

    }


    public async Task<APIGatewayProxyResponse> CreatePartner(APIGatewayProxyRequest apigAuthRequest, ILambdaContext context)
    {
        LambdaLogger.Log("\nEVENT: " + JsonConvert.SerializeObject(apigAuthRequest) + "\n");
        LambdaLogger.Log("\nCONTEXT: " + JsonConvert.SerializeObject(context) + "\n");

        try
        {
            //get the company name from the body
            string companyName = string.Empty;
            var company = JsonConvert.DeserializeObject<Company>(apigAuthRequest.Body);
            if (company == null)
            {
                return Unauthorised("No company name specified");
            }

            // add the partner company to RDS Aurora MySql database
            // "INSERT INTO " + PARTNER_COMPANY_TABLE + " (NAME) VALUES ('" + companyName + "');";
            // this returns the Id called insertId and that is the new CompanyId
            long? companyId = null;
            using var cn = await GetMySqlConnection("unicorn_customization");
            using var cmd = cn.CreateCommand();
            cmd.CommandText = $"INSERT INTO Companies (NAME) VALUES ('{company.name}');";
            try
            {
                await cn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                companyId = cmd.LastInsertedId;
            }
            finally
            {
                if (cn.State != ConnectionState.Closed)
                {
                    await cn.CloseAsync();
                }
            }

            if (companyId == null)
            {
                return Unauthorised("Unable to obtain company id");
            }

            LambdaLogger.Log("\nCompanyId:" + companyId);

            // That needs to be added to Cognito, so set the Cognito params and submit to create the company in the Cognito User Pool
            //
            //const createUserPoolClientParams = {
            //    ClientName: company,
            //    UserPoolId: process.env["USER_POOL_ID"],
            //    GenerateSecret: true,
            //    RefreshTokenValidity: 1,
            //    AllowedOAuthFlows: ['client_credentials'],
            //    AllowedOAuthScopes: ['WildRydes/CustomizeUnicorn'],
            //    AllowedOAuthFlowsUserPoolClient: true
            //}
            //cognito.createUserPoolClient(createUserPoolClientParams)

            var createUserPoolClientParams = new CreateUserPoolClientRequest
            {
                ClientName = company.name,
                UserPoolId = "us-east-1_hom5BedaB",
                GenerateSecret = true,
                RefreshTokenValidity = 1,
                AllowedOAuthFlows = { "client_credentials" },
                AllowedOAuthScopes = { "WildRydes/CustomizeUnicorn" },
                AllowedOAuthFlowsUserPoolClient = true
            };
            using var cognito = new Amazon.CognitoIdentityProvider.AmazonCognitoIdentityProviderClient();
            var response = await cognito.CreateUserPoolClientAsync(createUserPoolClientParams);

            //Get the params for the ClientId and ClientSecret from the Cognito User Pool
            //clientId = createUserPoolClientResponse["UserPoolClient"]["ClientId"];
            //clientSecret = createUserPoolClientResponse["UserPoolClient"]["ClientSecret"];
            var clientId = response.UserPoolClient.ClientId;
            var clientSecret = response.UserPoolClient.ClientSecret;
            LambdaLogger.Log("\nClientId:" + clientId);
            LambdaLogger.Log("\nClientSecret:" + clientSecret);

            //add the ClientID and CompanyID to DynamoDb
            //    const putItemParam =
            //    {
            //        TableName: companyDDBTable,
            //        Item:
            //        {
            //          'ClientID': clientId,
            //          'CompanyID': companyId
            //        }
            //    }
            //   ddbDocClient.put(putItemParam)

            using var client = new AmazonDynamoDBClient();
            var putItemRequest = new PutItemRequest
            {
                TableName = "CustomizeUnicorns-WildRydePartners",
                Item = new Dictionary<string, AttributeValue> 
                { 
                    { "ClientID", new AttributeValue { S = clientId } },
                    { "CompanyID", new AttributeValue { N = companyId.ToString() } },
                }
            };
            await client.PutItemAsync(putItemRequest);

            LambdaLogger.Log("\nAdded ClientID and CompanyID to DynamoDb table:" + putItemRequest.TableName);

            //finally build the body message for returning to the caller
            //let returnMessage = { "ClientID": clientId, "ClientSecret": clientSecret}
            //callback(null, httpUtil.returnOK(returnMessage))

            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Body =
                    @"{
                        ""ClientID"" = """ + clientId + @""",
                        ""ClientSecret"" = """ + clientSecret + @""",
                    }"
            };


        }
        catch (Exception ex)
        {
            return Unauthorised(ex.Message);
        }

    }

    private async Task<MySqlConnection> GetMySqlConnection(string databaseName)
    {
        var connectionDetails = await GetSecretAsync("secure-serverless-db-secret", "us-east-1");
        LambdaLogger.Log("Secret:" + connectionDetails);
        if (connectionDetails == null)
        {
            throw new Exception("Secret unable to be retrieved");
        }
        var dbUser = JsonConvert.DeserializeObject<DatabaseUser>(connectionDetails);
        if (dbUser == null)
        {
            throw new Exception("No database connection details");
        }
        string connectionString;// = string.Format("Server={0};Database={2};Uid={3};Pwd={4};",dbUser.host, dbUser.port, "unicorn_customization",dbUser.username,dbUser.password);

        var cb = new MySqlConnectionStringBuilder();
        cb.Server = dbUser.host;
        cb.Database = databaseName;
        cb.UserID = dbUser.username;
        cb.Password = dbUser.password;
        cb.Port = (uint)dbUser.port;
        connectionString = cb.ConnectionString;
        LambdaLogger.Log($"\nConnectionString:{connectionString}");
        var cn = new MySqlConnection(connectionString);
        return cn;
    }


    private APIGatewayProxyResponse Unauthorised(string message)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = 401,
            Body = @"
{
    message=""" + message + @"""
}"
        };
    }

    public async Task<string> GetSecretAsync(string secretName, string region)
    {
        var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
        var request = new GetSecretValueRequest
        {
            SecretId = secretName,
        };

        var response = await client.GetSecretValueAsync(request);
        if (response.SecretString == null)
        {
            throw new ArgumentNullException(response.SecretString);
        }

        return response.SecretString;
    }

}
