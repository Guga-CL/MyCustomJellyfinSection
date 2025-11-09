using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Common.Logging;

namespace My.Custom.Section.Plugin
{
    public class Plugin : BasePlugin, IServerEntryPoint
    {
        private readonly ILogger _logger;

        // Use the ctor signature that BasePlugin commonly exposes.
        // If this exact signature errors, remove parameters and use the minimal variant below.
        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer, ILogger logger)
            : base(appPaths, xmlSerializer)
        {
            _logger = logger;
        }

        public override string Name => "My Custom Section Plugin";
        public override string Description => "Registers a custom home section";

        public void Start()
        {
            _logger?.Info($"{Name}: Start called");
            try
            {
                var bootstrap = new PluginBootstrap(_logger);
                bootstrap.RegisterSectionOnStartup();
                _logger?.Info($"{Name}: RegisterSectionOnStartup finished");
            }
            catch (System.Exception ex)
            {
                _logger?.ErrorException($"{Name}: Start error", ex);
            }
        }

        public void Stop()
        {
            _logger?.Info($"{Name}: Stop called");
        }
    }
}
