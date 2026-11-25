namespace ChatApp
{
    // Uygulama açılınca çıkan pencere: sunucu mu, istemci mi açılacak?
    public class StartForm : Form
    {
        private readonly Label lblTitle = new Label();
        private readonly Button btnServer = new Button();
        private readonly Button btnClient = new Button();
        private readonly Label lblHint = new Label();

        public StartForm()
        {
            Text = "Chat Uygulaması - YMH219";
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoSize = true;                                // pencere boyutu içeriğe göre ayarlansın
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Kontrolleri yukarıdan aşağıya dizen panel
            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.FlowDirection = FlowDirection.TopDown;
            panel.WrapContents = false;
            panel.AutoSize = true;
            panel.Padding = new Padding(20);

            lblTitle.Text = "Hangi pencereyi açmak istiyorsunuz?";
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblTitle.Margin = new Padding(0, 0, 0, 12);

            SetupButton(btnServer, "Sunucu Aç", BtnServer_Click);
            SetupButton(btnClient, "İstemci Aç", BtnClient_Click);

            lblHint.Text = "Birden fazla istemci penceresi açabilirsiniz.";
            lblHint.AutoSize = true;
            lblHint.ForeColor = Color.Gray;
            lblHint.Margin = new Padding(0, 8, 0, 0);

            panel.Controls.AddRange(new Control[] { lblTitle, btnServer, btnClient, lblHint });
            Controls.Add(panel);
        }

        private void SetupButton(Button button, string text, EventHandler onClick)
        {
            button.Text = text;
            button.Size = new Size(300, 45);
            button.Margin = new Padding(0, 0, 0, 8);
            button.Click += onClick;
        }

        private void BtnServer_Click(object? sender, EventArgs e)
        {
            new ServerForm().Show();
        }

        private void BtnClient_Click(object? sender, EventArgs e)
        {
            // İstemci penceresi bir sonraki adımda eklenecek
            MessageBox.Show("İstemci penceresi henüz eklenmedi.", "Bilgi");
        }
    }
}