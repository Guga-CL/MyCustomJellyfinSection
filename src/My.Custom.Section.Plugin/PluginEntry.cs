using System;
using System.Threading.Tasks;
using System.Reflection;

namespace My.Custom.Section.Plugin
{
    public class PluginEntry
    {
        public PluginEntry()
        {
            var debugPath = @"C:\Temp\jellyfin_plugin_debug.txt";

            try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] PluginEntry constructed{Environment.NewLine}"); } catch { }

            try
            {
                var logType = Type.GetType("Serilog.Log, Serilog");
                if (logType != null)
                {
                    var loggerProp = logType.GetProperty("Logger", BindingFlags.Static | BindingFlags.Public);
                    var logger = loggerProp?.GetValue(null);
                    var mi = logger?.GetType().GetMethod("Information", new[] { typeof(string) });
                    mi?.Invoke(logger, new object[] { "[MyCustomSection] PluginEntry constructed" });
                }
            }
            catch { }

            try
            {
                Task.Run(async () =>
                {
                    try
                    {
                        try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] Task started{Environment.NewLine}"); } catch { }
                        await Task.Delay(4000);

                        try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] Calling RegisterSectionOnStartup{Environment.NewLine}"); } catch { }

                        var bootstrap = new PluginBootstrap(null);
                        bootstrap.RegisterSectionOnStartup();

                        try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] RegisterSectionOnStartup complete{Environment.NewLine}"); } catch { }

                        try
                        {
                            var logType2 = Type.GetType("Serilog.Log, Serilog");
                            if (logType2 != null)
                            {
                                var loggerProp2 = logType2.GetProperty("Logger", BindingFlags.Static | BindingFlags.Public);
                                var logger2 = loggerProp2?.GetValue(null);
                                var mi2 = logger2?.GetType().GetMethod("Information", new[] { typeof(string) });
                                mi2?.Invoke(logger2, new object[] { "[MyCustomSection] RegisterSectionOnStartup complete" });
                            }
                        }
                        catch { }
                    }
                    catch (Exception ex)
                    {
                        try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] Task exception: {ex}{Environment.NewLine}"); } catch { }
                        try
                        {
                            var logType3 = Type.GetType("Serilog.Log, Serilog");
                            if (logType3 != null)
                            {
                                var loggerProp3 = logType3.GetProperty("Logger", BindingFlags.Static | BindingFlags.Public);
                                var logger3 = loggerProp3?.GetValue(null);
                                var mi3 = logger3?.GetType().GetMethod("Error", new[] { typeof(string) });
                                mi3?.Invoke(logger3, new object[] { $"[MyCustomSection] Task exception: {ex}" });
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
