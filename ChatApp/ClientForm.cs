using System.Net.Sockets;

namespace ChatApp
{
    // Kullanıcının sunucuya bağlanıp mesajlaştığı pencere
    public class ClientForm : Form
    {
        private readonly ChatClient client = new ChatClient();
        private readonly ChatHistoryService historyService = new ChatHistoryService();

        private readonly TextBox txtHost = new TextBox();
        private readonly NumericUpDown numPort = new NumericUpDown();
        private readonly TextBox txtUserName = new TextBox();
        private readonly Button btnConnect = new Button();
        private readonly Button btnDisconnect = new Button();
        private readonly Button btnSaveHistory = new Button();
        private readonly Label lblStatus = new Label();
        private readonly ListBox lstMessages = new ListBox();
        private readonly TextBox txtMessage = new TextBox();
        private readonly Button btnSend = new Button();

        public ClientForm()
        {
            BuildUi();
            client.MessageReceived += Client_MessageReceived;
            client.Disconnected += Client_Disconnected;
            UpdateUi();
        }

        private void BuildUi()
        {
            Text = "Chat İstemcisi - YMH219";
            ClientSize = new Size(640, 500);
            MinimumSize = new Size(600, 420);
            StartPosition = FormStartPosition.WindowsDefaultLocation;   // pencereler üst üste binmesin
            Font = new Font("Segoe UI", 10);
            Padding = new Padding(8);

            // 1. satır: bağlantı bilgileri
            FlowLayoutPanel rowSettings = CreateRow();
            txtHost.Text = "127.0.0.1";   // aynı bilgisayardaki sunucu
            txtHost.Width = 130;
            numPort.Minimum = 1024;
            numPort.Maximum = 65535;
            numPort.Value = 5000;
            numPort.Width = 80;
            txtUserName.Width = 140;
            txtUserName.MaxLength = 20;
            rowSettings.Controls.AddRange(new Control[]
            {
                CreateLabel("Sunucu IP:"), txtHost,
                CreateLabel("Port:"), numPort,
                CreateLabel("Kullanıcı adı:"), txtUserName
            });

            // 2. satır: düğmeler ve durum
            FlowLayoutPanel rowButtons = CreateRow();
            SetupButton(btnConnect, "Bağlan", BtnConnect_Click);
            SetupButton(btnDisconnect, "Bağlantıyı Kes", BtnDisconnect_Click);
            SetupButton(btnSaveHistory, "Geçmişi Kaydet", BtnSaveHistory_Click);
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblStatus.Margin = new Padding(12, 8, 0, 0);
            rowButtons.Controls.AddRange(new Control[] { btnConnect, btnDisconnect, btnSaveHistory, lblStatus });

            // Mesaj listesi
            lstMessages.Dock = DockStyle.Fill;
            lstMessages.IntegralHeight = false;
            lstMessages.HorizontalScrollbar = true;
            lstMessages.Font = new Font("Consolas", 10);

            // Alt satır: mesaj yazma kutusu + Gönder
            TableLayoutPanel rowSend = new TableLayoutPanel();
            rowSend.Dock = DockStyle.Bottom;
            rowSend.AutoSize = true;
            rowSend.ColumnCount = 2;
            rowSend.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            rowSend.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            rowSend.Padding = new Padding(0, 6, 0, 0);
            txtMessage.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtMessage.MaxLength = 500;
            SetupButton(btnSend, "Gönder", BtnSend_Click);
            rowSend.Controls.Add(txtMessage, 0, 0);
            rowSend.Controls.Add(btnSend, 1, 0);

            Controls.Add(lstMessages);
            Controls.Add(rowButtons);
            Controls.Add(rowSettings);
            Controls.Add(rowSend);
            lstMessages.BringToFront();   // Fill olan kontrol en son yerleşsin

            AcceptButton = btnSend;   // Enter tuşu = Gönder
            FormClosing += ClientForm_FormClosing;
        }

