using MyPrivate.JSON_Converter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyClient.JSON_Converter
{
	public class RequestType5 : RequestBase //запит на реєстрацію
	{
		public override Int32 Type { get; } = 5;
        public string FirstName { get; set; } = string.Empty; // Default value for first name
        public string LastName { get; set; } = string.Empty; // Default value for last name

        public string FatherName { get; set; } = string.Empty; // Default value for father's name

        public Int64 PinCode { get; set; } = 0; // Default value for PIN code
    }
}
