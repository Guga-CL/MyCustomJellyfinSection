using System.Collections.Generic;

namespace My.Custom.Section.Plugin
{
    public class ResultsHandler
    {
        // Temporary, for the plugin to just compile 
        public static object GetSectionResults(object request)
        {
            return new
            {
                Items = new object[0],
                TotalRecordCount = 0
            };
        }
    }
}
