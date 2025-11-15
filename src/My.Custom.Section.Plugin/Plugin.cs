using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace MyCustomJellyfinSection
{
    public class Plugin : BasePlugin<BasePluginConfiguration>
    {
        public override Guid Id => new Guid("aef0c16b-7e00-456c-b4df-0dc38c42e942");
        public override string Name => "My Custom Jellyfin Section";
        public override string Description => "Adds a custom section to the Jellyfin home screen.";

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Console.WriteLine("[MyCustomSection] Plugin constructor running...");

            // First attempt at 5s
            System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5));
                RegisterSectionSafe("first");
            });

            // Second attempt at 12s (catches late init)
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
                // Use the declaring type’s assembly name to avoid context differences.
                var assemblyName = typeof(SectionResults).Assembly.GetName().Name; // "MyCustomSectionPlugin"
                var resultsClass = typeof(SectionResults).FullName;                // "MyCustomJellyfinSection.SectionResults"

                var payloadDict = new Dictionary<string, object>
                {
                    ["id"] = "mycustomsection",
                    ["displayText"] = "My Custom Section",
                    ["resultsAssembly"] = assemblyName,
                    ["resultsClass"] = resultsClass,
                    ["resultsMethod"] = nameof(SectionResults.GetResults),
                    ["type"] = "cards",
                    ["sectionType"] = "CustomSection",
                    ["category"] = "Custom",
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
                    Console.WriteLine($"[MyCustomSection] ERROR: RegisterSection method not found (attempt: {attemptLabel}).");
                    return;
                }

                var jPayload = JObject.FromObject(payloadDict);
                Console.WriteLine($"[MyCustomSection] RegisterSection payload ({attemptLabel}): {jPayload.ToString(Newtonsoft.Json.Formatting.None)}");

                registerMethod.Invoke(null, new object[] { jPayload });
                Console.WriteLine($"[MyCustomSection] Section registered successfully ({attemptLabel}).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MyCustomSection] ERROR during registration ({attemptLabel}): {ex}");
            }
        }
    }
}
