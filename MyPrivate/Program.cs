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
TcpListener tcpListener = new TcpListener(IPAddress.Any, 5000); // Listening on port 5000 | пам'ять виділяється динамічно
const int MinIntervalMs = 5000; // Minimum interval between connections in milliseconds
const int MaxConcurrentClients = 100; //максимальна к-сть клієнтів
ConcurrentDictionary<IPEndPoint, DateTime> clientLastAccess = new(); //фіксація по публічним IP - адресам клієнтів
SemaphoreSlim semaphoreSlim = new SemaphoreSlim(MaxConcurrentClients); // Semaphore to control access to the shared resource
IPEndPoint iP;
DateTime now;
tcpListener.Start();
TcpClient client;
Console.WriteLine($"Server started on port 5000. Waiting for connections...");
while (true)
{
    try
    {
        await semaphoreSlim.WaitAsync(); // Wait for an available slot

        client = await tcpListener.AcceptTcpClientAsync(); //звільняємо потік при очікуванні клієнта

        /// Check if the client is already connected or if the last access time is within the minimum interval
        /// 
        iP = client.Client.RemoteEndPoint as IPEndPoint; //зберігання IP-адреси клієнта

        if (clientLastAccess.ContainsKey(iP))
        {
            now = DateTime.Now;

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
            clientLastAccess.TryAdd(iP, DateTime.Now); // Add new client with current time

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
    NetworkStream networkStream = client.GetStream();
    SslStream sslStream = new SslStream(networkStream, false); // Accept any certificate
    var json_options = new System.Text.Json.JsonSerializerOptions();
    json_options.Converters.Add(new MyPrivate.JSON_Converter.RequestBaseConverter());
    RequestBase? request = null;

    try
    {

        await sslStream.AuthenticateAsServerAsync(
            new System.Security.Cryptography.X509Certificates.X509Certificate2("server.pfx", "password"),
            false,
            System.Security.Authentication.SslProtocols.Tls12,
            true);

        Console.WriteLine("Client connected: " + client.Client.RemoteEndPoint);

        byte[] buffer = new byte[1024];
        UserEntity? user = null; // Initialize user variable//it is used to store user information if needed//keep it null until we receive a request that requires user data
        bool isAuthenticated = false; // Flag to check if the client is authenticated

        int bytesRead = client.ReceiveBufferSize;
        while (true)
        {
            bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length);
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
                        user = context.Users.FirstOrDefault(u => u.CardNumber == request1.NumberCard); // Retrieve user by card number
                        var response = new RequestType0
                        {
                            Comment = "Card number exists / Please enter PIB and PIN - code",
                            PassCode = 1945 // Example passcode for successful request
                        };

                        buffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                        await sslStream.WriteAsync(buffer, 0, buffer.Length); // Send response back to client

                    } // Example usage of request1
                }
                else if (request is RequestType2 request2)
                {
                    if (user != null) // Ensure user is not null
                    {
                        // Process request2 with user data
                        Console.WriteLine($"Processing RequestType2 for user: {user.FirstName} {user.LastName}");
                        // Here you can add logic to handle the request, e.g., update user information
                        // ...
                        if ((user.FirstName.Equals(request2.FirstName)) && (user.FatherName.Equals(request2.FatherName)) && (user.LastName.Equals(request2.LastName)))
                        {
                            isAuthenticated = true; // Set authentication flag to true if the user data matches
                            var response = new RequestType0
                            {
                                Comment = "Authentication successful",
                                PassCode = 1945 // Example passcode for successful request
                            };
                            buffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                            await sslStream.WriteAsync(buffer, 0, buffer.Length); // Send response back to client

                        }
                        else
                        {
                            //тут треба надати три спроби, якщо клієнт не ввів правильні дані, то заблокувати його на сервері і не давати можливості авторизації
                            isAuthenticated = false; // Set authentication flag to false if the user data does not match
                            var response = new RequestType0
                            {
                                Comment = "Authentication failed",
                                PassCode = 1939 // Example passcode for failed request
                            };
                            buffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                            await sslStream.WriteAsync(buffer, 0, buffer.Length); // Send response back to client
                        }

                    }
                    else
                    {
                        //тут варто заблокувати клієнта бо він неправильно почав авторизацію згідно клієнтською программи

                        //!!!!!!!!!!!!!!!!!!!!!!!!!!
                        Console.WriteLine("User not found for RequestType2 processing.");
                    }
                    // ...
                }
                else if (request is RequestType3 request3)
                {
                    if (user != null && isAuthenticated == true)
                    {
                        //отримуємо доступ до балансу(треба буде реалізувати через метод)
                        var balance = context.Balances.FirstOrDefault(c => c.UserId == user.Id); // Retrieve balance for the authenticated user
                        if (balance != null)
                        {
                            if (balance.Amount > 0 && balance.Amount > request3.Sum)
                            {
                                balance.Amount -= request3.Sum; // Deduct the requested sum from the balance
                                context.SaveChanges(); // Save changes to the database
                                var response = new RequestType0
                                {
                                    Comment = "Transaction successful",
                                    PassCode = 1945 // Example passcode for successful transaction
                                };
                                buffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await sslStream.WriteAsync(buffer, 0, buffer.Length); // Send response back to client
                            }
                            else
                            {
                                var response = new RequestType0
                                {
                                    Comment = "Insufficient funds",
                                    PassCode = 1939 // Example passcode for failed transaction
                                };
                                buffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await sslStream.WriteAsync(buffer, 0, buffer.Length); // Send response back to client
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
