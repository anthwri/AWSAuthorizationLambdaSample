using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSAuthorizationLambdaSample
{
    public class PemKey
    {
        public string alg { get; set; } = string.Empty;
        public string e { get; set; } = string.Empty;
        public string kid { get; set; } = string.Empty;
        public string kty { get; set; } = string.Empty;
        public string n { get; set; } = string.Empty;
        public string use { get; set; } = string.Empty;
    }

    public class JwksObject
    {
        public List<PemKey> keys { get; set; } = new();
    }
}
