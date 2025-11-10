using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace My.Custom.Section.Plugin
{
    public class PluginBootstrap
    {
        private readonly ILogger<PluginBootstrap>? _logger;

        public PluginBootstrap(ILogger<PluginBootstrap>? logger = null)
        {
            _logger = logger;
            // Run registration asynchronously so constructor returns fast and startup isn't blocked
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1500); // small delay to avoid load-order races
                    LogInfo("[MyCustomSection] Scheduled RegisterSectionOnStartup attempt starting");
                    RegisterSectionOnStartup();
                    LogInfo("[MyCustomSection] Scheduled RegisterSectionOnStartup attempt finished");
                }
                catch (Exception ex)
                {
                    LogError($"[MyCustomSection] Scheduled RegisterSectionOnStartup failed: {ex}");
                }
            });
        }

        private void TryWriteFile(string text)
        {
            try
            {
                // Write next to the deployed plugin DLL (always writable by the process that loaded the plugin)
                var asm = Assembly.GetExecutingAssembly();
                var dir = System.IO.Path.GetDirectoryName(asm.Location) ?? @"C:\Temp";
                var path = System.IO.Path.Combine(dir, "jellyfin_plugin_debug.txt");
                System.IO.File.AppendAllText(path, $"{DateTime.Now:O} {text}{Environment.NewLine}");
            }
            catch
            {
            }
        }

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
            }

            return false;
        }

        private void LogDebug(string text)
        {
            if (!TryInvokeLogger("LogDebug", text) && !TryInvokeLogger("Debug", text))
            {
                Console.WriteLine(text);
            }
            TryWriteFile($"DEBUG {text}");
        }

        private void LogInfo(string text)
        {
            if (!TryInvokeLogger("LogInformation", text) && !TryInvokeLogger("Information", text) && !TryInvokeLogger("Info", text))
            {
                Console.WriteLine(text);
            }
            TryWriteFile($"INFO {text}");
        }

        private void LogWarn(string text)
        {
            if (!TryInvokeLogger("LogWarning", text) && !TryInvokeLogger("Warn", text))
            {
                Console.WriteLine("WARN: " + text);
            }
            TryWriteFile($"WARN {text}");
        }

        private void LogError(string text)
        {
            if (!TryInvokeLogger("LogError", text) && !TryInvokeLogger("Error", text))
            {
                Console.WriteLine("ERROR: " + text);
            }
            TryWriteFile($"ERROR {text}");
        }

        public void RegisterSectionOnStartup()
        {
            var debugHeader = "[MyCustomSection] RegisterSectionOnStartup";
            TryWriteFile($"{debugHeader} Entering");
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

                // Try 1: JSON string
                try
                {
                    LogInfo("PluginBootstrap: attempting RegisterSection with JSON string payload.");
                    registerMethod.Invoke(null, new object?[] { payloadJson });
                    LogInfo("PluginBootstrap: RegisterSection invoked successfully with JSON string.");
                    TryWriteFile($"{debugHeader} RegisterSection invoked with JSON");
                    return;
                }
                catch (TargetInvocationException tie)
                {
                    LogWarn($"PluginBootstrap: RegisterSection with JSON string threw: {tie.InnerException?.ToString() ?? tie.ToString()}");
                    TryWriteFile($"{debugHeader} JSON TargetInvocationException: {tie.InnerException}");
                }
                catch (Exception ex)
                {
                    LogWarn($"PluginBootstrap: RegisterSection with JSON string failed: {ex}");
                    TryWriteFile($"{debugHeader} JSON EX: {ex}");
                }

                // Try 2: typed payload via reflection
                try
                {
                    LogInfo("PluginBootstrap: attempting to construct typed payload object via reflection.");

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

                    void SetIfExists(string propName, object? value)
                    {
                        var pi = payloadType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                        if (pi != null && pi.CanWrite)
                        {
                            try { pi.SetValue(payloadInstance, Convert.ChangeType(value, pi.PropertyType)); }
                            catch
                            {
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
                    TryWriteFile($"{debugHeader} RegisterSection invoked with typed payload");
                    return;
                }
                catch (TargetInvocationException tie) when (tie.InnerException != null)
                {
                    LogError($"PluginBootstrap: target invocation threw an exception during typed payload attempt: {tie.InnerException}");
                    TryWriteFile($"{debugHeader} Typed payload TargetInvocationException: {tie.InnerException}");
                }
                catch (Exception ex)
                {
                    LogError($"PluginBootstrap: unexpected error during typed payload attempt: {ex}");
                    TryWriteFile($"{debugHeader} Typed payload EX: {ex}");
                }
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                LogError($"PluginBootstrap: outer target invocation threw an exception: {tie.InnerException}");
                TryWriteFile($"{debugHeader} Outer TargetInvocationException: {tie.InnerException}");
            }
            catch (Exception ex)
            {
                LogError($"PluginBootstrap: unexpected error during RegisterSectionOnStartup: {ex}");
                TryWriteFile($"{debugHeader} Outer EX: {ex}");
            }
            finally
            {
                TryWriteFile($"{debugHeader} Exiting");
            }
        }
    }
}
