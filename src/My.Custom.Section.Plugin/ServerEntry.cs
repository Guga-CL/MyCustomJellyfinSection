using MediaBrowser.Common.Plugins;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace My.Custom.Section.Plugin
{
    public class ServerEntry : BasePlugin
    {
        // Keep these extremely simple and guaranteed not to throw
        public override string Name => "My Custom Section";
        public override string Description => "Adds a custom Home Screen section";

        public ServerEntry()
        {
            // Minimal ctor: log and defer all work
            TryWriteDebug("ServerEntry ctor entered");
            Task.Run(() => SafeInit());
        }

        private void SafeInit()
        {
            try
            {
                TryWriteDebug("SafeInit started");
                PluginBootstrap.TryRegisterSection();
                TryWriteDebug("SafeInit finished");
            }
            catch (Exception ex)
            {
                TryWriteDebug($"SafeInit exception: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // Non-throwing logger used only for diagnostics
        internal static void TryWriteDebug(string text)
        {
            try
            {
                var baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "jellyfin",
                    "plugins",
                    "MyCustomSectionPlugin_1.0.0.0");
                Directory.CreateDirectory(baseDir);
                var logPath = Path.Combine(baseDir, "jellyfin_plugin_debug.txt");
                File.AppendAllText(logPath, $"{DateTime.UtcNow:O} {text}{Environment.NewLine}", Encoding.UTF8);
            }
            catch { /* swallow */ }
        }
    }
}
