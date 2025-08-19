using System.Net;
using System.Net.Sockets;
using System.Text;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

var port = 5000;
var listener = new TcpListener(IPAddress.Any, port);
listener.Start();
Console.WriteLine($"[SERVER] Listening on 0.0.0.0:{port}");

while (!cts.IsCancellationRequested)
{
    var client = await listener.AcceptTcpClientAsync(cts.Token);
    _ = HandleClientAsync(client, cts.Token);
}

static async Task HandleClientAsync(TcpClient client, CancellationToken token)
{
    var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
    Console.WriteLine($"[+] {remote} connected");
    try
    {
        using var stream = client.GetStream();
        await WriteAsync(stream, "Welcome to TcpEchoServer. Type 'quit' to exit.\n", token);
        var buffer = new byte[4096];
        while (!token.IsCancellationRequested)
        {
            int read = await stream.ReadAsync(buffer, token);
            if (read == 0) break;

            var text = Encoding.UTF8.GetString(buffer, 0, read).TrimEnd('\r', '\n');
            if (text.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;

            await WriteAsync(stream,$"[echo] {text}\n" , token);
        }
    }
    catch(IOException) {  }
    catch(ObjectDisposedException) { }
    finally
    {
        Console.WriteLine($"[-] {remote} disconnected");
        client.Close();
    }
}
static ValueTask WriteAsync(NetworkStream stream, string message, CancellationToken token)
    => stream.WriteAsync(Encoding.UTF8.GetBytes(message), token);
