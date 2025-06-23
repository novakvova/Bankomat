using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MyPrivate.JSON_Converter;
using VisualClient.Models;

public class AtmClientService
{
    private readonly string _serverIp = "18.185.184.246"; 
    private readonly int _port = 5000;
    private TcpClient _client;
    private SslStream _stream;
    private readonly JsonSerializerOptions _jsonOptions;

    public AtmClientService()
    {
        _client = new TcpClient();
        _jsonOptions = new JsonSerializerOptions();
        _jsonOptions.Converters.Add(new RequestBaseConverter());
    }

    public async Task<ServerResponse?> SendAsync(RequestBase request)
    {
        try
        {
            if (_client == null || !_client.Connected)
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_serverIp, _port);
                _stream = new SslStream(_client.GetStream(), false, (_, _, _, _) => true);
                await _stream.AuthenticateAsClientAsync(_serverIp);
            }
            
            string requestJson = JsonSerializer.Serialize(request, _jsonOptions);
            byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);

            await _stream.WriteAsync(requestBytes);
            await _stream.FlushAsync();

            var buffer = new byte[4096];
            using var ms = new MemoryStream();
            int bytesRead;
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            do
            {
                bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                if (bytesRead == 0) break;
                ms.Write(buffer, 0, bytesRead);
            } while (bytesRead == buffer.Length);

            string responseJson = Encoding.UTF8.GetString(ms.ToArray());
            
            var response = JsonSerializer.Deserialize<ServerResponse>(responseJson);
            return response;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}