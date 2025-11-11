using Newtonsoft.Json.Linq;
using System;

namespace My.Custom.Section.Plugin
{
    internal class ResultsHandler
    {
        public JObject Handle(object? input)
        {
            try
            {
                var j = new JObject { ["ok"] = true };
                return j;
            }
            catch (Exception ex)
            {
                return new JObject { ["ok"] = false, ["error"] = ex.Message };
            }
        }
    }
}
