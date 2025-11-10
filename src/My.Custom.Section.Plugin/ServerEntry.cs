using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;

namespace My.Custom.Section.Plugin
{
    // Minimal plugin entry deriving from BasePlugin and implementing required members.
    public class ServerEntry : BasePlugin
    {
        // Provide a short plugin name shown in server UI
        public override string Name => "My Custom Section";

        // Provide a short description shown in server UI
        public override string Description => "Adds a custom section to Jellyfin UI";

        // Constructor schedules registration asynchronously to avoid blocking plugin load.
        public ServerEntry()
        {
            TryWrite("ServerEntry(BasePlugin) ctor invoked");

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(800).ConfigureAwait(false);
                    TryWrite("ServerEntry scheduled RegisterSectionOnStartup start");
                    try
                    {
                        var bootstrap = new PluginBootstrap(null);
                        bootstrap.RegisterSectionOnStartup();
                        TryWrite("ServerEntry RegisterSectionOnStartup completed");
                    }
                    catch (Exception ex)
                    {
                        TryWrite("ServerEntry RegisterSectionOnStartup exception: " + ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    TryWrite("ServerEntry scheduling exception: " + ex.ToString());
                }
            });
        }

        private void TryWrite(string text)
        {
            try
            {
                var asmPath = Assembly.GetExecutingAssembly().Location;
                var dir = Path.GetDirectoryName(asmPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var path = Path.Combine(dir, "jellyfin_plugin_debug.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? dir);
                File.AppendAllText(path, $"{DateTime.Now:O} {text}{Environment.NewLine}");
            }
            catch { }
        }
    }
}
