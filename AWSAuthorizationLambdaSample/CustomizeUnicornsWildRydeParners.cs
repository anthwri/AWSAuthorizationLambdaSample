using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSAuthorizationLambdaSample
{
    [DynamoDBTable("CustomizeUnicorns-WildRydePartners")]
    public class CustomizeUnicorns_WildRydePartners
    {
        [DynamoDBHashKey("ClientID")]
        public string? ClientID { get; set; }
        [DynamoDBProperty("CompanyID")]
        public int CompanyID { get; set; }
    }
}
