using Newtonsoft.Json.Linq;
using System;

namespace My.Custom.Section.Plugin
{
    internal static class SectionRegistrar
    {
        internal static JObject BuildPayload()
        {
            var payload = new JObject
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["title"] = "My Custom Section",
                ["assembly"] = typeof(SectionRegistrar).Assembly.GetName().Name
            };
            return payload;
        }
    }
}
