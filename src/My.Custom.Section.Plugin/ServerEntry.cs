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
            // schedule the registration so ctor does not throw
            Task.Run(RegisterSectionOnStartup);
        }

        private void RegisterSectionOnStartup()
        {
            try
            {
                var payload = SectionRegistrar.BuildPayload();
                var payloadJson = payload.ToString(Newtonsoft.Json.Formatting.None);
                PluginBootstrap.TryRegisterSectionWithPayload(payloadJson);
                TryDebugWrite("RegisterSection invoked successfully");
            }
            catch (Exception ex)
            {
                TryDebugWrite("RegisterSectionOnStartup: " + ex.ToString());
            }
        }

        private void TryDebugWrite(string text)
        {
            try
            {
                var asmPath = Assembly.GetExecutingAssembly().Location;
                var dir = Path.GetDirectoryName(asmPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var path = Path.Combine(dir, "jellyfin_plugin_debug.txt");
                File.AppendAllText(path, $"{DateTime.UtcNow:O} {text}{Environment.NewLine}");
            }
            catch { /* ignore */ }
        }
    }
}
