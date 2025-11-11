using System;
using Newtonsoft.Json.Linq;

namespace My.Custom.Section.Plugin
{
    public static class SectionRegistrar
    {
        public static JObject BuildPayload()
        {
            return new JObject
            {
                ["id"] = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                ["displayText"] = "My Custom Section",
                ["limit"] = 1,
                ["route"] = "/my-custom-section",
                ["additionalData"] = "{}",
                ["resultsAssembly"] = typeof(SectionRegistrar).Assembly.FullName,
                ["resultsClass"] = "My.Custom.Section.Plugin.ResultsHandler",
                ["resultsMethod"] = "GetSectionResults"
            };
        }
    }
}