        private static FlowLayoutPanel CreateRow()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = false,
                Padding = new Padding(0, 0, 0, 4)
            };
        }

        private static Label CreateLabel(string text)
        {
            return new Label { Text = text, AutoSize = true, Margin = new Padding(0, 7, 4, 0) };
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
            bool connected = client.IsConnected;

            txtHost.Enabled = !connected;
            numPort.Enabled = !connected;
            txtUserName.Enabled = !connected;
            btnConnect.Enabled = !connected;
            btnDisconnect.Enabled = connected;
            txtMessage.Enabled = connected;
            btnSend.Enabled = connected;
            btnSaveHistory.Enabled = lstMessages.Items.Count > 0;

            lblStatus.Text = connected ? "● Bağlı" : "● Bağlı değil";
            lblStatus.ForeColor = connected ? Color.ForestGreen : Color.Crimson;
            Text = connected ? $"Chat İstemcisi - {client.UserName}" : "Chat İstemcisi - YMH219";
        }

        private void AddLine(string line)
        {
            lstMessages.Items.Add(line);
            lstMessages.TopIndex = lstMessages.Items.Count - 1;
            btnSaveHistory.Enabled = true;
        }

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            string host = txtHost.Text.Trim();
            string userName = txtUserName.Text.Trim();

            if (host.Length == 0 || userName.Length == 0)
            {
                MessageBox.Show("Lütfen sunucu IP adresini ve kullanıcı adını girin.", "Eksik bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnConnect.Enabled = false;
            lblStatus.Text = "● Bağlanıyor...";
            lblStatus.ForeColor = Color.DarkOrange;

            try
            {
                await client.ConnectAsync(host, (int)numPort.Value, userName);
                AddLine($"[{DateTime.Now:HH:mm:ss}] *** {host}:{numPort.Value} adresine bağlanıldı.");
                txtMessage.Focus();
            }
            catch (SocketException)
            {
                MessageBox.Show("Sunucuya bağlanılamadı.\nSunucunun çalıştığından ve IP/port bilgilerinin doğru olduğundan emin olun.",
                    "Bağlantı hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Bağlantı zaman aşımına uğradı (5 saniye). Sunucu yanıt vermiyor.",
                    "Bağlantı hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UpdateUi();
        }

        private void BtnDisconnect_Click(object? sender, EventArgs e)
        {
            client.Disconnect();
            AddLine($"[{DateTime.Now:HH:mm:ss}] *** Bağlantı kesildi.");
            UpdateUi();
        }

        private async void BtnSend_Click(object? sender, EventArgs e)
        {
            string text = txtMessage.Text.Trim();
            if (text.Length == 0 || !client.IsConnected)
                return;

            try
            {
                await client.SendAsync(text);
                AddLine(ChatMessage.CreateChat(client.UserName + " (sen)", text).ToDisplayString());
                txtMessage.Clear();
                txtMessage.Focus();
            }
            catch (Exception ex) when (ex is IOException || ex is ObjectDisposedException || ex is InvalidOperationException)
            {
                MessageBox.Show("Mesaj gönderilemedi. Sunucu bağlantısı kopmuş olabilir.",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                client.Disconnect();
                UpdateUi();
            }
        }

        private void BtnSaveHistory_Click(object? sender, EventArgs e)
        {
            string userName = client.UserName.Length > 0 ? client.UserName : txtUserName.Text.Trim();

            using SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Mesaj Geçmişini Kaydet";
            dialog.Filter = "Metin dosyası (*.txt)|*.txt";
            dialog.FileName = $"sohbet_{userName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                historyService.Save(dialog.FileName, userName, lstMessages.Items.Cast<string>());
                MessageBox.Show($"{lstMessages.Items.Count} satır kaydedildi:\n{dialog.FileName}",
                    "Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show("Dosya kaydedilemedi:\n" + ex.Message,
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Client_MessageReceived(ChatMessage message)
        {
            AddLine(message.ToDisplayString());
        }

        private void Client_Disconnected(string reason)
        {
            AddLine($"[{DateTime.Now:HH:mm:ss}] *** {reason}");
            UpdateUi();
        }

        private void ClientForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            client.Disconnect();   // pencere kapanınca sunucuya ayrıldığını bildir
        }
    }
}