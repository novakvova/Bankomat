using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net;
using System.IO;
using System.Collections.Concurrent;


int port = 5000;
try
{
	TcpClient myClient = new TcpClient();
	await myClient.ConnectAsync("127.0.0.1", port);
	NetworkStream stream = myClient.GetStream();

	Console.WriteLine("Ви підключились до банкомату");
}
catch(Exception ex)
{
	Console.WriteLine($"При підключені до банкомата сталась помилка:{ex.Message}");
}
