using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;
using Newtonsoft.Json.Linq;

namespace My.Custom.Section.Plugin
{
    public class ServerEntry : BasePlugin
    {
        public override string Name => "My Custom Section";
        public override string Description => "Adds a custom Home Screen section";

        public ServerEntry()
        {
            // Schedule initialization asynchronously so ctor remains trivial and non-blocking.
            Task.Run(() => RegisterSectionOnStartup());
        }

        private void RegisterSectionOnStartup()
        {
            try
            {
                var payload = SectionRegistrar.BuildPayload();
                var asm = PluginBootstrap.FindHomeScreenSectionsAssembly();
                if (asm == null)
                {
                    TryDebugWrite("FindHomeScreenSectionsAssembly returned null");
                    return;
                }

                var pluginInterfaceType = asm.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");
                if (pluginInterfaceType == null)
                {
                    TryDebugWrite("PluginInterface type not found in HomeScreenSections assembly");
                    return;
                }

                var registerMethod = pluginInterfaceType.GetMethod("RegisterSection", BindingFlags.Public | BindingFlags.Static);
                if (registerMethod == null)
                {
                    TryDebugWrite("RegisterSection method not found on PluginInterface");
                    return;
                }

                // Pass the JObject payload. The HomeScreenSections plugin accepts JObject payloads.
                registerMethod.Invoke(null, new object[] { payload });

                TryDebugWrite("RegisterSection invoked successfully");
            }
            catch (TargetInvocationException tie)
            {
                TryDebugWrite("TargetInvocationException: " + tie.InnerException?.ToString() ?? tie.ToString());
            }
            catch (Exception ex)
            {
                TryDebugWrite("Exception in RegisterSectionOnStartup: " + ex.ToString());
            }
        }

        private void TryDebugWrite(string text)
        {
            try
            {
                var path = Path.Combine(Path.GetDirectoryName(typeof(ServerEntry).Assembly.Location) ?? Path.GetTempPath(), "jellyfin_plugin_debug.txt");
                File.AppendAllText(path, $"{DateTime.UtcNow:O} - {text}\n");
            }
            catch { /* ignore */ }
        }
    }
}
