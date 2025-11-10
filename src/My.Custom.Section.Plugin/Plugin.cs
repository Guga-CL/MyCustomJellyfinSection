using System;

namespace My.Custom.Section.Plugin
{
    // Minimal entrypoint: expose Start/Stop so we can wire into Jellyfin lifecycle.
    // This avoids depending on IServerEntryPoint or BasePlugin at compile time.
    public class Plugin
    {
        public Plugin()
        {
            // Keep constructor minimal to avoid BasePlugin ctor mismatches
        }

        // Public method named Start so it can be discovered/called in many plugin lifecycles.
        public void Start()
        {
            try
            {
                var bootstrap = new PluginBootstrap(null);
                bootstrap.RegisterSectionOnStartup();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"My Custom Section Plugin: Start error: {ex}");
            }
        }

        // Public Stop method for symmetry
        public void Stop()
        {
            // no-op for now
        }
    }
}
