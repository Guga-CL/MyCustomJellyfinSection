using System;
using System.Linq;
using System.Runtime.Loader;
using System.Reflection;
using System.Text.Json;
using MediaBrowser.Common.Logging;

namespace My.Custom.Section.Plugin
{
    public class PluginBootstrap
    {
        private readonly ILogger _logger;

        public PluginBootstrap(ILogger logger)
        {
            _logger = logger;
        }

        public void RegisterSectionOnStartup()
        {
            try
            {
                _logger.Info("PluginBootstrap: building payload for Home Screen Sections");

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
                _logger.Info($"PluginBootstrap: payload => {payloadJson}");

                // Diagnostic: list loaded assemblies so we can see what's available
                var loaded = AssemblyLoadContext.All
                    .SelectMany(ctx => ctx.Assemblies)
                    .Select(a => a.FullName)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToArray();

                _logger.Info($"PluginBootstrap: {loaded.Length} assemblies loaded. (first 10 shown)");
                foreach (var name in loaded.Take(10))
                {
                    _logger.Debug($"Loaded assembly: {name}");
                }

                // Try to find the HomeScreenSections assembly with multiple patterns
                var homeScreenSectionsAssembly = AssemblyLoadContext
                    .All
                    .SelectMany(ctx => ctx.Assemblies)
                    .FirstOrDefault(a =>
                        (a.FullName?.IndexOf(".HomeScreenSections", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                        (a.FullName?.IndexOf("HomeScreenSections", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                        (a.GetName().Name?.IndexOf("HomeScreenSections", StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);

                if (homeScreenSectionsAssembly == null)
                {
                    _logger.Warn("PluginBootstrap: HomeScreenSections assembly not found. Will retry with broader scan and fallback.");

                    // Fallback: try any assembly that exposes PluginInterface type
                    homeScreenSectionsAssembly = AssemblyLoadContext
                        .All
                        .SelectMany(ctx => ctx.Assemblies)
                        .FirstOrDefault(a => a.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface") != null);

                    if (homeScreenSectionsAssembly == null)
                    {
                        _logger.Warn("PluginBootstrap: HomeScreenSections plugin interface assembly still not found. Aborting registration.");
                        return;
                    }
                }

                _logger.Info($"PluginBootstrap: found candidate assembly: {homeScreenSectionsAssembly.FullName}");

                var pluginInterfaceType = homeScreenSectionsAssembly.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");

                if (pluginInterfaceType == null)
                {
                    _logger.Warn("PluginBootstrap: PluginInterface type not found on candidate assembly. Aborting registration.");
                    return;
                }

                var registerMethod = pluginInterfaceType.GetMethod("RegisterSection", BindingFlags.Public | BindingFlags.Static);

                if (registerMethod == null)
                {
                    _logger.Warn("PluginBootstrap: RegisterSection method not found on PluginInterface. Aborting registration.");
                    return;
                }

                // Convert payload JSON into a plain object compatible with the target method
                var payloadObj = JsonSerializer.Deserialize<object>(payloadJson)!;

                _logger.Info("PluginBootstrap: invoking RegisterSection on HomeScreenSections");
                registerMethod.Invoke(null, new object?[] { payloadObj });
                _logger.Info("PluginBootstrap: RegisterSection invoked successfully");
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                _logger.ErrorException("PluginBootstrap: target invocation threw an exception", tie.InnerException);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("PluginBootstrap: unexpected error during RegisterSectionOnStartup", ex);
            }
        }
    }
}
