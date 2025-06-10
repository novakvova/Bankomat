using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPrivate.JSON_Converter
{
    public class RequestType2 : RequestBase
    {
        public string FirstName { get; set; } = string.Empty; // Default value for first name
        public string LastName { get; set; } = string.Empty; // Default value for last name

        public string FatherName { get; set; } = string.Empty; // Default value for father's name

        public Int64 PinCode { get; set; } = 0; // Default value for PIN code
    }
}
