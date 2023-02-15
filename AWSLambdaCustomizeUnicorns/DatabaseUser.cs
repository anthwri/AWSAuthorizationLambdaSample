using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSLambdaCustomizeUnicorns
{
    public class DatabaseUser
    {
        public string? username { get; set; }
        public string? password { get; set; }
        public string? engine { get; set; }
        public string? host { get; set; }
        public int port { get; set; }
        public string? dbClusterIdentifier { get; set; }
    }
}
