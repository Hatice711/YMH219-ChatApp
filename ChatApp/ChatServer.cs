using System.Net;
using System.Net.Sockets;

namespace ChatApp
{
    // Bağlantıları kabul eden ve mesajları istemciler arasında dağıtan sunucu
    public class ChatServer
    {
        private TcpListener? listener;
        private readonly List<ClientConnection> clients = new List<ClientConnection>();

        public bool IsRunning { get; private set; }
        public IReadOnlyList<ClientConnection> Clients => clients;

        // Arayüzün dinleyeceği olaylar
        public event Action<string>? LogMessage;   // günlüğe yazılacak satır
        public event Action? ClientsChanged;        // bağlı istemci listesi değişti

        public void Start(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);   // tüm ağ kartlarından bağlantı kabul et
            listener.Start();                                  // port doluysa SocketException fırlatır
            IsRunning = true;
            Log($"Sunucu {port} portunda başlatıldı.");

            _ = AcceptLoopAsync();   // bağlantı bekleme döngüsünü arka planda başlat
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            IsRunning = false;
            listener?.Stop();

            foreach (ClientConnection client in clients.ToList())
                client.Close();

            clients.Clear();
            ClientsChanged?.Invoke();
            Log("Sunucu durduruldu.");
        }

        // Yeni istemcileri sürekli bekler
        private async Task AcceptLoopAsync()
        {
            while (IsRunning)
            {
                try
                {
                    TcpClient tcpClient = await listener!.AcceptTcpClientAsync();
                    ClientConnection connection = new ClientConnection(tcpClient);
                    _ = HandleClientAsync(connection);   // her istemci kendi döngüsünde dinlenir
                }
                catch (Exception) when (!IsRunning)
                {
                    break;   // sunucu durdurulduğu için bekleme iptal oldu
                }
                catch (Exception ex)
                {
                    Log("Bağlantı kabul edilirken hata: " + ex.Message);
                }
            }
        }

        // Tek bir istemciden gelen mesajları okur
        private async Task HandleClientAsync(ClientConnection connection)
        {
            bool joined = false;

            try
            {
                while (IsRunning)
                {
                    string? line = await connection.ReceiveLineAsync();
                    if (line == null)
                        break;   // istemci bağlantıyı kapattı

                    ChatMessage? message = ChatMessage.FromJson(line);
                    if (message == null)
                        continue;   // bozuk veri geldi, yok say

                    switch (message.Type)
                    {
                        case MessageType.Join:
                            bool nameTaken = clients.Any(c =>
                                c.UserName.Equals(message.Sender, StringComparison.OrdinalIgnoreCase));
                            if (nameTaken)
                            {
                                await connection.SendAsync(ChatMessage.CreateSystem(
                                    $"'{message.Sender}' kullanıcı adı zaten kullanılıyor. Başka bir ad seçin."));
                                return;   // bağlantıyı kapat (finally çalışır)
                            }

                            connection.UserName = message.Sender;
                            clients.Add(connection);
                            joined = true;
                            ClientsChanged?.Invoke();
                            Log($"{connection.UserName} bağlandı ({connection.EndPoint}).");

                            await connection.SendAsync(ChatMessage.CreateSystem(
                                $"Sohbete hoş geldin, {connection.UserName}! Çevrimiçi: {clients.Count} kişi"));
                            await BroadcastAsync(ChatMessage.CreateSystem($"{connection.UserName} sohbete katıldı."), connection);
                            break;

                        case MessageType.Chat:
                            if (!joined)
                                break;   // katılmadan mesaj gönderilemez
                            message.Sender = connection.UserName;
                            Log($"{connection.UserName}: {message.Text}");
                            await BroadcastAsync(message, connection);
                            break;

                        case MessageType.Leave:
                            return;   // istemci ayrılıyor (finally çalışır)
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is SocketException || ex is ObjectDisposedException)
            {
                // bağlantı beklenmedik şekilde koptu; aşağıda temizlenecek
            }
            finally
            {
                connection.Close();

                if (clients.Remove(connection))
                {
                    ClientsChanged?.Invoke();
                    Log($"{connection.UserName} ayrıldı.");
                    if (IsRunning)
                        await BroadcastAsync(ChatMessage.CreateSystem($"{connection.UserName} sohbetten ayrıldı."), null);
                }
            }
        }

        // Mesajı (gönderen hariç) tüm istemcilere iletir
        private async Task BroadcastAsync(ChatMessage message, ClientConnection? except)
        {
            foreach (ClientConnection client in clients.ToList())
            {
                if (client == except)
                    continue;

                try
                {
                    await client.SendAsync(message);
                }
                catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException || ex is InvalidOperationException)
                {
                    // bu istemcinin bağlantısı kopmuş; kendi döngüsü onu temizleyecek
                }
            }
        }

        private void Log(string text)
        {
            LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {text}");
        }
    }
}