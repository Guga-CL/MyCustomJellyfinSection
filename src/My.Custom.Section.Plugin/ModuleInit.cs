using System;
using System.Runtime.CompilerServices;

namespace My.Custom.Section.Plugin
{
    internal static class ModuleInit
    {
        [ModuleInitializer]
        internal static void Init()
        {
            var debugPath = @"C:\Temp\jellyfin_plugin_debug.txt";
            try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] ModuleInitializer invoked{Environment.NewLine}"); } catch { }
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
        }
    }
}
