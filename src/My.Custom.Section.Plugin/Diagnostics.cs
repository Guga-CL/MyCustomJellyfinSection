using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;

namespace My.Custom.Section.Plugin
{
    // This class only adds robust logging for early exceptions.
    // It is minimal and defensive so it should never throw.
    internal static class Diagnostics
    {
        private static string? LogPath;
        private static int _subscribed;

        static Diagnostics()
        {
            try
            {
                var baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) ?? ".",
                    "jellyfin",
                    "plugins",
                    "MyCustomSectionPlugin_1.0.0.0");
                Directory.CreateDirectory(baseDir);
                LogPath = Path.Combine(baseDir, "jellyfin_plugin_exceptions.log");
                TrySubscribe();
                SafeWrite("Diagnostics static ctor complete");
            }
            catch
            {
                // swallow - this should never stop plugin loading
            }
        }

        private static void TrySubscribe()
        {
            // Ensure subscription only happens once and is thread-safe
            if (Interlocked.Exchange(ref _subscribed, 1) == 1) return;
            try
            {
                AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
                {
                    try
                    {
                        SafeWrite("FIRST CHANCE: " + FormatException(e.Exception));
                    }
                    catch { }
                };

                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    try
                    {
                        SafeWrite("UNHANDLED: " + (e.ExceptionObject as Exception)?.ToString() ?? e.ExceptionObject?.ToString() ?? "null");
                    }
                    catch { }
                };

                AppDomain.CurrentDomain.AssemblyLoad += (s, a) =>
                {
                    try { SafeWrite($"AssemblyLoad: {a.LoadedAssembly.FullName}"); } catch { }
                };
            }
            catch { /* swallow */ }
        }

        private static string FormatException(Exception ex)
        {
            try
            {
                if (ex == null) return "null";
                var sb = new StringBuilder();
                sb.AppendLine($"{ex.GetType().FullName}: {ex.Message}");
                sb.AppendLine(ex.StackTrace ?? "");
                if (ex is ReflectionTypeLoadException rtle && rtle.LoaderExceptions != null)
                {
                    sb.AppendLine("ReflectionTypeLoadException.LoaderExceptions:");
                    foreach (var le in rtle.LoaderExceptions) sb.AppendLine(" - " + (le?.ToString() ?? "null"));
                }
                if (ex.InnerException != null)
                {
                    sb.AppendLine("InnerException:");
                    sb.AppendLine(FormatException(ex.InnerException));
                }
                return sb.ToString();
            }
            catch
            {
                return ex.ToString();
            }
        }

        internal static void SafeWrite(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(LogPath)) return;
                var entry = $"{DateTime.UtcNow:O} {text}{Environment.NewLine}";
                File.AppendAllText(LogPath, entry, Encoding.UTF8);
            }
            catch { /* swallow */ }
        }
    }
}
