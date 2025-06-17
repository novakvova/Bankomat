using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPrivate.JSON_Converter
{
    public class RequestType0 : RequestBase
    {
        public override Int32 Type { get;} = 0; // Default type for this request
        public string Comment { get; set; } = string.Empty; // Default comment for type 0 requests

        public Int16 PassCode { get; set; } = 0; // Default passcode for type 0 requests//1945 - GOOD//1939 - BAD
    }
}
