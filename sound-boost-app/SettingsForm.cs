using System;
using System.Windows.Forms;
using Microsoft.Win32;
using sound_boost_app.Properties;

namespace MicrophoneBoosterApp
{
    public partial class SettingsForm : Form
    {
        private CheckBox autorunCheckBox;
        private ComboBox closeBehaviorComboBox;
        private Button backButton;

        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.autorunCheckBox = new CheckBox();
            this.closeBehaviorComboBox = new ComboBox();
            this.backButton = new Button();

            // Autorun CheckBox
            this.autorunCheckBox.Location = new System.Drawing.Point(20, 20);
            this.autorunCheckBox.Name = "autorunCheckBox";
            this.autorunCheckBox.Size = new System.Drawing.Size(200, 24);
            this.autorunCheckBox.Text = "Enable Autorun";
            this.Controls.Add(this.autorunCheckBox);

            // Close Behavior ComboBox
            this.closeBehaviorComboBox.Location = new System.Drawing.Point(20, 60);
            this.closeBehaviorComboBox.Name = "closeBehaviorComboBox";
            this.closeBehaviorComboBox.Size = new System.Drawing.Size(200, 24);
            this.closeBehaviorComboBox.Items.AddRange(new object[] {
                "Close Application",
                "Minimize to Tray"
            });
            this.Controls.Add(this.closeBehaviorComboBox);

            // Back Button
            this.backButton.Location = new System.Drawing.Point(20, 100);
            this.backButton.Name = "backButton";
            this.backButton.Size = new System.Drawing.Size(75, 23);
            this.backButton.Text = "Back";
            this.backButton.Click += new EventHandler(this.BackButton_Click);
            this.Controls.Add(this.backButton);

            // Settings Form
            this.ClientSize = new System.Drawing.Size(250, 150);
            this.Name = "SettingsForm";
            this.Text = "Settings";
        }

        private void LoadSettings()
        {
            // Load settings from Properties.Settings.Default
            this.autorunCheckBox.Checked = Settings.Default.AutoRun;
            this.closeBehaviorComboBox.SelectedIndex = Settings.Default.CloseToTray ? 1 : 0;
        }

        private void SaveSettings()
        {
            // Save settings to Properties.Settings.Default
            Settings.Default.AutoRun = this.autorunCheckBox.Checked;
            Settings.Default.CloseToTray = this.closeBehaviorComboBox.SelectedIndex == 1;
            Settings.Default.Save();

            // Apply autorun setting
            SetAutorun(this.autorunCheckBox.Checked);
        }

        private void SetAutorun(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (enabled)
                {
                    key.SetValue("MicrophoneBoosterApp", Application.ExecutablePath);
                }
                else
                {
                    key.DeleteValue("MicrophoneBoosterApp", false);
                }
            }
        }

        private void BackButton_Click(object sender, EventArgs e)
        {
            SaveSettings();
            this.Close();
        }
    }
}
