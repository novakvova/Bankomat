using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net;
using System.IO;
using System.Collections.Concurrent;
using System.Net.Security;
using MyPrivate.JSON_Converter;
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
    NetworkStream networkStream = client.GetStream();
    SslStream sslStream = new SslStream(networkStream,false); // Accept any certificate
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
                   // ...
                }
                else if (request is RequestType2 request2)
                {
                   // ...
                }
                else if (request is RequestType0 request0)
                {
                    //...
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
