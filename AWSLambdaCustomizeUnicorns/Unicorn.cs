using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSLambdaCustomizeUnicorns
{
    public class Unicorn
    {
        public int ID { get; set; }
        public string? NAME { get; set; }
        //COMPANY has been added here so that you can see what happens if various users retrieve the Unicorns.
        //A user that belongs to a company will see only their own unicorns.
        public int COMPANY { get; set; }
        public string? IMAGEURL { get; set; }
        public int SOCK { get; set; }
        public int HORN { get; set; }
        public int GLASSES { get; set; }
        public int CAPE { get; set; }
    }
}
