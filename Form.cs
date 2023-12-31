using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Windows.Forms;
namespace ChromiumUpdaterLauncher
{
    public partial class Form : System.Windows.Forms.Form
    {
        private readonly static NotifyIcon notifyIcon = new() { Visible = true, Icon = System.Drawing.SystemIcons.Application };
        private readonly static HttpClient client = new();
        public Form() => InitializeComponent();
        private void Form_Load(object sender, EventArgs e) 
        {
            Hide();
            Initialize();
        }
        private async static void Initialize()
        {
            Process[] runningInstances = Process.GetProcessesByName(UpdaterConstants.installPath.TrimEnd(".exe".ToCharArray()));
            if (runningInstances.Length > 0)
            {
                for (int i = 0; i < runningInstances.Length; i++)
                    runningInstances[i].Kill();
            }
            if (Process.GetCurrentProcess().MainModule.FileName == UpdaterConstants.tempLauncherPath)
            {
                File.Delete(UpdaterConstants.launcherPath);
                File.Copy(Process.GetCurrentProcess().MainModule.FileName, UpdaterConstants.launcherPath);
                Process.Start(UpdaterConstants.launcherPath);
                ExitProgram(false);
            }
            if (!File.Exists(UpdaterConstants.launcherPath))
            {
                Directory.CreateDirectory(UpdaterConstants.installDirectory);
                File.Copy(Environment.ProcessPath, UpdaterConstants.launcherPath, true);
            }
            if (!File.Exists(UpdaterConstants.installPath))
            {
                await File.WriteAllBytesAsync(UpdaterConstants.installPath, Properties.Resources.Chromium_Updater);
                CheckForUpdate();
            }
            else if (File.Exists(UpdaterConstants.configPath))
            {
                using StreamReader reader = new(UpdaterConstants.configPath);
                const string CheckForSelfUpdate = "\"CheckForSelfUpdate\": ";
                while (!reader.EndOfStream)
                {
                    string readerResult = reader.ReadLine();
                    if (readerResult.Contains(CheckForSelfUpdate + "true"))
                        CheckForUpdate();
                    else if (readerResult.Contains(CheckForSelfUpdate + "false"))
                        ExitProgram(true);
                }
            }
            else
                ExitProgram(true);
        }
        private async static void CheckForUpdate()
        {
            const string ChromiumUpdaterGUI = "ChromiumUpdaterGUI";
            Octokit.GitHubClient githubClient = new(new Octokit.ProductHeaderValue(ChromiumUpdaterGUI));
            System.Collections.Generic.IReadOnlyList<Octokit.Release> releases = null;

            try { releases = await githubClient.Repository.Release.GetAll("ozkut", ChromiumUpdaterGUI); }
            catch (HttpRequestException)
            {
                _ = MessageBox.Show("A connection error has occurred when checking for updates!", UpdaterConstants.title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExitProgram(true);
            }

            float existingVersion = float.Parse(FileVersionInfo.GetVersionInfo(UpdaterConstants.installPath).ProductVersion[..3]);
            float latestVersion = float.Parse(releases[0].Name);

            string existingVersionString = existingVersion.ToString("0.0");
            string latestVersionString = latestVersion.ToString("0.0");

            if (latestVersion == existingVersion)
            {
                if (File.Exists(UpdaterConstants.tempLauncherPath) && Process.GetCurrentProcess().MainModule.FileName == UpdaterConstants.launcherPath)
                    File.Delete(UpdaterConstants.tempLauncherPath);
                ExitProgram(true);
            }
            else if (latestVersion > existingVersion)
            {
                DialogResult installLatestDialog =
                MessageBox.Show($"A newer version of Chromium Updater is available! (Version {latestVersionString})\n" +
                                $"Would you like to update from Version {existingVersionString} that is currently running?",
                                UpdaterConstants.title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (installLatestDialog == DialogResult.Yes)
                    await Task.Run(() => UpdateLauncher());
                else
                    ExitProgram(true);
            }
        }
        private async static Task UpdateLauncher()
        {
            notifyIcon.ShowBalloonTip(3000, UpdaterConstants.title, "Updating Chromum Updater", ToolTipIcon.Info);
            Process[] processes = Process.GetProcessesByName("Chromium Updater");
            if (processes.Length > 0)
            {
                foreach (Process process in processes)
                    process.Kill();
            }
            if (File.Exists(UpdaterConstants.installPath))
                File.Delete(UpdaterConstants.installPath);
            //download newest launcher from github which has chromium updater inside it and install chromium updater using the new launcher
            if (Environment.ProcessPath == UpdaterConstants.launcherPath)
            {
                try
                {
                    byte[] launcher = await client.GetByteArrayAsync("https://github.com/ozkut/ChromiumUpdaterGUI/releases/latest/download/Chromium.Updater.Launcher.exe");
                    await File.WriteAllBytesAsync(UpdaterConstants.tempLauncherPath, launcher);
                    Process.Start(UpdaterConstants.tempLauncherPath);
                }
                catch (Exception e)
                {
                    if (e is HttpRequestException)
                        _ = MessageBox.Show("A connection error has occurred when trying to download Chromium Updater!", UpdaterConstants.title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                        _ = MessageBox.Show(e.Message, UpdaterConstants.title, MessageBoxButtons.OK, MessageBoxIcon.Error);

                    notifyIcon.Dispose();
                    client.Dispose();
                    Environment.Exit(1);
                }
                ExitProgram(false);
            }
            else 
            {
                Process.Start(UpdaterConstants.launcherPath);
                ExitProgram(false);
            }
        }
        private static void ExitProgram(bool start)
        {
            if (start)
                Process.Start(UpdaterConstants.installPath);
            notifyIcon.Dispose();
            client.Dispose();
            Environment.Exit(Environment.ExitCode);
        }
    }
}