using System.Text;

namespace ChatApp
{
    // Mesaj geçmişini metin dosyasına yazmaktan sorumlu sınıf
    public class ChatHistoryService
    {
        public void Save(string filePath, string userName, IEnumerable<string> lines)
        {
            // using: iş bitince dosya otomatik kapatılır
            using StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8);

            writer.WriteLine("Chat Uygulaması - Mesaj Geçmişi");
            writer.WriteLine($"Kullanıcı: {userName}");
            writer.WriteLine($"Kaydedilme: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            writer.WriteLine(new string('-', 50));

            foreach (string line in lines)
                writer.WriteLine(line);
        }
    }
}