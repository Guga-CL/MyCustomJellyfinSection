using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;

namespace My.Custom.Section.Plugin
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer) { }

        public override string Name => "My Custom Section";
        public override string Description => "Minimal test plugin to register a Home Screen Section.";
        public override Guid Id => Guid.Parse("9194a4db-c196-44ab-bf6a-b6c54da9414a");

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "MyCustomSection",
                    EmbeddedResourcePath = "My.Custom.Section.Plugin.Configuration.configPage.html"
                }
            };
        }
    }
}
