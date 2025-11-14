using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Dto;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities; 
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MyCustomSectionPlugin
{
    public static class SectionResults
    {
        // Must accept JObject, not object
        public static QueryResult<BaseItemDto> GetResults(JObject request)
        {
            var items = new List<BaseItemDto>
            {
                new BaseItemDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Hello Jellyfin!",
                    Type = BaseItemKind.Movie,
                    Overview = "This is a dummy item from My Custom Section plugin.",
                    ProductionYear = 2025,
                    PremiereDate = DateTime.UtcNow,
                    ImageTags = new Dictionary<ImageType,string>
                    {
                        { ImageType.Primary, Guid.NewGuid().ToString() }
                    }
                }
            };

            return new QueryResult<BaseItemDto>
            {
                Items = items,
                TotalRecordCount = items.Count
            };
        }
    }
}
