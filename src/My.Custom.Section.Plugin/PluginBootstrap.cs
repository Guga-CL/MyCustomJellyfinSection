using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace My.Custom.Section.Plugin
{
    internal static class PluginBootstrap
    {
        internal static void TryRegisterSection()
        {
            try
            {
                var payload = SectionRegistrar.BuildPayload();
                HomeScreenRegistrationInvoker.InvokeRegisterSection(payload);
            }
            catch (Exception ex)
            {
                ServerEntry.TryWriteDebug($"PluginBootstrap.TryRegisterSection exception: {ex.GetType().FullName}: {ex.Message}");
            }
        }

        // Diagnostic helper to force a visible file when the bootstrap type is loaded or invoked
        private static void TouchBootstrapAndWriteDebug()
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
                File.AppendAllText(p, $"{DateTime.UtcNow:O} PluginBootstrap touched{Environment.NewLine}");
            }
            catch
            {
                // Swallow any errors to avoid affecting server startup
            }
        }

        // NEW METHOD: Called by Jellyfin via reflection or plugin lifecycle
        public static void RegisterSectionOnStartup()
        {
            try
            {
                // ensure we produce a visible marker so we can confirm this method was executed
                TouchBootstrapAndWriteDebug();

                // Start the plugin registration logic (runs background registration task)
                try
                {
                    new Plugin().Start();
                }
                catch (Exception ex)
                {
                    ServerEntry.TryWriteDebug($"PluginBootstrap.RegisterSectionOnStartup (Plugin.Start) exception: {ex.GetType().FullName}: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                ServerEntry.TryWriteDebug($"PluginBootstrap.RegisterSectionOnStartup exception: {ex.GetType().FullName}: {ex.Message}");
            }
        }
    }
}
