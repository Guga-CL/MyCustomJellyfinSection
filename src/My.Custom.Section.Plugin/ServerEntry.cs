using MediaBrowser.Common.Plugins;
using System;
using System.IO;
using System.Text;

namespace My.Custom.Section.Plugin
{
    public class ServerEntry : BasePlugin
    {
        // Simple literal values only
        public override string Name => "My Custom Section (Test Minimal)";
        public override string Description => "Minimal plugin used to test plugin loading.";

        // Parameterless ctor only; do not run any background work here
        public ServerEntry()
        {
            TryWriteDebug("Minimal ServerEntry ctor executed");
        }

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
                var p = Path.Combine(baseDir, "jellyfin_plugin_debug_minimal.txt");
                File.AppendAllText(p, $"{DateTime.UtcNow:O} {text}{Environment.NewLine}", Encoding.UTF8);
            }
            catch { /* swallow */ }
        }
    }
}
