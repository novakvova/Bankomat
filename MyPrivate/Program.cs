using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Net;
using System.IO;
using System.Collections.Concurrent;
TcpListener tcpListener = new TcpListener(IPAddress.Any, 5000); // Listening on port 5000 | пам'ять виділяється динамічно
const int MinIntervalMs = 5000; // Minimum interval between connections in milliseconds
ConcurrentDictionary<IPEndPoint, DateTime> clientLastAccess = new(); //фіксація по публічним IP - адресам клієнтів
IPEndPoint iP;
DateTime now;
tcpListener.Start();
Console.WriteLine($"Server started on port 5000. Waiting for connections...");
while (true)
{
    try
    {
        TcpClient client = await tcpListener.AcceptTcpClientAsync(); //звільняємо потік при очікуванні клієнта
        /// Check if the client is already connected or if the last access time is within the minimum interval
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
                    continue; // Skip processing this client
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}
