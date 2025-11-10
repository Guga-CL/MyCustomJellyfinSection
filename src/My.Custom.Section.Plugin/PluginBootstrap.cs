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

        public void RegisterSectionOnStartup()
        {
            try
            {
                LogInfo("PluginBootstrap: building payload for Home Screen Sections");

                var payload = new
                {
                    id = "11111111-2222-3333-4444-555555555555",
                    displayText = "My Custom Section",
                    limit = 1,
                    route = "",
                    additionalData = "",
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

                var homeScreenSectionsAssembly = AssemblyLoadContext
                    .All
                    .SelectMany(ctx => ctx.Assemblies)
                    .FirstOrDefault(a =>
                        (a.FullName?.IndexOf(".HomeScreenSections", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                        (a.FullName?.IndexOf("HomeScreenSections", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                        (a.GetName().Name?.IndexOf("HomeScreenSections", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);

                if (homeScreenSectionsAssembly == null)
                {
                    LogWarn("PluginBootstrap: HomeScreenSections assembly not found. Will retry with broader scan and fallback.");

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

                var payloadObj = JsonSerializer.Deserialize<object>(payloadJson)!;

                LogInfo("PluginBootstrap: invoking RegisterSection on HomeScreenSections");
                registerMethod.Invoke(null, new object?[] { payloadObj });
                LogInfo("PluginBootstrap: RegisterSection invoked successfully");
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                LogError($"PluginBootstrap: target invocation threw an exception: {tie.InnerException}");
            }
            catch (Exception ex)
            {
                LogError($"PluginBootstrap: unexpected error during RegisterSectionOnStartup: {ex}");
            }
        }

        // TryInvokeLogger now returns true if it invoked something, false otherwise
        private bool TryInvokeLogger(string methodName, params object[] args)
        {
            if (_logger == null) return false;

            try
            {
                // Try exact parameter match
                var m = _logger.GetType().GetMethod(methodName, args.Select(a => a?.GetType() ?? typeof(object)).ToArray());
                if (m != null)
                {
                    m.Invoke(_logger, args);
                    return true;
                }

                // Try a single string parameter overload
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

        private void LogDebug(string text)
        {
            if (!TryInvokeLogger("Debug", text)) Console.WriteLine(text);
        }

        private void LogInfo(string text)
        {
            if (!TryInvokeLogger("Info", text)) Console.WriteLine(text);
        }

        private void LogWarn(string text)
        {
            if (!TryInvokeLogger("Warn", text)) Console.WriteLine("WARN: " + text);
        }

        private void LogError(string text)
        {
            if (!TryInvokeLogger("Error", text)) Console.WriteLine("ERROR: " + text);
        }
    }
}
