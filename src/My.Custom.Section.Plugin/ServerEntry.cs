using MediaBrowser.Common.Plugins;

namespace My.Custom.Section.Plugin
{
    public class ServerEntry : BasePlugin
    {
        public override string Name => "My Custom Section";
        public override string Description => "Minimal test";

        public ServerEntry() { }
    }
}
