using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace My.Custom.Section.Plugin
{
    internal static class ModuleInit
    {
        [ModuleInitializer]
        internal static void Init()
        {
            var debugPath = @"C:\Temp\jellyfin_plugin_debug.txt";

            // Immediate trace to show module initializer ran
            try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] ModuleInitializer invoked{Environment.NewLine}"); } catch { }

            // Best-effort Serilog notice
            try
            {
                var logType = Type.GetType("Serilog.Log, Serilog");
                if (logType != null)
                {
                    var loggerProp = logType.GetProperty("Logger", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    var logger = loggerProp?.GetValue(null);
                    var mi = logger?.GetType().GetMethod("Information", new[] { typeof(string) });
                    mi?.Invoke(logger, new object[] { "[MyCustomSection] ModuleInitializer invoked" });
                }
            }
            catch { }

            // Schedule the registration after a short delay to avoid race conditions.
            // This runs even if Jellyfin never instantiates PluginEntry.
            try
            {
                Task.Run(async () =>
                {
                    await Task.Delay(4000);
                    try
                    {
                        try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] ModuleInit forcing RegisterSectionOnStartup{Environment.NewLine}"); } catch { }

                        new PluginBootstrap(null).RegisterSectionOnStartup();

                        try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] ModuleInit RegisterSectionOnStartup complete{Environment.NewLine}"); } catch { }
                        try
                        {
                            var logType2 = Type.GetType("Serilog.Log, Serilog");
                            if (logType2 != null)
                            {
                                var loggerProp2 = logType2.GetProperty("Logger", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                                var logger2 = loggerProp2?.GetValue(null);
                                var mi2 = logger2?.GetType().GetMethod("Information", new[] { typeof(string) });
                                mi2?.Invoke(logger2, new object[] { "[MyCustomSection] ModuleInit RegisterSectionOnStartup complete" });
                            }
                        }
                        catch { }
                    }
                    catch (Exception ex)
                    {
                        try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] ModuleInit exception: {ex}{Environment.NewLine}"); } catch { }
                        try
                        {
                            var logType3 = Type.GetType("Serilog.Log, Serilog");
                            if (logType3 != null)
                            {
                                var loggerProp3 = logType3.GetProperty("Logger", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                                var logger3 = loggerProp3?.GetValue(null);
                                var mi3 = logger3?.GetType().GetMethod("Error", new[] { typeof(string) });
                                mi3?.Invoke(logger3, new object[] { $"[MyCustomSection] ModuleInit exception: {ex}" });
                            }
                        }
                        catch { }
                    }
                });
            }
            catch { }
        }
    }
}
