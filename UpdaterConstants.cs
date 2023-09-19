using System.IO;
namespace ChromiumUpdaterLauncher
{
    internal static class UpdaterConstants
    {
        //other
        internal const string title = "Chromium Updater Launcher";

        internal readonly static string installDirectory = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "Chromium Updater");
         
        //related to Chromium updater
        internal readonly static string configPath = Path.Combine(installDirectory, "ChromiumUpdater_Config.json");
        internal readonly static string installPath = Path.Combine(installDirectory, "Chromium Updater.exe");
        
        //launcher
        internal readonly static string launcherPath = Path.Combine(installDirectory, "Launcher.exe");
        internal readonly static string tempLauncherPath = Path.Combine(installDirectory, "LauncherTemp.exe");
    }
}