using MyPrivate;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;


int port = 5000;
TcpClient myClient = new TcpClient();


try
{
	await myClient.ConnectAsync("127.0.0.1", port);
	SslStream stream = new(
		myClient.GetStream(),
			false,
			new RemoteCertificateValidationCallback(ValidateServerCertificate),
			null
	);
	await stream.AuthenticateAsClientAsync("127.0.0.1");
	Console.WriteLine("Ви підключились до банкомату. ");
	Console.WriteLine("Введіть запит (Баланс, Зняти, Покласти):");
	string choice = Console.ReadLine().Trim().ToLower();
	float cost = 0;
	if (choice == "зняти" || choice == "покласти")
	{
		Console.WriteLine("Введіть суму:");
		string coststr = Console.ReadLine();
		if (!float.TryParse(coststr, out cost) || cost <= 0)
		{
			Console.WriteLine("Ви вказали неправильний формат суми.");
			return;
		}
	}
	var request = new
	{
		Action = choice,
		Amount = cost
	};
	string jsonRequest = JsonSerializer.Serialize(request);
	byte[] data = Encoding.UTF8.GetBytes(jsonRequest);
	await stream.WriteAsync(data, 0, data.Length);
	await stream.FlushAsync();

	byte[] buffer = new byte[4096];
	int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
	string jsonResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);
	var response = JsonSerializer.Deserialize<ServerResponse>(jsonResponse);
	if (response != null)
	{
		Console.WriteLine($"Код відповіді: {response.PassCode}");
		Console.WriteLine($"Коментар від сервера: {response.Comment}");
	}
	else
	{
		Console.WriteLine("Не вдалось отримати відповідь сервера");
	}
}
catch (Exception ex)
{
	Console.WriteLine($"При підключенні до банкомату сталася помилка: {ex.Message}");
}
finally
{
	myClient.Close();
}

static bool ValidateServerCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors)
{
	if (errors == SslPolicyErrors.None)
		return true;

	string trustedThumbprint = "XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX XX"; 

	if (cert != null)
	{
		string certThumbprint = cert.GetCertHashString();
		if (certThumbprint.Equals(trustedThumbprint, StringComparison.OrdinalIgnoreCase))
		{
			return true; 
		}
	}

	Console.WriteLine($"Помилка сертифіката: {errors}");
	return false;
}