using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using Microsoft.Win32;
using sound_boost_app.Properties;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using sound_boost_app;


namespace MicrophoneBoosterApp
{
    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private Panel appListPanel;
        private Button settingsButton;
        private Dictionary<string, AppConfig> appConfigs = new Dictionary<string, AppConfig>();
        private IWaveIn waveIn;
        private WaveOutEvent waveOut;
        private BufferedWaveProvider bufferedWaveProvider;
        private VolumeSampleProvider volumeProvider;
        private float gainFactor = 1.0f;
        private ComboBox microphoneDropdown;
        private ComboBox outputDeviceDropdown;
        public ComboBox appDropdown;
        private TrackBar boostSlider;
        private Label microphoneLabel;
        private Label outputDeviceLabel;
        private Label appLabel;
        private Label boostLabel;
        private Label boostValueLabel;
        private Timer appUpdateTimer;
        private bool isUpdatingAppDropdown = false;
        private HashSet<string> loadedAppConfigs = new HashSet<string>();

        public MainForm()
        {
            InitializeComponent();
            LoadSettings();
            SetupSystemTray();
            PopulateMicrophoneDropdown();
            PopulateOutputDeviceDropdown();
            PopulateAppDropdown();
            LoadAllAppConfigs();

            // Set up the timer to check for new apps every 5 seconds
            appUpdateTimer = new Timer();
            appUpdateTimer.Interval = 5000;
            appUpdateTimer.Tick += new EventHandler(UpdateAppDropdown);
            appUpdateTimer.Start();

            // Subscribe to the SessionEnding event to handle system shutdown/logoff
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;

            this.Icon = Resources.soundboosticon;
        }

        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            // Stop the timer when the session is ending to prevent exceptions
            if (appUpdateTimer != null)
            {
                appUpdateTimer.Stop();
                appUpdateTimer.Dispose();
                appUpdateTimer = null;
            }
        }

        private void UpdateAppDropdown(object sender, EventArgs e)
        {
            isUpdatingAppDropdown = true; // Set the flag

            // Preserve the currently selected app
            string selectedApp = appDropdown.SelectedItem?.ToString();

            // Update the app dropdown
            PopulateAppDropdown();

            // Try to re-select the previously selected app
            if (!string.IsNullOrEmpty(selectedApp))
            {
                appDropdown.SelectedItem = selectedApp;
            }

            isUpdatingAppDropdown = false; // Reset the flag
        }

        private void InitializeComponent()
        {
            this.settingsButton = new Button();
            this.microphoneDropdown = new ComboBox();
            this.outputDeviceDropdown = new ComboBox();
            this.appDropdown = new ComboBox();
            this.boostSlider = new TrackBar();
            this.boostValueLabel = new Label();
            this.appListPanel = new Panel();
            this.microphoneLabel = new Label();
            this.outputDeviceLabel = new Label();
            this.boostLabel = new Label();

            // Main Form
            this.ClientSize = new Size(800, 600);
            this.Name = "MainForm";
            this.Text = "Microphone Booster App";
            this.Load += new EventHandler(this.MainForm_Load);
            this.FormClosing += new FormClosingEventHandler(this.MainForm_FormClosing);

            // Settings Button
            this.settingsButton.Location = new Point(10, 10);  // Adjust position
            this.settingsButton.Name = "settingsButton";
            this.settingsButton.Size = new Size(29, 23);
            this.settingsButton.TabIndex = 0;
            this.settingsButton.Text = "⚙️";
            this.settingsButton.Click += new EventHandler(this.SettingsButton_Click);
            this.Controls.Add(this.settingsButton);

            // Microphone Label
            this.microphoneLabel.Location = new Point(330, 10);  // Adjust position
            this.microphoneLabel.Size = new Size(100, 20);
            this.microphoneLabel.Text = "Input Microphone:";
            this.Controls.Add(this.microphoneLabel);

            // Microphone Dropdown
            this.microphoneDropdown.Location = new Point(330, 30);  // Adjust position
            this.microphoneDropdown.Name = "microphoneDropdown";
            this.microphoneDropdown.Size = new Size(316, 21);
            this.microphoneDropdown.SelectedIndex = -1; // Blank by default
            this.Controls.Add(this.microphoneDropdown);

            // Output Device Label
            this.outputDeviceLabel.Location = new Point(330, 60);  // Position below microphone
            this.outputDeviceLabel.Size = new Size(100, 20);
            this.outputDeviceLabel.Text = "Output Device:";
            this.Controls.Add(this.outputDeviceLabel);

            // Output Device Dropdown
            this.outputDeviceDropdown.Location = new Point(330, 80);  // Position below microphone
            this.outputDeviceDropdown.Size = new Size(316, 21);
            this.Controls.Add(this.outputDeviceDropdown);

            // App Label
            this.appLabel = new Label(); // Add this line to instantiate the appLabel
            this.appLabel.Location = new Point(330, 110);  // Adjust position
            this.appLabel.Size = new Size(100, 20);
            this.appLabel.Text = "Application:";
            this.Controls.Add(this.appLabel);

            // App Dropdown
            this.appDropdown.Location = new Point(330, 130);  // Adjust position
            this.appDropdown.Name = "appDropdown";
            this.appDropdown.Size = new Size(316, 21);
            this.appDropdown.SelectedIndex = -1; // Blank by default
            this.appDropdown.SelectedIndexChanged += new EventHandler(this.AppDropdown_SelectedIndexChanged);
            this.Controls.Add(this.appDropdown);

            // Boost Label
            this.boostLabel.Location = new Point(330, 160);  // Adjust position
            this.boostLabel.Size = new Size(100, 20);
            this.boostLabel.Text = "Boost:";
            this.Controls.Add(this.boostLabel);

            // Boost Slider
            this.boostSlider.Location = new Point(330, 180);  // Adjust position
            this.boostSlider.Maximum = 1000;
            this.boostSlider.Minimum = 100;
            this.boostSlider.Name = "boostSlider";
            this.boostSlider.Size = new Size(316, 45);
            this.boostSlider.Value = 100;
            this.boostSlider.ValueChanged += new EventHandler(this.BoostSlider_ValueChanged);
            this.Controls.Add(this.boostSlider);

            // Boost Value Label
            this.boostValueLabel.Location = new Point(660, 180);  // Adjust position
            this.boostValueLabel.Name = "boostValueLabel";
            this.boostValueLabel.Size = new Size(50, 20);
            this.Controls.Add(this.boostValueLabel);

            // App List Panel
            this.appListPanel.Location = new Point(10, 50);  // Adjust position
            this.appListPanel.Name = "appListPanel";
            this.appListPanel.Size = new Size(300, 500);
            this.appListPanel.AutoScroll = true; // Enable scrolling if items exceed panel size
            this.Controls.Add(this.appListPanel);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Location = Settings.Default.WindowLocation;
            LoadAllAppConfigs();
            PopulateMicrophoneDropdown();
            PopulateOutputDeviceDropdown();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            waveIn?.StopRecording();
            waveIn?.Dispose();
            waveIn = null;

            waveOut?.Stop();
            waveOut?.Dispose();
            waveOut = null;

            Settings.Default.WindowLocation = this.Location;
            Settings.Default.Save();
        }

        private void PopulateMicrophoneDropdown()
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            microphoneDropdown.Items.Clear();
            foreach (var device in devices)
            {
                microphoneDropdown.Items.Add(device.FriendlyName);
            }
        }

        private void PopulateOutputDeviceDropdown()
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            outputDeviceDropdown.Items.Clear();
            foreach (var device in devices)
            {
                outputDeviceDropdown.Items.Add(device.FriendlyName);
            }
        }

        private void PopulateAppDropdown()
        {
            var processes = Process.GetProcesses();
            appDropdown.Items.Clear();

            foreach (var process in processes)
            {
                if (!string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    appDropdown.Items.Add(process.MainWindowTitle);
                }
            }

            appDropdown.SelectedIndex = -1; // Set the appDropdown to blank by default
        }

        private void LoadSettings()
        {
            // Load saved settings from Properties.Settings.Default
            bool closeToTray = Settings.Default.CloseToTray;
            bool autoRun = Settings.Default.AutoRun;

            // Apply these settings as needed in your application
        }

        private void SetupSystemTray()
        {
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show", null, ShowApp);
            trayMenu.Items.Add("Exit", null, ExitApp);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "[RGB.DEV] \n Sound Booster App";
            trayIcon.Icon = Resources.soundboosticon;
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;

            trayIcon.DoubleClick += (sender, args) => ShowApp(sender, args);
        }

        private void SetAutorun(bool enabled)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (enabled)
            {
                key.SetValue("MicrophoneBoosterApp", Application.ExecutablePath);
            }
            else
            {
                key.DeleteValue("MicrophoneBoosterApp", false);
            }
        }

        private void SetCloseToTray(bool enabled)
        {
            Settings.Default.CloseToTray = enabled;
            Settings.Default.Save();
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm();
            settingsForm.Owner = this;
            settingsForm.ShowDialog();
        }

        private void AppDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Only proceed if the update is not programmatic
            if (!isUpdatingAppDropdown)
            {
                if (appDropdown.SelectedIndex == -1) return;

                if (microphoneDropdown.SelectedIndex == -1 || outputDeviceDropdown.SelectedIndex == -1)
                {
                    appDropdown.SelectedIndex = -1;
                    MessageBox.Show("Please select both input and output microphones first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string selectedApp = appDropdown.SelectedItem.ToString();

                // Get the Unique App Identifier 
                string appId = Program.GetUniqueAppIdentifier(selectedApp);

                AddAppToList(selectedApp, appId);

                var boostValue = boostSlider.Value;
                var config = new AppConfig
                {
                    Microphone = microphoneDropdown.SelectedItem?.ToString(),
                    OutputDevice = outputDeviceDropdown.SelectedItem?.ToString(),
                    BoostValue = boostValue
                };

                SaveAppConfig(appId, config);
            }
        }

        private int GetProcessIdFromAppName(string appName)
        {
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                if (process.MainWindowTitle == appName)
                {
                    return process.Id;
                }
            }
            
            MessageBox.Show($"Process '{appName}' not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return -1;
        }

        private void AddAppToList(string appName, string appId)
        {
            if (appConfigs.ContainsKey(appId))
            {
                return; // Do not add duplicates 
            }

            var appItem = new AppItem(appName)
            {
                OutputDevice = outputDeviceDropdown.SelectedItem?.ToString(),
                AppId = appId
            };
            appItem.DeleteButtonClick += (s, e) => RemoveAppFromList(appItem);
            appItem.AppNameClick += (s, e) => SelectAppForConfiguration(appItem);
            appItem.Location = new Point(0, appListPanel.Controls.Count * appItem.Height);
            appListPanel.Controls.Add(appItem);

            // Create/load config for the app:
            if (!appConfigs.ContainsKey(appId))
            {
                var config = new AppConfig
                {
                    Microphone = microphoneDropdown.SelectedItem?.ToString(),
                    OutputDevice = outputDeviceDropdown.SelectedItem?.ToString(),
                    BoostValue = 100,
                    AppName = appName
                };
                appConfigs[appId] = config;
                SaveAppConfig(appId, config);
            }

            // Load and apply the config: 
            var appConfig = appConfigs[appId];
            appItem.Microphone = appConfig.Microphone;
            appItem.BoostValue = appConfig.BoostValue;
            appItem.UpdateConfigDisplay();
        }


        private void RemoveAppFromList(AppItem appItem)
        {
            appListPanel.SuspendLayout();

            // Get appId from the appItem (You'll need to store appId in AppItem)
            string appId = appItem.AppId; // Assuming you added 'AppId' property to AppItem

            // Remove config and appItem 
            appConfigs.Remove(appId);
            DeleteAppConfig(appId);

            // Remove from UI 
            appListPanel.Controls.Remove(appItem);

            // Reposition the remaining items
            for (int i = 0; i < appListPanel.Controls.Count; i++)
            {
                AppItem item = (AppItem)appListPanel.Controls[i];
                item.Location = new Point(0, i * item.Height);
            }

            appListPanel.ResumeLayout();
            appListPanel.Refresh();
        }

        public void AddAppToListFromConfig(AppConfig config, string appId)
        {
            // Duplicate Check (use appId)
            if (appConfigs.ContainsKey(appId))
            {
                return;
            }

            // Create AppItem using appName from loaded config:
            var appItem = new AppItem(config.AppName)
            {
                Microphone = config.Microphone,
                OutputDevice = config.OutputDevice,
                BoostValue = config.BoostValue,
                AppId = appId
            };

            // Position appItem:
            appItem.Location = new Point(0, appListPanel.Controls.Count * appItem.Height);

            // Add event handlers:
            appItem.DeleteButtonClick += (s, e) => RemoveAppFromList(appItem);
            appItem.AppNameClick += (s, e) => SelectAppForConfiguration(appItem);

            // Update the config display: 
            appItem.UpdateConfigDisplay();

            // Add appItem to appListPanel:
            appListPanel.Controls.Add(appItem);

            // Add to appConfigs (using appId as the key)
            appConfigs[appId] = config;
        }

        private void SelectAppForConfiguration(AppItem appItem)
        {
            appListPanel.Controls.OfType<AppItem>()
                            .ToList()
                            .ForEach(item => item.BackColor = SystemColors.Control);
            appItem.BackColor = Color.LightBlue;

            // Get appId from appItem (Make sure AppItem has the AppId property)
            string appId = appItem.AppId;

            if (appConfigs.ContainsKey(appId)) // Check if the config exists 
            {
                AppConfig config = appConfigs[appId]; // Retrieve directly from dictionary 

                if (config != null)
                {
                    // Update boost slider: 
                    boostSlider.Value = config.BoostValue;
                    boostValueLabel.Text = $"{config.BoostValue}%";

                    // Set microphone selections (using your helper methods):
                    SelectMicrophoneInDropdown(config.Microphone);
                    SelectOutputDeviceInDropdown(config.OutputDevice);

                    // (Optional)
                    // appItem.Microphone = config.Microphone;
                    // appItem.BoostValue = config.BoostValue; 
                    // appItem.UpdateConfigDisplay(); 
                }
                else
                {
                    // Configuration not found - Handle this appropriately
                    Console.WriteLine($"Error: Configuration for appId '{appId}' not found!");
                }
            }
            else
            {
                // Configuration does not exist in appConfigs - handle this case 
                Console.WriteLine($"Error: Configuration for appId '{appId}' does not exist in 'appConfigs'!");
            }
        }

        private AppConfig LoadAppConfig(string appName)
        {
            string configPath = Program.GetAppConfigPath();
            if (File.Exists(configPath))
            {
                var config = JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(configPath));
                appConfigs[appName] = config;
                return config;
            }
            return null;
        }

        private void DeleteAppConfig(string appId)
        {
            string configPath = Program.GetAppConfigPath(appId);

            if (File.Exists(configPath))
            {
                try
                {
                    File.Delete(configPath);
                }
                catch (Exception ex)
                {
                    // (Optional: Handle exceptions) 
                    MessageBox.Show($"Error deleting configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        private string GetAppConfigPath(string appName)
        {
            // Remove invalid characters from the app name
            string sanitizedAppName = string.Join("_", appName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(Application.StartupPath, $"{sanitizedAppName}.json");
        }

        private int GetAppBoostConfig(string appName)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\MicrophoneBoosterApp", true))
            {
                if (key != null)
                {
                    object value = key.GetValue($"{appName}_Boost");
                    if (value != null)
                    {
                        return (int)value;
                    }
                }
            }
            return 100; // Default boost value
        }

        private void SaveAppBoostConfig(string appName, int boostValue)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\MicrophoneBoosterApp", true);
            key.SetValue($"{appName}_Boost", boostValue);
            key.Close();
        }

        private void DeleteAppBoostConfig(string appName)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\MicrophoneBoosterApp", true))
            {
                if (key != null && key.GetValue($"{appName}_Boost") != null)
                {
                    key.DeleteValue($"{appName}_Boost", false);
                }
            }
        }

        private void BoostSlider_ValueChanged(object sender, EventArgs e)
        {
            var selectedApp = appListPanel.Controls.OfType<AppItem>()
                                          .FirstOrDefault(item => item.BackColor == Color.LightBlue);

            if (selectedApp != null)
            {
                int boostValue = boostSlider.Value;
                boostValueLabel.Text = $"{boostValue}%";

                // GET appId FROM selectedApp:
                string appId = selectedApp.AppId;

                // CREATE CONFIG OBJECT:
                var config = new AppConfig
                {
                    Microphone = microphoneDropdown.SelectedItem?.ToString(),
                    OutputDevice = outputDeviceDropdown.SelectedItem?.ToString(),
                    BoostValue = boostValue,
                    AppName = selectedApp.AppName // Set the AppName in the config
                };

                // SAVE CONFIG (using appId):
                SaveAppConfig(appId, config);

                // UPDATE AppItem AND APPLY BOOST:
                selectedApp.Microphone = config.Microphone;
                selectedApp.OutputDevice = config.OutputDevice;
                selectedApp.BoostValue = boostValue;
                selectedApp.UpdateConfigDisplay();

                ApplyMicrophoneBoost(config.Microphone, config.BoostValue);
            }
        }

        private void LoadAllAppConfigs()
        {
            string cfgsFolderPath = Path.Combine(Application.StartupPath, "cfgs");

            // Check if "cfgs" folder exists:
            if (!Directory.Exists(cfgsFolderPath))
            {
                return;
            }

            // Get all config files in the "cfgs" folder:
            string[] configFiles = Directory.GetFiles(cfgsFolderPath, "*.json");

            foreach (string configFile in configFiles)
            {
                try
                {
                    // Load AppConfig from file: 
                    string configJson = File.ReadAllText(configFile);
                    AppConfig config = JsonConvert.DeserializeObject<AppConfig>(configJson);

                    if (config != null)
                    {
                        // Get appId from filename:
                        string appId = Path.GetFileNameWithoutExtension(configFile);

                        // Directly add app to list using appId 
                        AddAppToListFromConfig(config, appId);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading configuration from {configFile}: {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveAppConfig(string appId, AppConfig config)
        {
            appConfigs[appId] = config;
            string configPath = Program.GetAppConfigPath(appId);

            File.WriteAllText(configPath, JsonConvert.SerializeObject(config));
        }

        private void ApplyMicrophoneBoost(string microphoneName, int boostValue)
        {
            WasapiOut waveOut = null;

            if (string.IsNullOrEmpty(microphoneName))
            {
                return;
            }

            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
            }

            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
            }

            var enumerator = new MMDeviceEnumerator();
            var inputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            foreach (var inputDevice in inputDevices)
            {
                if (inputDevice.FriendlyName == microphoneName)
                {
                    waveIn = new WasapiCapture(inputDevice); // Capture from the real microphone

                    // We'll use WaveInProvider to use waveIn with VolumeSampleProvider
                    var waveInProvider = new WaveInProvider(waveIn);
                    volumeProvider = new VolumeSampleProvider(waveInProvider.ToSampleProvider());
                    volumeProvider.Volume = boostValue / 10.0f;

                    // Find the virtual audio cable device
                    var virtualCableDevice = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                                                        .FirstOrDefault(d => d.FriendlyName == "Line 1 (Virtual Audio Cable)");

                    if (virtualCableDevice != null)
                    {
                        waveOut = new WasapiOut(virtualCableDevice,
                                               AudioClientShareMode.Shared,
                                               false,
                                               100);
                        waveOut.Init(volumeProvider);
                        waveOut.Play();
                    }
                    else
                    {
                        // Handle the case where the virtual cable is not found
                        MessageBox.Show("Virtual audio cable 'Line 1 (Virtual Audio Cable)' not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        // Stop the recording and clean up resources
                        if (waveIn != null)
                        {
                            waveIn.StopRecording();
                            waveIn.Dispose();
                            waveIn = null;
                        }

                        return;
                    }

                    waveIn.StartRecording();
                    break;
                }
            }
        }

        private int GetOutputDeviceIndex(string deviceName)
        {
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var capabilities = WaveOut.GetCapabilities(i);
                if (capabilities.ProductName == deviceName)
                {
                    return i;
                }
            }
            return -1; // Default to the first device if not found
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (volumeProvider != null) // Check if volumeProvider is initialized
            {
                // Process audio data in blocks for efficiency
                int blockSize = 1024; // Process 1024 samples at a time
                byte[] buffer = e.Buffer;
                int bytesRecorded = e.BytesRecorded;

                // Process the audio data in blocks
                for (int offset = 0; offset < bytesRecorded; offset += blockSize)
                {
                    // Create a temporary buffer for the current block
                    int bytesToProcess = Math.Min(blockSize, bytesRecorded - offset);
                    byte[] blockBuffer = new byte[bytesToProcess];
                    Array.Copy(buffer, offset, blockBuffer, 0, bytesToProcess);

                    // Convert the block to floats, apply gain, and convert back to bytes
                    float[] floatBuffer = new float[blockBuffer.Length / 2]; // 2 bytes per float
                    for (int i = 0, j = 0; i < blockBuffer.Length; i += 2, j++)
                    {
                        short sample = BitConverter.ToInt16(blockBuffer, i);
                        floatBuffer[j] = sample / 32768f;
                        floatBuffer[j] *= gainFactor; // Apply gain
                        if (floatBuffer[j] > 1.0f) floatBuffer[j] = 1.0f; // Clip if necessary
                        if (floatBuffer[j] < -1.0f) floatBuffer[j] = -1.0f;
                    }

                    // Convert the float buffer back to bytes
                    byte[] processedBlock = new byte[blockBuffer.Length];
                    for (int i = 0, j = 0; i < processedBlock.Length; i += 2, j++)
                    {
                        short sample = (short)(floatBuffer[j] * 32768f);
                        byte[] bytes = BitConverter.GetBytes(sample);
                        processedBlock[i] = bytes[0];
                        processedBlock[i + 1] = bytes[1];
                    }

                    // Add the processed block to the buffered provider
                    bufferedWaveProvider.AddSamples(processedBlock, 0, bytesToProcess);
                }
            }
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            waveOut?.Stop();
            waveOut?.Dispose();
            waveOut = null;
        }
        private void SelectMicrophoneInDropdown(string microphoneName)
        {
            microphoneDropdown.SelectedItem = microphoneName;
        }

        private void SelectOutputDeviceInDropdown(string deviceName)
        {
            outputDeviceDropdown.SelectedItem = deviceName;
        }

        private void ShowApp(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void ExitApp(object sender, EventArgs e)
        {
            // Stop Audio Processing (Important!) 
            if (waveIn != null)
            {
                waveIn.StopRecording();
                waveIn.Dispose();
                waveIn = null;
            }
            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
            }

            // (Optional) Save Settings:
            Settings.Default.WindowLocation = this.Location;
            Settings.Default.Save();

            // Exit the Application:
            Environment.Exit(0);  // Or 'Application.Exit();' <-- less forceful, may not work
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (Settings.Default.CloseToTray)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                base.OnFormClosing(e);
            }
        }
    }
}
