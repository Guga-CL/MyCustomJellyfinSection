using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Dto;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MyCustomJellyfinSection
{
    public static class SectionResults
    {
        // Main entry point: HomeScreenSections will call this
        public static QueryResult<BaseItemDto> GetResults(JObject request)
        {
            return GetResultsInternal("JObject", request);
        }

        // Defensive overload in case the host calls with object
        public static QueryResult<BaseItemDto> GetResults(object request)
        {
            return GetResultsInternal("object", request);
        }

        private static QueryResult<BaseItemDto> GetResultsInternal(string sourceType, object request)
        {
            Console.WriteLine($"[MyCustomSection] GetResults START (called as {sourceType}) {DateTime.UtcNow}");

            var items = new List<BaseItemDto>
            {
                new BaseItemDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Hello Jellyfin!",
                    Type = BaseItemKind.Movie,
                    Overview = "Dummy item from plugin (no images).",
                    ProductionYear = DateTime.UtcNow.Year,
                    PremiereDate = DateTime.UtcNow,
                    ImageTags = null
                }
            };

            Console.WriteLine("[MyCustomSection] Built items count: " + items.Count);

            return new QueryResult<BaseItemDto>
            {
                Items = items,
                TotalRecordCount = items.Count
            };
        }
    }
}
