using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPrivate.JSON_Converter
{
    public class RequestType1 : RequestBase
    {
        public override Int32 Type { get; } = 1; // Default type for this request
        public long NumberCard { get; set; } = 0; // Default value for card number
    }
}
