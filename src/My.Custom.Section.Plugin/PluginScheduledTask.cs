using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyCustomSectionPlugin
{
    public class StartupTask : IScheduledTask
    {
        public string Key => "MyCustomSectionStartup";
        public string Name => "MyCustomSection Startup Task";
        public string Description => "Runs once at server startup to test plugin execution.";
        public string Category => "MyCustomSection";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            Console.WriteLine("[MyCustomSection] ScheduledTask ExecuteAsync() called at startup");

            Plugin.RegisterHomeScreenSection(); // now static

            await Task.CompletedTask;
        }


        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Return an empty list: Jellyfin will run this once at startup
            return Array.Empty<TaskTriggerInfo>();
        }
    }
}
