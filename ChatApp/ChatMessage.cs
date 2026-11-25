using System.Text.Json;

namespace ChatApp
{
    // Mesaj türleri
    public enum MessageType
    {
        Join,     // istemci kullanıcı adıyla katıldı
        Chat,     // normal sohbet mesajı
        System,   // sunucudan bilgi mesajı (X katıldı, X ayrıldı...)
        Leave     // istemci ayrılıyor
    }

    // Sunucu ile istemci arasında gidip gelen tek bir mesaj
    public class ChatMessage
    {
        public MessageType Type { get; set; }
        public string Sender { get; set; } = "";
        public string Text { get; set; } = "";
        public DateTime Time { get; set; } = DateTime.Now;

        // Hazır mesaj oluşturan yardımcı metotlar (factory method)
        public static ChatMessage CreateJoin(string userName) =>
            new ChatMessage { Type = MessageType.Join, Sender = userName };

        public static ChatMessage CreateChat(string userName, string text) =>
            new ChatMessage { Type = MessageType.Chat, Sender = userName, Text = text };

        public static ChatMessage CreateSystem(string text) =>
            new ChatMessage { Type = MessageType.System, Sender = "Sunucu", Text = text };

        public static ChatMessage CreateLeave(string userName) =>
            new ChatMessage { Type = MessageType.Leave, Sender = userName };

        // Nesneyi ağdan gönderilecek tek satırlık JSON metnine çevirir
        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        // Ağdan gelen JSON metnini nesneye çevirir; bozuk veri gelirse null döner
        public static ChatMessage? FromJson(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<ChatMessage>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // Ekranda gösterilecek hali: [16:30:05] Hatice: Merhaba
        public string ToDisplayString()
        {
            string time = Time.ToString("HH:mm:ss");

            if (Type == MessageType.Chat)
                return $"[{time}] {Sender}: {Text}";

            return $"[{time}] *** {Text}";
        }
    }
}