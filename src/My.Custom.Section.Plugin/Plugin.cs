using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace My.Custom.Section.Plugin
{
    // Internal helper invoked optionally by hosting code. Keeps reflection strictly inside Start.
    internal class Plugin
    {
        public Plugin()
        {
        }

        public void Start()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                var bootstrapType = asm.GetType("My.Custom.Section.Plugin.PluginBootstrap", throwOnError: false);
                if (bootstrapType == null)
                {
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            bootstrapType = a.GetType("My.Custom.Section.Plugin.PluginBootstrap", throwOnError: false);
                            if (bootstrapType != null) break;
                        }
                        catch { /* ignore */ }
                    }
                }

                if (bootstrapType == null)
                {
                    Log("Plugin.Start: PluginBootstrap type not found");
                }
                else
                {
                    object? instance = null;
                    try
                    {
                        var ctor = bootstrapType.GetConstructor(new Type[] { typeof(object) }) ?? bootstrapType.GetConstructor(Type.EmptyTypes);
                        if (ctor != null)
                        {
                            instance = ctor.GetParameters().Length == 1 ? ctor.Invoke(new object?[] { null }) : ctor.Invoke(Array.Empty<object>());
                        }
                        else
                        {
                            instance = Activator.CreateInstance(bootstrapType);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Plugin.Start: failed to construct PluginBootstrap: {ex.GetType().FullName}: {ex.Message}");
                    }

                    try
                    {
                        var method = bootstrapType.GetMethod("RegisterSectionOnStartup", BindingFlags.Public | BindingFlags.Instance);
                        method?.Invoke(instance, null);
                        Log("Plugin.Start: RegisterSectionOnStartup invoked (reflection).");
                    }
                    catch (Exception ex)
                    {
                        Log($"Plugin.Start: invoking RegisterSectionOnStartup failed: {ex.GetType().FullName}: {ex.Message}");
                    }
                }

                // Schedule registration via our safe path after a short delay to ensure dependent plugins/services are up.
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));

                        try
                        {
                            var payload = SectionRegistrar.BuildPayload();
                            HomeScreenRegistrationInvoker.InvokeRegisterSection(payload);
                            Log("Plugin.Start: HomeScreen registration invoked via SectionRegistrar/HomeScreenRegistrationInvoker.");
                        }
                        catch (Exception ex)
                        {
                            Log($"Plugin.Start: HomeScreen registration attempt failed: {ex.GetType().FullName}: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Plugin.Start: background registration task failed: {ex.GetType().FullName}: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Plugin.Start: unexpected error: {ex.GetType().FullName}: {ex.Message}");
            }
        }

        public void Stop()
        {
            /* no-op */
        }

        private static void Log(string message)
        {
            try
            {
                var baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "jellyfin",
                    "plugins",
                    "MyCustomSectionPlugin_1.0.0.0");
                Directory.CreateDirectory(baseDir);
                var p = Path.Combine(baseDir, "jellyfin_plugin_debug.txt");
                File.AppendAllText(p, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}", Encoding.UTF8);
            }
            catch { }
        }
    }
}
