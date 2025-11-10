using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Plugins; // NuGet/assembly symbol namespace used by Jellyfin
using Microsoft.Extensions.Logging;

namespace My.Custom.Section.Plugin
{
    // Minimal implementation so Jellyfin constructs this type via its plugin loader.
    public class ServerEntry : IServerEntryPoint
    {
        private readonly ILogger<ServerEntry>? _logger;

        // Parameterless ctor is fine; DI may inject logger if available.
        public ServerEntry() { }

        // Called by Jellyfin on plugin enable/start
        public void Run()
        {
            TryWrite("ServerEntry Run invoked");
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1200);
                    TryWrite("ServerEntry scheduled RegisterSectionOnStartup start");
                    try
                    {
                        var bootstrap = new PluginBootstrap(null);
                        bootstrap.RegisterSectionOnStartup();
                        TryWrite("ServerEntry RegisterSectionOnStartup completed");
                    }
                    catch (Exception ex)
                    {
                        TryWrite("ServerEntry RegisterSectionOnStartup exception: " + ex);
                    }
                }
                catch (Exception ex)
                {
                    TryWrite("ServerEntry scheduling exception: " + ex);
                }
            });
        }

        // Called by Jellyfin on shutdown; keep lightweight
        public void Stop()
        {
            TryWrite("ServerEntry Stop invoked");
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
