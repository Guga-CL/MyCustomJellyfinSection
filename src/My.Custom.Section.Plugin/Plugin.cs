using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Diagnostics; //added for debugging, look for the: Debugger.Break();  can be removed later
using MediaBrowser.Controller.Plugins;

namespace MyCustomSectionPlugin
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        // Static constructor: runs once when the class is first loaded
        static Plugin()
        {
            System.Console.WriteLine("[MyCustomSection] Static constructor hit");
            System.Diagnostics.Debugger.Break();
        }
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            // Force debugger to break here
            // Debugger.Break();
            Console.WriteLine("[MyCustomSection] Plugin constructor hit");
            RegisterHomeScreenSection();
        }

        public override string Name => "My Custom Section";
        public override string Description => "Minimal test plugin to register a Home Screen Section.";
        public override Guid Id => Guid.Parse("aef0c16b-7e00-456c-b4df-0dc38c42e942");


        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "My Custom Section",
                    EmbeddedResourcePath = "MyCustomSectionPlugin.Configuration.configPage.html"
                }
            };
        }

        public static void RegisterHomeScreenSection()
        {
            var payload = new JObject
            {
                ["Section"] = "MyCustomSection",
                ["id"] = Guid.NewGuid().ToString(),
                ["displayText"] = "My Custom Section",
                ["limit"] = 1,
                ["route"] = "mycustomsection",
                ["additionalData"] = "extra-info",
                ["resultsAssembly"] = typeof(Plugin).Assembly.FullName,
                ["resultsClass"] = typeof(SectionResults).FullName,
                ["resultsMethod"] = nameof(SectionResults.GetResults),

                // Add this block so the section appears in /HomeScreen/Sections
                ["OriginalPayload"] = new JObject
                {
                    ["Name"] = "My Custom Section",
                    ["ServerId"] = Guid.NewGuid().ToString(),
                    ["Id"] = Guid.NewGuid().ToString(),
                    ["Type"] = "Folder",
                    ["Overview"] = "Dummy section from plugin",
                    ["CollectionType"] = "animes"
                }
            };

            var homeScreenSectionsAssembly = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => x.FullName?.Contains(".HomeScreenSections") ?? false);

            if (homeScreenSectionsAssembly != null)
            {
                var pluginInterfaceType = homeScreenSectionsAssembly.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");
                pluginInterfaceType?.GetMethod("RegisterSection")
                    ?.Invoke(null, new object?[] { payload });
            }
        }



    }

}
