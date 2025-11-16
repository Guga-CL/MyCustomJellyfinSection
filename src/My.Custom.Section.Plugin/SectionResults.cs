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
    public static class SectionResults
    {
        // Parameterless version — most likely what reflection is binding to
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static QueryResult<BaseItemDto> GetResults()
        {
            Console.WriteLine($"[MyCustomSection] GetResults() START {DateTime.UtcNow}");
            return BuildDummyResult();
        }

        // Keep other overloads just in case
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static QueryResult<BaseItemDto> GetResults(JObject request)
        {
            Console.WriteLine($"[MyCustomSection] GetResults(JObject) START {DateTime.UtcNow}");
            return BuildDummyResult();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static QueryResult<BaseItemDto> GetResults(object request)
        {
            Console.WriteLine($"[MyCustomSection] GetResults(object) START {DateTime.UtcNow}");
            return BuildDummyResult();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static QueryResult<BaseItemDto> GetResults(JToken request)
        {
            Console.WriteLine($"[MyCustomSection] GetResults(JToken) START {DateTime.UtcNow}");
            return BuildDummyResult();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static QueryResult<BaseItemDto> GetResults(Dictionary<string, object> request)
        {
            Console.WriteLine($"[MyCustomSection] GetResults(Dictionary) START {DateTime.UtcNow}");
            return BuildDummyResult();
        }

        private static QueryResult<BaseItemDto> BuildDummyResult()
        {
            var items = new List<BaseItemDto>
            {
                new BaseItemDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Hello Jellyfin!",
                    Type = BaseItemKind.Movie,
                    Overview = "Dummy item from plugin (with fake image tag).",
                    ProductionYear = DateTime.UtcNow.Year,
                    PremiereDate = DateTime.UtcNow,
                    ImageTags = new Dictionary<ImageType, string>
                    {
                        { ImageType.Primary, Guid.NewGuid().ToString() }
                    }
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
