using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net;
using System.IO;
using System.Collections.Concurrent;
using System.Net.Security;
using MyPrivate.JSON_Converter;
using MyPrivate.Data.Entitys;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
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
Console.WriteLine($"Server started on {tcpListener.LocalEndpoint}. Waiting for connections...");
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
            Console.WriteLine("Failed to retrieve client IP address.");
            client.Close();
            semaphoreSlim.Release(); // Release the semaphore slot
            continue; // Skip processing this client
        }
        else if (bannedClients.Contains(iP)) // Check if the client is banned
        {
            Console.WriteLine($"Connection attempt from banned client: {iP}");
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

                    Console.WriteLine("Connection attempt denied due to minimum interval restriction.");

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
        Console.WriteLine($"Error: {ex.Message}");
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

        while (true)
        {
            // Виділяємо новий буфер для кожного запиту
            byte[] buffer = new byte[4096];
            int bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                Console.WriteLine("Client disconnected: " + client.Client.RemoteEndPoint);
                break; // Exit the loop if the client disconnects
            }
            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Received message from {client.Client.RemoteEndPoint}: {message}");
            request = System.Text.Json.JsonSerializer.Deserialize<RequestBase>(message, json_options);
            if (request == null)
            {
                Console.WriteLine("Received null request, skipping processing.");
                continue; // Skip processing if the request is null
            }
            else
            {
                if (request is RequestType1 request1)
                {
                    if (context.Users.Any(u => u.CardNumber == request1.NumberCard))
                    {
                        user = context.Users.FirstOrDefault(u => u.CardNumber == request1.NumberCard);
                        var response = new RequestType0
                        {
                            Comment = "Card number exists / Please enter PIB and PIN - code",
                            PassCode = 1945
                        };
                        byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                        await sslStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                    }
                    else
                    {
                        var response = new RequestType0
                        {
                            Comment = "Card number does not exist",
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
                        Console.WriteLine($"Processing RequestType2 for user: {user.FirstName} {user.LastName}");
                        if ((user.FirstName.Equals(request2.FirstName)) && (user.FatherName.Equals(request2.FatherName)) && (user.LastName.Equals(request2.LastName)))
                        {
                            isAuthenticated = true;
                            var response = new RequestType0
                            {
                                Comment = "Authentication successful",
                                PassCode = 1945
                            };
                            byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                            await sslStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                        }
                        else
                        {
                            RequestType0 response;
                            if (tryes >= 3)
                            {
                                bannedClients.Add(client.Client.RemoteEndPoint as IPEndPoint);
                                Console.WriteLine($"Client {client.Client.RemoteEndPoint as IPEndPoint} has been banned due to too many failed authentication attempts.");
                                response = new RequestType0
                                {
                                    Comment = "You have been banned due to too many failed authentication attempts.",
                                    PassCode = 1918
                                };
                                user = null;
                                byte[] responseBuffer1 = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await sslStream.WriteAsync(responseBuffer1, 0, responseBuffer1.Length);
                                break;
                            }
                            tryes++;
                            isAuthenticated = false;
                            response = new RequestType0
                            {
                                Comment = "Authentication failed",
                                PassCode = 1939
                            };
                            byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                            await sslStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                        }
                    }
                    else
                    {
                        bannedClients.Add(client.Client.RemoteEndPoint as IPEndPoint);
                        var response = new RequestType0
                        {
                            Comment = "User not found",
                            PassCode = 1914
                        };
                        Console.WriteLine("User not found for RequestType2 processing.");
                        byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                        await sslStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                        break;
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
                                    Comment = "Transaction successful",
                                    PassCode = 1945
                                };
                                byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await sslStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                            }
                            else
                            {
                                var response = new RequestType0
                                {
                                    Comment = "Insufficient funds",
                                    PassCode = 1939
                                };
                                byte[] responseBuffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await sslStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                            }
                        }
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error handling client: {ex.Message}");
    }
    finally
    {
        client.Close();
        Console.WriteLine("Client disconnected: " + client.Client.RemoteEndPoint);
    }
}
