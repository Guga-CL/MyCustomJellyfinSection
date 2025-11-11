using Newtonsoft.Json.Linq;
using System;

namespace My.Custom.Section.Plugin
{
    internal static class PluginBootstrap
    {
        internal static void TryRegisterSection()
        {
            try
            {
                // Build a minimal payload object and call the HomeScreenSections interop.
                var payload = SectionRegistrar.BuildPayload();
                // If the HomeScreenSections plugin expects a JObject sent to a static method,
                // call it reflectively here. Keep calls optional and guarded.
                HomeScreenRegistrationInvoker.InvokeRegisterSection(payload);
            }
            catch (Exception ex)
            {
                Log($"TryRegisterSection exception: {ex.GetType().FullName}: {ex.Message}");
            }
        }

        private static void Log(string s) => SafeLog(s);

        private static void SafeLog(string s)
        {
            try
            {
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "jellyfin",
                    "plugins",
                    "MyCustomSectionPlugin_1.0.0.0",
                    "jellyfin_plugin_debug.txt");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath) ?? ".");
                System.IO.File.AppendAllText(logPath, $"{DateTime.UtcNow:O} {s}{Environment.NewLine}");
            }
            catch { }
        }

    }
}
