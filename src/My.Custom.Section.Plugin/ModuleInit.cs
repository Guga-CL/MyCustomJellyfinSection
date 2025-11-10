using System;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;

namespace My.Custom.Section.Plugin
{
    internal static class ModuleInit
    {
        [ModuleInitializer]
        internal static void Init()
        {
            string TrySafePath(string p)
            {
                try { Directory.CreateDirectory(Path.GetDirectoryName(p) ?? p); File.AppendAllText(p, $"{DateTime.Now:O} [MyCustomSection] probe write{Environment.NewLine}"); return p; } catch { return null; }
            }

            string asmPath = null;
            try { asmPath = Assembly.GetExecutingAssembly().Location; } catch { asmPath = null; }

            var candidates = new[]
            {
                @"C:\Temp\jellyfin_plugin_debug.txt",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"jellyfin","jellyfin_plugin_debug.txt"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"jellyfin","jellyfin_plugin_debug.txt"),
                asmPath != null ? Path.Combine(Path.GetDirectoryName(asmPath)??@"C:\Temp","jellyfin_plugin_debug.txt") : null,
                Path.Combine(AppContext.BaseDirectory ?? @"C:\Temp","jellyfin_plugin_debug.txt")
            };

            foreach (var c in candidates)
            {
                if (string.IsNullOrEmpty(c)) continue;
                var written = TrySafePath(c);
                try
                {
                    if (written == null) File.AppendAllText(@"C:\Temp\jellyfin_module_fallback_log.txt", $"{DateTime.Now:O} Could not write to {c}{Environment.NewLine}");
                }
                catch { }
            }

            try
            {
                Task.Run(async () =>
                {
                    await Task.Delay(3000);
                    try
                    {
                        File.AppendAllText(candidates[0] ?? @"C:\Temp\jellyfin_plugin_debug.txt", $"{DateTime.Now:O} [MyCustomSection] attempting forced registration; asmPath={asmPath}{Environment.NewLine}");
                    }
                    catch { }
                    try
                    {
                        new PluginBootstrap(null).RegisterSectionOnStartup();
                        try { File.AppendAllText(candidates[0] ?? @"C:\Temp\jellyfin_plugin_debug.txt", $"{DateTime.Now:O} [MyCustomSection] RegisterSectionOnStartup OK{Environment.NewLine}"); } catch { }
                    }
                    catch (Exception ex)
                    {
                        try { File.AppendAllText(candidates[0] ?? @"C:\Temp\jellyfin_plugin_debug.txt", $"{DateTime.Now:O} [MyCustomSection] Register exception: {ex}{Environment.NewLine}"); } catch { }
                    }
                });
            }
            catch { }
        }
    }
}
