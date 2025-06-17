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
        if(iP == null)
        {
            Console.WriteLine("Failed to retrieve client IP address.");
            client.Close();
            semaphoreSlim.Release(); // Release the semaphore slot
            continue; // Skip processing this client
        }
        else if(bannedClients.Contains(iP)) // Check if the client is banned
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

        byte[] buffer = new byte[4096];
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
            Array.Clear(buffer, 0, buffer.Length); // Clear the buffer for the next read
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
                        Array.Clear(buffer, 0, buffer.Length); // Clear the buffer for the next read

                    } // Example usage of request1
                    else
                    {
                        var response = new RequestType0
                        {
                            Comment = "Card number does not exist",
                            PassCode = 1789 // Example passcode for failed request
                        };
                        buffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                        await sslStream.WriteAsync(buffer, 0, buffer.Length); // Send response back to client
                        Array.Clear(buffer, 0, buffer.Length); // Clear the buffer for the next read

                    }
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
                            Array.Clear(buffer, 0, buffer.Length); // Clear the buffer for the next read

                        }
                        else
                        {
                            RequestType0 response;
                            if(tryes >= 3)
                            {
                                // Block the client after 3 failed attempts
                                bannedClients.Add(client.Client.RemoteEndPoint as IPEndPoint); // Add the client's IP to the banned list
                                Console.WriteLine($"Client {client.Client.RemoteEndPoint as IPEndPoint} has been banned due to too many failed authentication attempts.");
                                response = new RequestType0
                                {
                                    Comment = "You have been banned due to too many failed authentication attempts.",
                                    PassCode = 1918 // Example passcode for failed request
                                };
                                user = null; // Clear user data since the client is banned

                                buffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                                await sslStream.WriteAsync(buffer, 0, buffer.Length); // Send response back to client
                                Array.Clear(buffer, 0, buffer.Length); // Clear the buffer for the next read
                                break; // Exit the loop to stop processing this client
                            }
                            tryes++; // Increment the number of attempts
                            //тут треба надати три спроби, якщо клієнт не ввів правильні дані, то заблокувати його на сервері і не давати можливості авторизації
                            isAuthenticated = false; // Set authentication flag to false if the user data does not match
                            response = new RequestType0
                            {
                                Comment = "Authentication failed",
                                PassCode = 1939 // Example passcode for failed request
                            };
                            buffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                            await sslStream.WriteAsync(buffer, 0, buffer.Length); // Send response back to client
                            Array.Clear(buffer, 0, buffer.Length); // Clear the buffer for the next read
                        }

                    }
                    else
                    {
                        bannedClients.Add(client.Client.RemoteEndPoint as IPEndPoint); // Add the client's IP to the banned list
                        var response = new RequestType0
                        {
                            Comment = "User not found",
                            PassCode = 1914 // Example passcode for failed request
                        };
                        Console.WriteLine("User not found for RequestType2 processing.");
                        buffer = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(response, json_options));
                        await sslStream.WriteAsync(buffer, 0, buffer.Length); // Send response back to client
                        Array.Clear(buffer, 0, buffer.Length); // Clear the buffer for the next read
                        break; 
                    }
                    
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
                                Array.Clear(buffer, 0, buffer.Length); // Clear the buffer for the next read
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
                                Array.Clear(buffer, 0, buffer.Length); // Clear the buffer for the next read
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
