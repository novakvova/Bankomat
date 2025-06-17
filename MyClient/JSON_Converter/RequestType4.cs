using MyPrivate;
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using MyPrivate.JSON_Converter;

namespace MyClient.JSON_Converter
{
	public class RequestType4 : RequestBase
	{
		public override Int32 Type { get;} = 4;
		public Decimal Sum { get; set; } = 0;
	}
}
