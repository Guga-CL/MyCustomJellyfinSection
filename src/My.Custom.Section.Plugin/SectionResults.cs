using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Dto;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MyCustomJellyfinSection
{
    // Ensure discoverability for reflection
    public static class SectionResults
    {
        // Strong hint to the JIT not to inline / fold this away
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static QueryResult<BaseItemDto> GetResults(JObject request)
        {
            Console.WriteLine($"[MyCustomSection] GetResults START {DateTime.UtcNow}");
            Console.WriteLine($"[MyCustomSection] Request type: {(request == null ? "null" : request.GetType().FullName)}");

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
