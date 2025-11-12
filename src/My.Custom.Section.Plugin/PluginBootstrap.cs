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

        // NEW METHOD: Called by Jellyfin via reflection or plugin lifecycle
        public static void RegisterSectionOnStartup()
        {
            try
            {
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
