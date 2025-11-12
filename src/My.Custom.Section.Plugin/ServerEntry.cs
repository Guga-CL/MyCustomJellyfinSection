using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Emby.Server.Implementations.Plugins; // adjust using to match your project


namespace My.Custom.Section.Plugin
{
    // Keep the same public class name/signature your plugin expects
    public class ServerEntry // : IServerEntry or base type your plugin uses
    {
        // Add to ServerEntry class
        public static void TryWriteDebug(string message, Exception? ex = null)
        {
            try
            {
                var path = Path.Combine(Path.GetTempPath(), "MyCustomSectionPlugin-init-error.txt");
                var text = $"{DateTime.UtcNow:O} - DEBUG: {message}";
                if (ex != null) text += $"{Environment.NewLine}{ex}";
                text += Environment.NewLine + "---" + Environment.NewLine;
                File.AppendAllText(path, text);
            }
            catch
            {
                // swallow
            }

        }

        private readonly ILogger _logger;

        // Lazy-safe expensive object example (XmlSerializer shown as example)
        private static readonly Lazy<object?> _maybeSerializer = new Lazy<object?>(() =>
        {
            try
            {
                // Replace with real serializer creation if needed.
                // Return null on failure so we don't throw at load time.
                // Example: return new XmlSerializer(typeof(SomeType));
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        });

        // Example: a resource that must be created later
        private object? _runtimeResource;

        // Public constructor: do minimal work only
        public ServerEntry(ILoggerFactory loggerFactory /*, other DI params if any */)
        {
            // Minimal constructor: save logger and do not perform heavy or failure-prone work here
            _logger = loggerFactory?.CreateLogger(typeof(ServerEntry).FullName ?? "ServerEntry") ?? NullLogger.Instance;

            // Very defensive: catch any unexpected exceptions and record them to disk for debugging
            try
            {
                // ONLY trivial non-throwing assignments here
                // If you have static initializers, convert them to Lazy<T> or move into InitializeAsync
            }
            catch (Exception ex)
            {
                SafeWriteError(ex);
                // Do not rethrow here; let the server create the instance. If you prefer to fail loudly during debugging, rethrow.
            }
        }

        // Public method to perform the plugin's real initialization after instantiation
        // Call this from the plugin lifecycle hook (OnLoad/OnApplicationStarted) or from CreatePluginInstance consumer when safe.
        public async Task InitializeAsync()
        {
            _logger.LogInformation("MyCustomSection: InitializeAsync starting");
            // or for file fallback
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "MyCustomSection-debug.txt"),
                $"{DateTime.UtcNow:O} InitializeAsync start{Environment.NewLine}");


            try
            {
                // Example: create or obtain the resource lazily and defensively
                _runtimeResource ??= CreateRuntimeResourceSafely();

                // If you actually need XmlSerializer, obtain from lazy wrapper and handle null
                var ser = _maybeSerializer.Value;
                if (ser == null)
                {
                    _logger.LogDebug("XmlSerializer stub not available; falling back to runtime serializer.");
                }

                // Perform remaining async initialization that might touch disk/network
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                SafeWriteError(ex);
                // Surface to logger as well
                _logger.LogError(ex, "Plugin InitializeAsync failed");
            }
        }

        // Example safe factory for resource
        private object CreateRuntimeResourceSafely()
        {
            try
            {
                // Put the code that used to run in the constructor here, but wrapped in try/catch
                return new object();
            }
            catch (Exception ex)
            {
                SafeWriteError(ex);
                return null!;
            }
        }

        // Helper that writes exception details to a known local file for quick debugging
        private static void SafeWriteError(Exception ex)
        {
            try
            {
                var path = Path.Combine(Path.GetTempPath(), "MyCustomSectionPlugin-init-error.txt");
                File.AppendAllText(path,
                    $"{DateTime.UtcNow:O} - Exception during plugin init: {ex.GetType()}: {ex.Message}{Environment.NewLine}{ex}{Environment.NewLine}---{Environment.NewLine}");
            }
            catch
            {
                // swallow any logging failures
            }
        }

        // NullLogger fallback so the constructor is safe even if logging isn't available
        private class NullLogger : ILogger
        {
            public static readonly NullLogger Instance = new NullLogger();
            // inside NullLogger class

            IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

            public IDisposable BeginScope<TState>(TState state) => NullScope.Instance; // optional helper

            public bool IsEnabled(LogLevel logLevel) => false;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
            private class NullScope : IDisposable { public static readonly NullScope Instance = new NullScope(); public void Dispose() { } }
        }
    }
}
