using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace MyCustomJellyfinSection
{
    public class Plugin : BasePlugin<BasePluginConfiguration>
    {
        private readonly ILogger<Plugin> _logger;

        public override Guid Id => new Guid("aef0c16b-7e00-456c-b4df-0dc38c42e942");
        public override string Name => "My Custom Jellyfin Section";
        public override string Description => "Adds a custom section to the Jellyfin home screen.";

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<Plugin> logger)
            : base(applicationPaths, xmlSerializer)
        {
            _logger = logger;
            _logger.LogInformation("[MyCustomSection] Plugin constructor running...");

            System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5));
                RegisterSectionSafe("first");
            });

            System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(12));
                RegisterSectionSafe("second");
            });
        }

        private void RegisterSectionSafe(string attemptLabel)
        {
            try
            {
                var assemblyName = typeof(SectionResults).Assembly.GetName().Name;
                var resultsClass = typeof(SectionResults).FullName;
                var resultsMethod = nameof(SectionResults.GetResults);

                _logger.LogInformation("[MyCustomSection] Reflection strings ({Attempt}): AssemblyName={Assembly}, ResultsClass={Class}, ResultsMethod={Method}",
                    attemptLabel, assemblyName, resultsClass, resultsMethod);

                var payloadDict = new Dictionary<string, object>
                {
                    ["id"] = "myCustomSection", // camelCase id
                    ["displayText"] = "My Custom Section",
                    ["resultsAssembly"] = assemblyName,
                    ["resultsClass"] = resultsClass,
                    ["resultsMethod"] = resultsMethod,
                    ["type"] = "cards",
                    ["sectionType"] = "customSection",
                    ["category"] = "custom",
                    ["order"] = 99,
                    ["limit"] = 10,
                    ["enabledByDefault"] = true
                };

                var hsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Jellyfin.Plugin.HomeScreenSections");
                var pluginInterfaceType = hsAssembly?.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");
                var registerMethod = pluginInterfaceType?.GetMethod("RegisterSection", BindingFlags.Public | BindingFlags.Static);

                if (registerMethod == null)
                {
                    _logger.LogError("[MyCustomSection] ERROR: RegisterSection method not found (attempt: {Attempt}).", attemptLabel);
                    return;
                }

                var jPayload = JObject.FromObject(payloadDict);
                _logger.LogInformation("[MyCustomSection] RegisterSection payload ({Attempt}): {Payload}", attemptLabel, jPayload.ToString(Newtonsoft.Json.Formatting.None));

                registerMethod.Invoke(null, new object[] { jPayload });
                _logger.LogInformation("[MyCustomSection] Section registered successfully ({Attempt}).", attemptLabel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MyCustomSection] ERROR during registration ({Attempt})", attemptLabel);
            }
        }
    }
}
