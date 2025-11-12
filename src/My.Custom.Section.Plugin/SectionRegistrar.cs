using Newtonsoft.Json.Linq;
using System;
using System.IO;

namespace My.Custom.Section.Plugin
{
    internal static class SectionRegistrar
    {
        internal static JObject BuildPayload()
        {
            // Minimal payload that avoids referencing any Jellyfin DTOs
            var payload = new JObject
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["title"] = "My Custom Section",
                ["assembly"] = typeof(SectionRegistrar).Assembly.GetName().Name ?? "My.Custom.Section.Plugin"
            };

            // Write the payload JSON to the plugin debug file so you can inspect it on the server
            try
            {
                var payloadJson = payload.ToString();
                var baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "jellyfin",
                    "plugins",
                    "MyCustomSectionPlugin_1.0.0.0");
                Directory.CreateDirectory(baseDir);
                var dbg = Path.Combine(baseDir, "jellyfin_plugin_debug.txt");
                File.AppendAllText(dbg, $"{DateTime.UtcNow:O} BuildPayload output:{Environment.NewLine}{payloadJson}{Environment.NewLine}---{Environment.NewLine}");
            }
            catch
            {
                // Swallow any errors to avoid affecting plugin startup
            }

            return payload;
        }
    }
}
