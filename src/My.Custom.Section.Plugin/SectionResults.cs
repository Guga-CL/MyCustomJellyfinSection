using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Dto;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyCustomJellyfinSection
{
    public static class SectionResults
    {
        // Common synchronous signature accepting JObject
        public static QueryResult<BaseItemDto> GetResults(Newtonsoft.Json.Linq.JObject request)
        {
            return GetResultsInternal("JObject", request);
        }

        // Common synchronous signature accepting object (defensive)
        public static QueryResult<BaseItemDto> GetResults(object request)
        {
            return GetResultsInternal("object", request);
        }

        // Async variant that some hosts might expect
        public static Task<QueryResult<BaseItemDto>> GetResultsAsync(Newtonsoft.Json.Linq.JObject request)
        {
            return Task.FromResult(GetResultsInternal("GetResultsAsync(JObject)", request));
        }

        // Async variant with CancellationToken that some hosts might call
        public static Task<QueryResult<BaseItemDto>> GetResults(Newtonsoft.Json.Linq.JObject request, CancellationToken cancellationToken)
        {
            return Task.FromResult(GetResultsInternal("GetResults(JObject,CancellationToken)", request));
        }

        // Fallback name variants (some versions may look for this exact method)
        public static Task<QueryResult<BaseItemDto>> GetResultsAsync(object request)
        {
            return Task.FromResult(GetResultsInternal("GetResultsAsync(object)", request));
        }

        // Single internal implementation used by all public overloads
        private static QueryResult<BaseItemDto> GetResultsInternal(string sourceType, object request)
        {
            var start = DateTime.UtcNow;
            Console.WriteLine($"[MyCustomSection] GetResults START (called as {sourceType}) {start}");

            try
            {
                // Avoid image resolution by returning no ImageTags for diagnostics
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
            catch (Exception ex)
            {
                Console.WriteLine("[MyCustomSection] GetResults ERROR: " + ex);
                throw;
            }
            finally
            {
                Console.WriteLine("[MyCustomSection] GetResults END duration(ms): " + (DateTime.UtcNow - start).TotalMilliseconds);
            }
        }
    }
}
