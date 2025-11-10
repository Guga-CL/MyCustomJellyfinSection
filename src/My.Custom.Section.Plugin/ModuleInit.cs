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
            string debugPath;
            try
            {
                var asmPath = Assembly.GetExecutingAssembly().Location;
                var pluginDir = Path.GetDirectoryName(asmPath) ?? @"C:\Temp";
                debugPath = Path.Combine(pluginDir, "jellyfin_plugin_debug.txt");
            }
            catch
            {
                debugPath = @"C:\Temp\jellyfin_plugin_debug.txt";
            }

            void TryWrite(string s)
            {
                try { Directory.CreateDirectory(Path.GetDirectoryName(debugPath) ?? @"C:\Temp"); File.AppendAllText(debugPath, s); } catch { }
            }

            TryWrite($"{DateTime.Now:O} [MyCustomSection] ModuleInitializer invoked (path={debugPath}){Environment.NewLine}");

            try
            {
                var logType = Type.GetType("Serilog.Log, Serilog");
                if (logType != null)
                {
                    var loggerProp = logType.GetProperty("Logger", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    var logger = loggerProp?.GetValue(null);
                    var mi = logger?.GetType().GetMethod("Information", new[] { typeof(string) });
                    mi?.Invoke(logger, new object[] { $"[MyCustomSection] ModuleInitializer invoked (path={debugPath})" });
                }
            }
            catch { }

            try
            {
                Task.Run(async () =>
                {
                    await Task.Delay(4000);
                    TryWrite($"{DateTime.Now:O} [MyCustomSection] ModuleInit forcing RegisterSectionOnStartup{Environment.NewLine}");
                    try
                    {
                        new PluginBootstrap(null).RegisterSectionOnStartup();
                        TryWrite($"{DateTime.Now:O} [MyCustomSection] ModuleInit RegisterSectionOnStartup complete{Environment.NewLine}");
                    }
                    catch (Exception ex)
                    {
                        TryWrite($"{DateTime.Now:O} [MyCustomSection] ModuleInit exception: {ex}{Environment.NewLine}");
                    }
                });
            }
            catch { }
        }
    }
}
