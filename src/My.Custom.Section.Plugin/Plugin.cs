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

    System.Threading.Tasks.Task.Run(async () =>
    {
        // Wait for HomeScreenSections to finish its Startup hook
        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5));

        try
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            var resultsClass = typeof(SectionResults).FullName;

            var payloadDict = new Dictionary<string, object>
            {
                // This is the ID the Modular Home settings page uses
                ["id"] = "mycustomsection",

                // Title shown in the Home screen UI
                ["displayText"] = "My Custom Section",

                // Reflection entry that points to your results method
                ["resultsAssembly"] = assemblyName,              // "MyCustomSectionPlugin"
                ["resultsClass"] = resultsClass,                 // "MyCustomJellyfinSection.SectionResults"
                ["resultsMethod"] = nameof(SectionResults.GetResults),

                // Renderer/layout
                ["type"] = "cards",                              // keep lower-case "cards" (this was visible before)

                // Optional metadata that can help grouping and ordering
                ["sectionType"] = "CustomSection",
                ["category"] = "Custom",
                ["order"] = 99,
                ["limit"] = 10,

                // Make it appear enabled by default for new users (optional)
                ["enabledByDefault"] = true
            };


            var hsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Jellyfin.Plugin.HomeScreenSections");
            var pluginInterfaceType = hsAssembly?.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");
            var registerMethod = pluginInterfaceType?.GetMethod("RegisterSection", BindingFlags.Public | BindingFlags.Static);

            if (registerMethod != null)
            {
                var jPayload = JObject.FromObject(payloadDict);
                registerMethod.Invoke(null, new object[] { jPayload });
                Console.WriteLine("[MyCustomSection] Section registered successfully (delayed).");
            }
            else
            {
                Console.WriteLine("[MyCustomSection] ERROR: RegisterSection method not found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[MyCustomSection] ERROR during registration: " + ex);
        }
    });
}
}}
