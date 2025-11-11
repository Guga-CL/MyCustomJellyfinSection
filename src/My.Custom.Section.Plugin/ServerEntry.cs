using MediaBrowser.Common.Plugins;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace My.Custom.Section.Plugin
{
    public class ServerEntry : BasePlugin
    {
        public override string Name => "My Custom Section";
        public override string Description => "Adds a custom Home Screen section";

        public ServerEntry()
        {
            TryWriteDebug("ServerEntry ctor entered");
            // Defer initialization to avoid heavy work in ctor
            Task.Run(() => SafeInit());
        }

        private void SafeInit()
        {
            try
            {
                TryWriteDebug("SafeInit started");
                // Keep this minimal. Call into other helpers only inside try/catch.
                PluginBootstrap.TryRegisterSection();
                TryWriteDebug("SafeInit finished");
            }
            catch (Exception ex)
            {
                TryWriteDebug($"SafeInit exception: {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void TryWriteDebug(string text)
        {
            try
            {
                var logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "jellyfin",
                    "plugins",
                    "MyCustomSectionPlugin_1.0.0.0",
                    "jellyfin_plugin_debug.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath) ?? ".");
                File.AppendAllText(logPath, $"{DateTime.UtcNow:O} {text}{Environment.NewLine}", Encoding.UTF8);
            }
            catch { /* swallow: never throw from debug logging */ }
        }
    }
}
