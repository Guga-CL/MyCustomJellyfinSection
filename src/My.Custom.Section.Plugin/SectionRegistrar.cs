using Newtonsoft.Json.Linq;
using System;

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
            return payload;
        }
    }
}
