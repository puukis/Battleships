using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using BattleshipsLan.Network.Protocol;

namespace BattleshipsLan.Network;

public class NetworkManager
{
    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private bool _isRunning;

    public event Action<Message>? OnMessageReceived;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    public bool IsHost { get; private set; }
    public bool IsConnected => _client?.Connected ?? false;

    public async Task StartHostAsync(int port)
    {
        IsHost = true;
        
        try 
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            
            _client = await _listener.AcceptTcpClientAsync();
            _stream = _client.GetStream();
            OnConnected?.Invoke();
            _ = ReceiveLoopAsync();
        }
        catch (Exception)
        {
            Stop();
            throw;
        }
    }

    public async Task ConnectAsync(string ip, int port)
    {
        IsHost = false;
        _client = new TcpClient();
        try
        {
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();
            OnConnected?.Invoke();
            _ = ReceiveLoopAsync();
        }
        catch (Exception)
        {
            Stop();
            throw;
        }
    }

    public async Task SendMessageAsync(Message message)
    {
        if (_stream == null || !_client!.Connected) return;

        try
        {
            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json + "\n"); // Newline delimiter
            await _stream.WriteAsync(bytes);
        }
        catch
        {
            Stop();
        }
    }

    private async Task ReceiveLoopAsync()
    {
        _isRunning = true;
        var buffer = new byte[4096];
        var sb = new StringBuilder();

        try
        {
            while (_isRunning && _client != null && _client.Connected)
            {
                int bytesRead = await _stream!.ReadAsync(buffer);
                if (bytesRead == 0) break;

                sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                
                // Process complete messages
                string content = sb.ToString();
                int newlineIndex;
                while ((newlineIndex = content.IndexOf('\n')) >= 0)
                {
                    string json = content.Substring(0, newlineIndex);
                    content = content.Substring(newlineIndex + 1);
                    
                    try 
                    {
                        var message = JsonSerializer.Deserialize<Message>(json);
                        if (message != null)
                        {
                            // Dispatch on UI thread later, but here just invoke
                            OnMessageReceived?.Invoke(message);
                        }
                    }
                    catch (JsonException) { /* Ignore malformed */ }
                }
                sb.Clear();
                sb.Append(content);
            }
        }
        catch
        {
            // Connection dropped
        }
        finally
        {
            Stop();
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _stream?.Dispose();
        _client?.Dispose();
        _listener?.Stop();
        OnDisconnected?.Invoke();
    }
    
    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }
}
