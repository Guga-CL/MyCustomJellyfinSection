using System;
using System.IO;

namespace My.Custom.Section.Plugin
{
    // Diagnostic probe: static constructor executes when the assembly is loaded into the AppDomain.
    // It only writes a small debug file and swallows exceptions to avoid affecting startup.
    internal static class PluginLoadProbe
    {
        static PluginLoadProbe()
        {
            try
            {
                var baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "jellyfin",
                    "plugins",
                    "MyCustomSectionPlugin_1.0.0.0");
                Directory.CreateDirectory(baseDir);
                var p = Path.Combine(baseDir, "jellyfin_plugin_debug.txt");
                File.AppendAllText(p, $"{DateTime.UtcNow:O} PluginLoadProbe static ctor executed{Environment.NewLine}");
            }
            catch
            {
                // intentionally swallow — diagnostic only
            }
        }

        // A no-op method you can call later if you want to force the type to be JIT-touched explicitly.
        public static void Touch() { }
    }
}
