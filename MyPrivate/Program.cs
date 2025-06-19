using MyClient.JSON_Converter;
using MyPrivate.Data.Entitys;
using MyPrivate.JSON_Converter;
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
using System.Threading.Tasks;
int port = 5000; // Port to listen on
TcpListener tcpListener = new TcpListener(IPAddress.Any, port); // Listening on port 5000 | пам'ять виділяється динамічно
const int MinIntervalMs = 5000; // Minimum interval between connections in milliseconds
const int MaxConcurrentClients = 100; //максимальна к-сть клієнтів
ConcurrentDictionary<IPEndPoint, DateTime> clientLastAccess = new(); //фіксація по публічним IP - адресам клієнтів
ConcurrentBag<IPEndPoint> bannedClients = new(); // Collection to store banned clients
SemaphoreSlim semaphoreSlim = new SemaphoreSlim(MaxConcurrentClients); // Semaphore to control access to the shared resource
IPEndPoint iP;
DateTime now;
tcpListener.Start();
TcpClient client;
Console.WriteLine($"Сервер запущено на {tcpListener.LocalEndpoint}. Очікуємо клієнтів...");
while (true)
{
    try
    {
        await semaphoreSlim.WaitAsync(); // Wait for an available slot

        client = await tcpListener.AcceptTcpClientAsync(); //звільняємо потік при очікуванні клієнта

        /// Check if the client is already connected or if the last access time is within the minimum interval
        /// 
        iP = client.Client.RemoteEndPoint as IPEndPoint; //зберігання IP-адреси клієнта
        if (iP == null)
        {
            Console.WriteLine("Не вдалося отримати IP-адресу клієнта.");
            client.Close();
            semaphoreSlim.Release(); // Release the semaphore slot
            continue; // Skip processing this client
        }
        else if (bannedClients.Contains(iP)) // Check if the client is banned
        {
            Console.WriteLine($"Спроба підключення від заблокованого клієнта: {iP}");
            client.Close(); // Close the connection if the client is banned
            semaphoreSlim.Release(); // Release the semaphore slot
            continue; // Skip processing this client
        }
        if (clientLastAccess.ContainsKey(iP))
        {
            now = DateTime.UtcNow;

            if (clientLastAccess.TryGetValue(iP, out DateTime lastAccess))
            {

                if ((now - lastAccess).TotalMilliseconds < MinIntervalMs)
                {

                    Console.WriteLine("Спробу підключення відхилено через обмеження мінімального інтервалу.");

                    client.Close();

                    semaphoreSlim.Release(); // Release the semaphore slot

                    continue; // Skip processing this client
                }

            }

        }
        else
        {
            clientLastAccess.TryAdd(iP, DateTime.UtcNow); // Add new client with current time

        }
        _ = HandleClientAsync(client).ContinueWith(_ => semaphoreSlim.Release()); // Start handling the client asynchronously
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка: {ex.Message}");
    }




}//HandShake and SSL/TLS encryption
async Task HandleClientAsync(TcpClient client)
{
    MyPrivate.Data.ContextATM context = new MyPrivate.Data.ContextATM();
    using NetworkStream networkStream = client.GetStream();
    using SslStream sslStream = new SslStream(networkStream, false); // Accept any certificate
    var json_options = new System.Text.Json.JsonSerializerOptions();
    json_options.Converters.Add(new MyPrivate.JSON_Converter.RequestBaseConverter());
    int tryes = 0;
    RequestBase? request = null;

    try
    {
        string certPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "server.pfx");
        X509Certificate2 certificate = new X509Certificate2(certPath, "password"); // Replace with your certificate password
        await sslStream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls12, true);
        Console.WriteLine("Client connected: " + client.Client.RemoteEndPoint);

        UserEntity? user = null; // Initialize user variable
        bool isAuthenticated = false; // Flag to check if the client is authenticated
        long currentcardnumber = 0;
        while (true)
        {
            // Виділяємо новий буфер для кожного запиту
            byte[] buffer = new byte[4096];
            int bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                Console.WriteLine("Клієнт відключився: " + client.Client.RemoteEndPoint);
                break; // Exit the loop if the client disconnects
            }
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Array.Clear(buffer, 0, buffer.Length); // Clear the buffer for the next read
            Console.WriteLine($"Отримано повідомлення від користувача {client.Client.RemoteEndPoint}: {message}");
            request = System.Text.Json.JsonSerializer.Deserialize<RequestBase>(message, json_options);
            if (request == null)
            {
                Console.WriteLine("Отримано нульовий запит, обробка пропущена.");
                continue; // Skip processing if the request is null
            }
            else
            {
                if (request is RequestType1 request1)
                {
                    currentcardnumber = request1.NumberCard;
                    user = context.Users.FirstOrDefault(u => u.CardNumber == currentcardnumber);
                    if (user != null)
                    {
                        var response = new RequestType0
                        {
                            Comment = "Номер картки існує. Будь ласка, введіть PIB та PIN-код",
                            PassCode = 1945
                        };
                        byte[] responseBuffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response, json_options));
                        await sslStream.WriteAsync(responseBuffer);
                    }
                    else
                    {
                        var response = new RequestType0
                        {
                            Comment = "Такої карти не існує",
                            PassCode = 1789
                        };
                        byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                        await sslStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                    }
                }
                else if (request is RequestType2 request2)
                {
                    if (user != null)
                    {
                        Console.WriteLine($"Обробка авторизації для користувача: {user.FirstName} {user.LastName}");
                        if ((user.FirstName.Equals(request2.FirstName)) && (user.FatherName.Equals(request2.FatherName)) && (user.LastName.Equals(request2.LastName)))
                        {
                            isAuthenticated = true;
                            var response = new RequestType0
                            {
                                Comment = "Авторизація успішна.",
                                PassCode = 1945
                            };
                            byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                            await sslStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                        }
                        else
                        {
                            tryes++;
                            if (tryes >= 3)
                            {
                                bannedClients.Add(client.Client.RemoteEndPoint as IPEndPoint);
                                Console.WriteLine($"Клієнт {client.Client.RemoteEndPoint as IPEndPoint} заблокований через велику кількість невдалих спроб автентифікації.");
                                var response = new RequestType0
                                {
                                    Comment = "Вас забанили через велику кількість невдалих спроб автентифікації.",
                                    PassCode = 1918
                                };
                                byte[] responseBuffer1 = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await sslStream.WriteAsync(responseBuffer1, 0, responseBuffer1.Length);
                                break;
                            }
                            else
                            {
                                var response = new RequestType0
                                {
                                    Comment = "Помилка автентифікації",
                                    PassCode = 1939
                                };
                                byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await sslStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                            }
                        }
                    }
                    else
                    {
                        var new_user = new UserEntity
                        {
                            CardNumber = currentcardnumber,
                            FirstName = request2.FirstName,
                            LastName = request2.LastName,
                            FatherName = request2.FatherName,
                            PinCode = request2.PinCode,

                        };
                        context.Users.Add(new_user);
                        context.SaveChanges();
                        var balance = new BalanceEntity
                        {
                            UserId = new_user.Id,
                            Amount = 0
                        };
                        context.Balances.Add(balance);
                        context.SaveChanges();

                        user = new_user;
                        isAuthenticated = true;

                        var response = new RequestType0
                        {
                            Comment = "Користувача зареєстровано успішно",
                            PassCode = 1945
                        };
                        byte[] responseBuffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response, json_options));
                        await sslStream.WriteAsync(responseBuffer);
                    }
                }
                else if (request is RequestType3 request3)
                {
                    if (user != null && isAuthenticated == true)
                    {
                        var balance = context.Balances.FirstOrDefault(c => c.UserId == user.Id);
                        if (balance != null)
                        {
                            if (balance.Amount > 0 && balance.Amount > request3.Sum)
                            {
                                balance.Amount -= request3.Sum;
                                context.SaveChanges();
                                var response = new RequestType0
                                {
                                    Comment = "Транзакція успішна",
                                    PassCode = 1945
                                };
                                byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await sslStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                            }
                            else
                            {
                                var response = new RequestType0
                                {
                                    Comment = "Недостатньо коштів на рахунку",
                                    PassCode = 1939
                                };
                                byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await sslStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                            }
                        }
                    }
                }
                else if (request is RequestType4 request4)
                {
                    if (user != null && isAuthenticated == true)
                    {
                        var balance = context.Balances.FirstOrDefault(c => c.UserId == user.Id);
                        if (balance != null)
                        {
                            balance.Amount += request4.Sum;
                            context.SaveChanges();
                            var response = new RequestType0
                            {
                                Comment = "Депозит успішний.",
                                PassCode = 1945
                            };
                            byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                            await sslStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                        }
                    }
                }
                else if (request is RequestType5 request5)
                {
                    if (user != null && isAuthenticated == true)
                    {
                        var balance = context.Balances.FirstOrDefault(c => c.UserId == user.Id);
                        if (balance != null)
                        {
                            var response = new RequestType0
                            {
                                Comment = $"На вашому рахунку: {balance.Amount}",
                                PassCode = 1945
                            };
                            byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                            await sslStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Невідомий тип запиту: {request.Type}");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка клієнта: {ex.Message}");
    }
    finally
    {
        client.Close();
        Console.WriteLine($"Клієнт {client.Client.RemoteEndPoint} відєднався");
    }
}
