using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyCustomJellyfinSection
{
    public class StartupTask : IScheduledTask
    {
        public string Key => "MyCustomSectionStartup";
        public string Name => "MyCustomSection Startup Task";
        public string Description => "No-op startup task kept for diagnostics and future work.";
        public string Category => "MyCustomSection";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Keep this task harmless. Only log so you can run it manually for tests.
            Console.WriteLine("[MyCustomSection] StartupTask executed (no-op).");
            await Task.CompletedTask;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return Array.Empty<TaskTriggerInfo>();
        }
    }
}
