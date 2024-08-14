using System;
using System.Drawing;
using System.Windows.Forms;

namespace sound_boost_app
{
    public class AppItem : UserControl
    {
        public event EventHandler DeleteButtonClick;
        public event EventHandler AppNameClick;

        private Label appNameLabel;
        private Label configLabel;
        private Button deleteButton;

        public string AppName { get; private set; }
        public string AppId { get; set; }
        public string Microphone { get; set; }
        public string OutputDevice { get; set; }
        public int BoostValue { get; set; }

        public AppItem(string appName)
        {
            AppName = appName;
            InitializeComponent();
            appNameLabel.Text = appName;
        }

        private void InitializeComponent()
        {
            this.appNameLabel = new Label();
            this.configLabel = new Label();
            this.deleteButton = new Button();

            // App Name Label
            this.appNameLabel.Location = new Point(10, 10);
            this.appNameLabel.Size = new Size(180, 20);
            this.appNameLabel.Click += (s, e) => AppNameClick?.Invoke(this, e);
            this.appNameLabel.Cursor = Cursors.Hand;

            // Config Label
            this.configLabel.Location = new Point(10, 30);
            this.configLabel.Size = new Size(180, 40); // Adjusted height to fit two lines
            this.configLabel.Font = new Font(this.configLabel.Font.FontFamily, 8);
            this.configLabel.ForeColor = Color.Gray;

            // Delete Button
            this.deleteButton.Text = "X";
            this.deleteButton.Location = new Point(200, 10);
            this.deleteButton.Size = new Size(20, 20);
            this.deleteButton.Click += (s, e) => DeleteButtonClick?.Invoke(this, e);

            // Controls in UserControl 
            this.Controls.Add(this.appNameLabel);
            this.Controls.Add(this.configLabel);
            this.Controls.Add(this.deleteButton);

            // UserControl size 
            this.Size = new Size(230, 80);
        }

        public void UpdateConfigDisplay()
        {
            configLabel.Text = $"{Microphone} \n [ {BoostValue}% ]";
        }
    }
}