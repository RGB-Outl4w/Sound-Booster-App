using System;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;

namespace MicrophoneBoosterApp
{
    static class Program
    {
        private const string appName = "RGBSoundBoosterApp";

        [STAThread]
        static void Main()
        {
            // Ensure only one instance of the application is running
            using (Mutex mutex = new Mutex(true, appName, out bool createdNew))
            {
                if (createdNew)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new MainForm());

                    LoadConfigsFromPreviousInstance();
                }
                else
                {
                    // Second instance: Signal the first instance (if running) to load config
                    // and then exit this instance. 
                    string configFilePath = GetAppConfigPath();
                    SignalFirstInstanceToLoadConfig(configFilePath);
                    return;
                }
            }
        }

        public static string GetAppConfigPath(string appName = null)
        {
            string exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string cfgsFolderPath = Path.Combine(exePath, "cfgs");
            Directory.CreateDirectory(cfgsFolderPath);

            if (string.IsNullOrEmpty(appName))
            {
                // Default to using current Process ID (if no appName is provided)
                // You'll likely call this function with an appName, 
                // mainly for initial instance launches
                appName = Process.GetCurrentProcess().Id.ToString();
            }

            string sanitizedAppName = string.Join("_", appName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(cfgsFolderPath, $"{sanitizedAppName}.json");
        }

        // Load any configuration files from previous instances 
        static void LoadConfigsFromPreviousInstance()
        {
            string previousInstanceConfigPath = GetAppConfigPath();
            // Check if the config file from a previous instance launch attempt exists 
            if (File.Exists(previousInstanceConfigPath))
            {
                try
                {
                    MainForm mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();

                    if (mainForm != null)
                    {
                        // Assuming the filename is the application's name, parse it. 
                        string appId = Path.GetFileNameWithoutExtension(previousInstanceConfigPath);

                        // Load the configuration data 
                        string configJson = File.ReadAllText(previousInstanceConfigPath);
                        AppConfig config = JsonConvert.DeserializeObject<AppConfig>(configJson);

                        mainForm.Invoke((MethodInvoker)delegate
                        {
                            // Add the application to the list 
                            mainForm.AddAppToListFromConfig(config, appId);
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static string GetUniqueAppIdentifier(string appName)
        {
            try
            {
                Process process = Process.GetProcessesByName(appName).FirstOrDefault();

                if (process != null)
                {
                    string exePath = process.MainModule.FileName;
                    return $"{appName}_{exePath}";
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., process not found, access denied) 
                Console.WriteLine($"Error getting unique identifier: {ex.Message}");
            }

            return appName; // Fallback if identifier generation fails 
        }

        // Handle the logic when a second instance is launched 
        static void SignalFirstInstanceToLoadConfig(string configFilePath)
        {
            // Deleting the config 
            // from the second instance as the first instance is already running 
            // You might need different logic depending on how you want 
            // the application to behave in the multi-instance case 
            if (File.Exists(configFilePath))
            {
                try
                {
                    File.Delete(configFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting temporary configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}