using System;
using Newtonsoft.Json.Linq;

namespace My.Custom.Section.Plugin
{
    internal class ResultsHandler
    {
        public ResultsHandler() { }

        // Return a JObject payload that HomeScreenSections can handle as an alternative to QueryResult<BaseItemDto>.
        // Keep this minimal and defensive.
        public object GetSectionResults(object requestModel)
        {
            try
            {
                // Return an empty result that HomeScreenSections will accept.
                // You can later return a typed QueryResult<BaseItemDto> if you restore the model reference.
                var result = new JObject
                {
                    ["Items"] = new JArray(),
                    ["TotalRecordCount"] = 0
                };
                return result;
            }
            catch
            {
                return new JObject
                {
                    ["Items"] = new JArray(),
                    ["TotalRecordCount"] = 0
                };
            }
        }
    }
}
