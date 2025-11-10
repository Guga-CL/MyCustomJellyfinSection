using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace My.Custom.Section.Plugin
{
    public class PluginBootstrap
    {
        private readonly object? _logger;

        public PluginBootstrap(object? logger = null)
        {
            _logger = logger;
        }

        // Logger helpers moved before RegisterSectionOnStartup to avoid CS0103 in editors/analysis
        private bool TryInvokeLogger(string methodName, params object[] args)
        {
            if (_logger == null) return false;
            try
            {
                var m = _logger.GetType().GetMethod(methodName, args.Select(a => a?.GetType() ?? typeof(object)).ToArray());
                if (m != null)
                {
                    m.Invoke(_logger, args);
                    return true;
                }

                var m2 = _logger.GetType().GetMethod(methodName, new[] { typeof(string) });
                if (m2 != null)
                {
                    m2.Invoke(_logger, new object[] { string.Join(" ", args.Select(a => a?.ToString())) });
                    return true;
                }
            }
            catch
            {
                // swallow logger problems
            }

            return false;
        }

        private void LogDebug(string text) { if (!TryInvokeLogger("Debug", text)) Console.WriteLine(text); }
        private void LogInfo(string text) { if (!TryInvokeLogger("Info", text)) Console.WriteLine(text); }
        private void LogWarn(string text) { if (!TryInvokeLogger("Warn", text)) Console.WriteLine("WARN: " + text); }
        private void LogError(string text) { if (!TryInvokeLogger("Error", text)) Console.WriteLine("ERROR: " + text); }

        public void RegisterSectionOnStartup()
        {
            // Debug wrapper: write entering/exiting and any caught exceptions to a user-writable file
            var debugPath = @"C:\Temp\jellyfin_plugin_debug.txt";
;
            try
            {
                try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] Entering RegisterSectionOnStartup{Environment.NewLine}"); } catch { }
                try
                {
                    var logType = Type.GetType("Serilog.Log, Serilog");
                    if (logType != null)
                    {
                        var loggerProp = logType.GetProperty("Logger", BindingFlags.Static | BindingFlags.Public);
                        var logger = loggerProp?.GetValue(null);
                        var mi = logger?.GetType().GetMethod("Information", new[] { typeof(string) });
                        mi?.Invoke(logger, new object[] { "[MyCustomSection] Entering RegisterSectionOnStartup" });
                    }
                }
                catch { }
            }
            catch { }

            try
            {
                LogInfo("PluginBootstrap: building payload for Home Screen Sections");

                var payload = new
                {
                    id = "11111111-2222-3333-4444-555555555555",
                    displayText = "My Custom Section",
                    limit = 1,
                    route = "/my-custom-section",
                    additionalData = "{}",
                    resultsAssembly = this.GetType().Assembly.FullName,
                    resultsClass = $"{this.GetType().Namespace}.ResultsHandler",
                    resultsMethod = "GetSectionResults"
                };

                string payloadJson = JsonSerializer.Serialize(payload);
                LogInfo($"PluginBootstrap: payload => {payloadJson}");

                var loaded = AssemblyLoadContext.All
                    .SelectMany(ctx => ctx.Assemblies)
                    .Select(a => a.FullName)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToArray();

                LogInfo($"PluginBootstrap: {loaded.Length} assemblies loaded. (first 10 shown)");
                foreach (var name in loaded.Take(10))
                {
                    LogDebug($"Loaded assembly: {name}");
                }

                // Find HomeScreenSections assembly
                var homeScreenSectionsAssembly = AssemblyLoadContext
                    .All
                    .SelectMany(ctx => ctx.Assemblies)
                    .FirstOrDefault(a =>
                        (a.FullName?.IndexOf(".HomeScreenSections", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                        || (a.FullName?.IndexOf("HomeScreenSections", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                        || (a.GetName().Name?.IndexOf("HomeScreenSections", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);

                if (homeScreenSectionsAssembly == null)
                {
                    LogWarn("PluginBootstrap: HomeScreenSections assembly not found. Will retry with fallback search.");
                    homeScreenSectionsAssembly = AssemblyLoadContext
                        .All
                        .SelectMany(ctx => ctx.Assemblies)
                        .FirstOrDefault(a => a.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface") != null);

                    if (homeScreenSectionsAssembly == null)
                    {
                        LogWarn("PluginBootstrap: HomeScreenSections plugin interface assembly still not found. Aborting registration.");
                        return;
                    }
                }

                LogInfo($"PluginBootstrap: found candidate assembly: {homeScreenSectionsAssembly.FullName}");

                var pluginInterfaceType = homeScreenSectionsAssembly.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");
                if (pluginInterfaceType == null)
                {
                    LogWarn("PluginBootstrap: PluginInterface type not found on candidate assembly. Aborting registration.");
                    return;
                }

                var registerMethod = pluginInterfaceType.GetMethod("RegisterSection", BindingFlags.Public | BindingFlags.Static);
                if (registerMethod == null)
                {
                    LogWarn("PluginBootstrap: RegisterSection method not found on PluginInterface. Aborting registration.");
                    return;
                }

                // Try 1: many implementations accept a JSON string
                try
                {
                    LogInfo("PluginBootstrap: attempting RegisterSection with JSON string payload.");
                    registerMethod.Invoke(null, new object?[] { payloadJson });
                    LogInfo("PluginBootstrap: RegisterSection invoked successfully with JSON string.");

                    try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] RegisterSection invoked with JSON{Environment.NewLine}"); } catch { }
                    try
                    {
                        var logType = Type.GetType("Serilog.Log, Serilog");
                        if (logType != null)
                        {
                            var loggerProp = logType.GetProperty("Logger", BindingFlags.Static | BindingFlags.Public);
                            var logger = loggerProp?.GetValue(null);
                            var mi = logger?.GetType().GetMethod("Information", new[] { typeof(string) });
                            mi?.Invoke(logger, new object[] { "[MyCustomSection] RegisterSection invoked with JSON" });
                        }
                    }
                    catch { }

                    return;
                }
                catch (TargetInvocationException tie)
                {
                    LogWarn($"PluginBootstrap: RegisterSection with JSON string threw: {tie.InnerException?.ToString() ?? tie.ToString()}");
                    try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] JSON TargetInvocationException: {tie.InnerException}{Environment.NewLine}"); } catch { }
                }
                catch (Exception ex)
                {
                    LogWarn($"PluginBootstrap: RegisterSection with JSON string failed: {ex}");
                    try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] JSON EX: {ex}{Environment.NewLine}"); } catch { }
                }

                // Try 2: construct typed payload object from HomeScreenSections assembly via reflection
                try
                {
                    LogInfo("PluginBootstrap: attempting to construct typed payload object via reflection.");

                    // Guess common payload DTO type names used by HomeScreenSections plugin
                    var candidateTypeNames = new[]
                    {
                        "Jellyfin.Plugin.HomeScreenSections.SectionPayload",
                        "Jellyfin.Plugin.HomeScreenSections.Models.SectionPayload",
                        "Jellyfin.Plugin.HomeScreenSections.Models.SectionRegistration",
                        "Jellyfin.Plugin.HomeScreenSections.SectionRegistration"
                    };

                    Type? payloadType = null;
                    foreach (var tn in candidateTypeNames)
                    {
                        payloadType = homeScreenSectionsAssembly.GetType(tn);
                        if (payloadType != null) break;
                    }

                    if (payloadType == null)
                    {
                        // fallback: look for a type with typical property names
                        payloadType = homeScreenSectionsAssembly.GetTypes()
                            .FirstOrDefault(t =>
                                t.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance) != null
                                && (t.GetProperty("DisplayText", BindingFlags.Public | BindingFlags.Instance) != null
                                    || t.GetProperty("Title", BindingFlags.Public | BindingFlags.Instance) != null));
                    }

                    if (payloadType == null)
                    {
                        LogWarn("PluginBootstrap: Could not locate a payload DTO type in HomeScreenSections assembly. Aborting typed payload attempt.");
                        return;
                    }

                    var payloadInstance = Activator.CreateInstance(payloadType);
                    if (payloadInstance == null)
                    {
                        LogWarn("PluginBootstrap: Failed to create instance of payload DTO type.");
                        return;
                    }

                    // Helper to set property if exists
                    void SetIfExists(string propName, object? value)
                    {
                        var pi = payloadType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                        if (pi != null && pi.CanWrite)
                        {
                            try { pi.SetValue(payloadInstance, Convert.ChangeType(value, pi.PropertyType)); }
                            catch
                            {
                                // fallback for string properties or JsonElement
                                try { pi.SetValue(payloadInstance, value); } catch { }
                            }
                        }
                    }

                    SetIfExists("Id", payload.id);
                    SetIfExists("Id", payload.id.ToString());
                    SetIfExists("DisplayText", payload.displayText);
                    SetIfExists("Title", payload.displayText);
                    SetIfExists("Limit", payload.limit);
                    SetIfExists("Route", payload.route);
                    SetIfExists("AdditionalData", payload.additionalData);
                    SetIfExists("ResultsAssembly", payload.resultsAssembly);
                    SetIfExists("ResultsClass", payload.resultsClass);
                    SetIfExists("ResultsMethod", payload.resultsMethod);

                    LogInfo("PluginBootstrap: invoking RegisterSection with typed payload instance.");
                    registerMethod.Invoke(null, new object?[] { payloadInstance });
                    LogInfo("PluginBootstrap: RegisterSection invoked successfully with typed payload.");

                    try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] RegisterSection invoked with typed payload{Environment.NewLine}"); } catch { }
                    try
                    {
                        var logType = Type.GetType("Serilog.Log, Serilog");
                        if (logType != null)
                        {
                            var loggerProp = logType.GetProperty("Logger", BindingFlags.Static | BindingFlags.Public);
                            var logger = loggerProp?.GetValue(null);
                            var mi = logger?.GetType().GetMethod("Information", new[] { typeof(string) });
                            mi?.Invoke(logger, new object[] { "[MyCustomSection] RegisterSection invoked with typed payload" });
                        }
                    }
                    catch { }

                    return;
                }
                catch (TargetInvocationException tie) when (tie.InnerException != null)
                {
                    LogError($"PluginBootstrap: target invocation threw an exception during typed payload attempt: {tie.InnerException}");
                    try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] Typed payload TargetInvocationException: {tie.InnerException}{Environment.NewLine}"); } catch { }
                }
                catch (Exception ex)
                {
                    LogError($"PluginBootstrap: unexpected error during typed payload attempt: {ex}");
                    try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] Typed payload EX: {ex}{Environment.NewLine}"); } catch { }
                }
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                LogError($"PluginBootstrap: target invocation threw an exception: {tie.InnerException}");
                try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] Outer TargetInvocationException: {tie.InnerException}{Environment.NewLine}"); } catch { }
            }
            catch (Exception ex)
            {
                LogError($"PluginBootstrap: unexpected error during RegisterSectionOnStartup: {ex}");
                try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] Outer EX: {ex}{Environment.NewLine}"); } catch { }
            }
            finally
            {
                try { System.IO.File.AppendAllText(debugPath, $"{DateTime.Now:O} [MyCustomSection] Exiting RegisterSectionOnStartup{Environment.NewLine}"); } catch { }
                try
                {
                    var logType = Type.GetType("Serilog.Log, Serilog");
                    if (logType != null)
                    {
                        var loggerProp = logType.GetProperty("Logger", BindingFlags.Static | BindingFlags.Public);
                        var logger = loggerProp?.GetValue(null);
                        var mi = logger?.GetType().GetMethod("Information", new[] { typeof(string) });
                        mi?.Invoke(logger, new object[] { "[MyCustomSection] Exiting RegisterSectionOnStartup" });
                    }
                }
                catch { }
            }
        }
    }
}
