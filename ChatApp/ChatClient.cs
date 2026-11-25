using System.Net.Sockets;
using System.Text;

namespace ChatApp
{
    // Sunucuya bağlanıp mesaj gönderen ve alan istemci tarafı
    public class ChatClient
    {
        private TcpClient? tcpClient;
        private StreamReader? reader;
        private StreamWriter? writer;
        private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

        public bool IsConnected { get; private set; }
        public string UserName { get; private set; } = "";

        // Arayüzün dinleyeceği olaylar
        public event Action<ChatMessage>? MessageReceived;   // sunucudan mesaj geldi
        public event Action<string>? Disconnected;            // bağlantı beklenmedik şekilde koptu (sebep)

        public async Task ConnectAsync(string host, int port, string userName)
        {
            tcpClient = new TcpClient();

            try
            {
                // En fazla 5 saniye bekle; sunucu yoksa sonsuza kadar takılmasın
                using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await tcpClient.ConnectAsync(host, port, timeout.Token);
            }
            catch
            {
                tcpClient.Dispose();
                tcpClient = null;
                throw;   // hatayı arayüze ilet, orada kullanıcıya gösterilecek
            }

            NetworkStream stream = tcpClient.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

            UserName = userName;
            IsConnected = true;

            await writer.WriteLineAsync(ChatMessage.CreateJoin(userName).ToJson());   // sunucuya kendini tanıt
            _ = ReceiveLoopAsync();   // gelen mesajları dinlemeye başla
        }

        public async Task SendAsync(string text)
        {
            if (!IsConnected || writer == null)
                throw new InvalidOperationException("Sunucuya bağlı değilsiniz.");

            await sendLock.WaitAsync();
            try
            {
                await writer.WriteLineAsync(ChatMessage.CreateChat(UserName, text).ToJson());
            }
            finally
            {
                sendLock.Release();
            }
        }

        // Kullanıcı "Bağlantıyı Kes" dediğinde
        public void Disconnect()
        {
            if (!IsConnected)
                return;

            try
            {
                writer?.WriteLine(ChatMessage.CreateLeave(UserName).ToJson());   // sunucuya ayrıldığını bildir
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException)
            {
                // bağlantı zaten kopmuş olabilir, önemli değil
            }

            Cleanup();
        }

        // Sunucudan gelen mesajları sürekli okur
        private async Task ReceiveLoopAsync()
        {
            string reason = "Sunucu bağlantıyı kapattı.";

            try
            {
                while (IsConnected && reader != null)
                {
                    string? line = await reader.ReadLineAsync();
                    if (line == null)
                        break;   // sunucu bağlantıyı kapattı

                    ChatMessage? message = ChatMessage.FromJson(line);
                    if (message != null)
                        MessageReceived?.Invoke(message);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is SocketException || ex is ObjectDisposedException)
            {
                reason = "Sunucuyla bağlantı koptu.";
            }

            // Kullanıcı kendisi kesmediyse arayüze haber ver
            if (IsConnected)
            {
                Cleanup();
                Disconnected?.Invoke(reason);
            }
        }

        private void Cleanup()
        {
            IsConnected = false;
            tcpClient?.Close();
            tcpClient = null;
            reader = null;
            writer = null;
        }
    }
}