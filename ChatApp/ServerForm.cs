using System.Net;
using System.Net.Sockets;

namespace ChatApp
{
    // Sunucuyu başlatıp durduran ve bağlı istemcileri listeleyen pencere
    public class ServerForm : Form
    {
        private readonly ChatServer server = new ChatServer();

        private readonly NumericUpDown numPort = new NumericUpDown();
        private readonly Button btnStart = new Button();
        private readonly Button btnStop = new Button();
        private readonly Label lblStatus = new Label();
        private readonly ListView lvClients = new ListView();
        private readonly ListBox lstLog = new ListBox();
        private readonly Label lblIp = new Label();

        public ServerForm()
        {
            BuildUi();
            server.LogMessage += Server_LogMessage;
            server.ClientsChanged += Server_ClientsChanged;
            UpdateUi();
        }

        private void BuildUi()
        {
            Text = "Chat Sunucusu - YMH219";
            ClientSize = new Size(620, 520);
            MinimumSize = new Size(560, 450);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            Padding = new Padding(8);

            // Üst çubuk: port, düğmeler, durum
            FlowLayoutPanel topBar = new FlowLayoutPanel();
            topBar.Dock = DockStyle.Top;
            topBar.AutoSize = true;
            topBar.WrapContents = false;
            topBar.Padding = new Padding(0, 0, 0, 6);

            Label lblPort = new Label { Text = "Port:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) };

            numPort.Minimum = 1024;
            numPort.Maximum = 65535;
            numPort.Value = 5000;
            numPort.Width = 90;
            numPort.Margin = new Padding(0, 5, 12, 0);

            SetupButton(btnStart, "Sunucuyu Başlat", BtnStart_Click);
            SetupButton(btnStop, "Durdur", BtnStop_Click);

            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblStatus.Margin = new Padding(12, 8, 0, 0);

            topBar.Controls.AddRange(new Control[] { lblPort, numPort, btnStart, btnStop, lblStatus });

            // Bağlı istemciler tablosu
            GroupBox grpClients = new GroupBox { Text = "Bağlı İstemciler", Dock = DockStyle.Top, Height = 200 };
            lvClients.Dock = DockStyle.Fill;
            lvClients.View = View.Details;
            lvClients.FullRowSelect = true;
            lvClients.GridLines = true;
            lvClients.Columns.Add("Kullanıcı Adı", 160);
            lvClients.Columns.Add("Adres (IP:Port)", 190);
            lvClients.Columns.Add("Bağlanma Zamanı", 160);
            grpClients.Controls.Add(lvClients);

            // Sunucu günlüğü
            GroupBox grpLog = new GroupBox { Text = "Sunucu Günlüğü", Dock = DockStyle.Fill };
            lstLog.Dock = DockStyle.Fill;
            lstLog.IntegralHeight = false;
            lstLog.Font = new Font("Consolas", 10);
            grpLog.Controls.Add(lstLog);

            // Alt bilgi: aynı ağdaki istemcilerin bağlanacağı IP adresleri
            lblIp.Dock = DockStyle.Bottom;
            lblIp.AutoSize = true;
            lblIp.ForeColor = Color.Gray;
            lblIp.Padding = new Padding(0, 6, 0, 0);
            lblIp.Text = "Bu bilgisayarın yerel IP adresi: " + GetLocalIpAddresses();

            Controls.Add(grpLog);
            Controls.Add(grpClients);
            Controls.Add(topBar);
            Controls.Add(lblIp);
            grpLog.BringToFront();   // Fill olan kontrol en son yerleşsin

            FormClosing += ServerForm_FormClosing;
        }

        private void SetupButton(Button button, string text, EventHandler onClick)
        {
            button.Text = text;
            button.AutoSize = true;
            button.MinimumSize = new Size(100, 34);
            button.Click += onClick;
        }

        private void UpdateUi()
        {
            bool running = server.IsRunning;

            btnStart.Enabled = !running;
            numPort.Enabled = !running;
            btnStop.Enabled = running;

            lblStatus.Text = running ? $"● Çalışıyor ({server.Clients.Count} istemci)" : "● Durduruldu";
            lblStatus.ForeColor = running ? Color.ForestGreen : Color.Crimson;
        }

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            try
            {
                server.Start((int)numPort.Value);
            }
            catch (SocketException ex)
            {
                MessageBox.Show(
                    $"Sunucu başlatılamadı. {numPort.Value} portu başka bir program tarafından kullanılıyor olabilir.\n\nAyrıntı: {ex.Message}",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateUi();
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            server.Stop();
            UpdateUi();
        }

        private void Server_LogMessage(string line)
        {
            lstLog.Items.Add(line);
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }

        // Bağlı istemci tablosunu yeniden doldurur
        private void Server_ClientsChanged()
        {
            lvClients.Items.Clear();

            foreach (ClientConnection client in server.Clients)
            {
                ListViewItem item = new ListViewItem(client.UserName);
                item.SubItems.Add(client.EndPoint);
                item.SubItems.Add(client.ConnectedAt.ToString("HH:mm:ss"));
                lvClients.Items.Add(item);
            }

            UpdateUi();
        }

        private void ServerForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            server.Stop();   // pencere kapanınca tüm bağlantıları kapat
        }

        private static string GetLocalIpAddresses()
        {
            try
            {
                IEnumerable<string> ips = Dns.GetHostAddresses(Dns.GetHostName())
                    .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.ToString());

                string text = string.Join(", ", ips);
                return text.Length > 0 ? text : "bulunamadı";
            }
            catch (SocketException)
            {
                return "bulunamadı";
            }
        }
    }
}