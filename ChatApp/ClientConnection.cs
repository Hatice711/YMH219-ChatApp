using System.Net.Sockets;
using System.Text;

namespace ChatApp
{
    // Sunucuya bağlı TEK bir istemciyi temsil eder
    public class ClientConnection
    {
        private readonly TcpClient tcpClient;
        private readonly StreamReader reader;
        private readonly StreamWriter writer;
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);   // aynı anda tek gönderim

        public string UserName { get; set; } = "(bilinmiyor)";
        public string EndPoint { get; }                          // istemcinin IP:port bilgisi
        public DateTime ConnectedAt { get; } = DateTime.Now;

        public ClientConnection(TcpClient client)
        {
            tcpClient = client;
            EndPoint = client.Client.RemoteEndPoint?.ToString() ?? "?";

            NetworkStream stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
        }

        // Bir satır (bir mesaj) okur. Bağlantı kapandıysa null döner.
        public Task<string?> ReceiveLineAsync()
        {
            return reader.ReadLineAsync();
        }

        // Mesajı JSON satırı olarak gönderir
        public async Task SendAsync(ChatMessage message)
        {
            await sendLock.WaitAsync();
            try
            {
                await writer.WriteLineAsync(message.ToJson());
            }
            finally
            {
                sendLock.Release();
            }
        }

        public void Close()
        {
            tcpClient.Close();
        }
    }
}