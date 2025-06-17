using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPrivate.JSON_Converter
{
    public class RequestType3 : RequestBase
    {

        public override Int32 Type { get; } = 3; // Default type for this request
        public Decimal Sum { get; set; } = 0; // Default value for sum
    }
}
