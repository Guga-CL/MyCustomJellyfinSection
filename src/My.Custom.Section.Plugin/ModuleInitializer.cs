using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace My.Custom.Section.Plugin
{
    internal static class PluginModuleInit
    {
        #pragma warning disable CA2255 // The 'ModuleInitializer' attribute is only intended to be used in application code or advanced source generator scenarios
        [ModuleInitializer]
        internal static void Init()
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
                File.AppendAllText(p, $"{DateTime.UtcNow:O} ModuleInitializer executed{Environment.NewLine}");

                // Fire-and-forget to avoid blocking server startup
                Task.Run(() =>
                {
                    try
                    {
                        new Plugin().Start();
                        File.AppendAllText(p, $"{DateTime.UtcNow:O} Plugin.Start invoked from ModuleInitializer{Environment.NewLine}");
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText(p, $"{DateTime.UtcNow:O} Plugin.Start failed: {ex.GetType().FullName}: {ex.Message}{Environment.NewLine}");
                    }
                });
            }
            catch
            {
                // swallow - diagnostic only
            }
        }
    }
}
