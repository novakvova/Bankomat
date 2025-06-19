using MyPrivate;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using MyPrivate.JSON_Converter;
using MyClient.JSON_Converter;


int port = 5000;
string serverIp = IPAddress.Loopback.ToString();
TcpClient myClient = new TcpClient();


try
{
	await myClient.ConnectAsync(IPAddress.Parse(serverIp), port);
	SslStream stream = new(
		myClient.GetStream(),
			false,
			ValidateServerCertificate,
			null
	);
	await stream.AuthenticateAsClientAsync(serverIp, null, SslProtocols.Tls12, false);
	Console.WriteLine("Ви підключились до банкомату. ");
	var options = new JsonSerializerOptions();
	options.Converters.Add(new RequestBaseConverter());

	Console.Write("Введіть номер картки: ");
	if (!long.TryParse(Console.ReadLine(), out long cardNumber))
	{
		Console.WriteLine("Неправильний формат номеру картки.");
		return;
	}
	var request1 = new RequestType1
	{
		NumberCard = cardNumber
	};
	var response1 = await RequestAsync(stream, request1, options);
	if (response1 == null || response1.PassCode == 1918 || response1.PassCode == 1914)
	{
		Console.WriteLine("Вас заблоковано сервером.");
		return;
	}
	else if (response1.PassCode == 1789)
	{
		Console.WriteLine("Картку не знайдено");
		Console.WriteLine("Чи хочете ви зареєструватись(yes/no)?");
		var choice = Console.ReadLine();
		if(choice?.ToLower() == "yes")
		{
			Console.Write("Введіть нове Ім’я: "); 
			string firstName = Console.ReadLine();
			Console.Write("Введіть нове Прізвище: ");
			string lastName = Console.ReadLine();
			Console.Write("Введіть нове По-батькові: ");
			string fatherName = Console.ReadLine();
			Console.Write("Введіть новий PIN-код: ");
			if (!long.TryParse(Console.ReadLine(), out long pinCode))
			{
				Console.WriteLine("Невірний формат PIN-коду.");
				return;
			}
			var register = new RequestType2
			{
				FirstName = firstName,
				LastName = lastName,
				FatherName = fatherName,
				PinCode = pinCode
			};
			var resp = await RequestAsync(stream, register, options);
			PrintResponse(resp);
			if (resp?.PassCode != 1945)
			{
				return;
			}
				
		}
		else
		{
			return;
		}
			
	}
	else if (response1.PassCode != 1945)
	{
		Console.WriteLine("При перевірці картки сталась помилка");
		return;
	}
	for (int attempt = 1; attempt <= 3; attempt++)
	{
		Console.Write("Введіть ваше Ім’я: ");
		string firstName = Console.ReadLine();

		Console.Write("Введіть ваше Прізвище: ");
		string lastName = Console.ReadLine();

		Console.Write("Введіть ваше По-батькові: ");
		string fatherName = Console.ReadLine();

		Console.Write("Введіть ваш PIN-код: ");
		if (!long.TryParse(Console.ReadLine(), out long pinCode))
		{
			Console.WriteLine("Неправильний формат PIN-коду.");
			return;
		}
		var request2 = new RequestType2
		{
			FirstName = firstName,
			LastName = lastName,
			FatherName = fatherName,
			PinCode = pinCode
		};
		var response2 = await RequestAsync(stream, request2, options);
		if (response2?.PassCode == 1945)
		{
			Console.WriteLine("Авторизація пройшла успішно.\n");
			break;
		}
		else if (response2?.PassCode == 1918 || response2?.PassCode == 1914)
		{
			Console.WriteLine("Сервер заблокував вас за несанкціоновану спробу доступу в аккаунт.");
			return;
		}
		else if (attempt == 3)
		{
			Console.WriteLine("\nВи використали ліміт спроб вас заблоковано");
			return;
		}
		else
		{
			Console.WriteLine($"Ви ввели неправильні данні. Спробуйте ще раз.\n");
		}
		await RequestAsync(stream, request1, options);
	}
	while (true)
	{
		Console.WriteLine("\nОберіть запит банкомату:");
		Console.WriteLine("1 - Зняти кошти");
		Console.WriteLine("2 - Поповнити рахунок");
		Console.WriteLine("3 - Переглянути баланс");
		Console.WriteLine("4 - Вихід");
		Console.Write("->_ ");
		string choice = Console.ReadLine();

		if (choice == "4") break;

		else if (choice == "1")
		{
			Console.Write("Сума зняття: ");
			if (decimal.TryParse(Console.ReadLine(), out decimal sum))
			{
				var request = new RequestType3 { Sum = sum };
				var resp = await RequestAsync(stream, request, options);
				PrintResponse(resp);
			}
			else Console.WriteLine("Невірна сума.");
		}
		else if (choice == "2")
		{
			Console.Write("Сума поповнення: ");
			if (decimal.TryParse(Console.ReadLine(), out decimal sum))
			{
				var request = new RequestType4 { Sum = sum };
				var resp = await RequestAsync(stream, request, options);
				PrintResponse(resp);
			}
			else Console.WriteLine("Невірна сума.");
		}
		else if (choice == "3")
		{
			var request = new RequestType5();
			var resp = await RequestAsync(stream, request, options);
			PrintResponse(resp);
		}
		else
		{
			Console.WriteLine("Ви ввели невірну команду. Спробуйте ще раз");
		}


	}
}
catch (Exception ex)
{
	Console.WriteLine($"Помилка з'єднання: {ex.Message}");
}
finally
{
	myClient.Close();
	Console.WriteLine("З'єднання з банкоматом завершено.");
}

static async Task<ServerResponse> RequestAsync(SslStream stream, RequestBase request, JsonSerializerOptions options)
{
	string jsonrequest = JsonSerializer.Serialize(request, options);
	byte[] data = Encoding.UTF8.GetBytes(jsonrequest);
	await stream.WriteAsync(data, 0, data.Length);
	await stream.FlushAsync();

	byte[] buffer = new byte[4096];
	int bytesread = await stream.ReadAsync(buffer, 0, buffer.Length);
	string jsonresponse = Encoding.UTF8.GetString(buffer, 0, bytesread);
	try
	{
		return JsonSerializer.Deserialize<ServerResponse>(jsonresponse);
	}
	catch
	{
		return null;
	}
}
static void PrintResponse(ServerResponse? response)
{
	if (response == null)
	{
		Console.WriteLine("Банкомат не надіслав відповідь.");
		return;
	}

	Console.WriteLine($"\n Відповідь сервера: {response.Comment} (Код відповіді: {response.PassCode})");
	switch (response.PassCode)
	{
		case 1945:
			Console.WriteLine("Операція успішна.");
			break;
		case 1939:
			Console.WriteLine("Операція неуспішна.");
			break;
		case 1918:
			Console.WriteLine("Вас забанено за несанкціонований доступ.");
			break;
		case 1914:
			Console.WriteLine("Вас забанено за порушення послідовності авторизації.");
			break;
		case 1789:
			Console.WriteLine("В базі даних немає такого номеру картки");
			break;
		default:
			Console.WriteLine("Банкомат надіслав невідомий код відповіді.");
			break;
	}
}
static bool ValidateServerCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors)
{
	return true;
}