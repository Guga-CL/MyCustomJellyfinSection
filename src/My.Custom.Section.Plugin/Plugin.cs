using System;
using System.Reflection;

namespace My.Custom.Section.Plugin
{
    // Reflection-based entry that compiles without Jellyfin types.
    // It will call PluginBootstrap.RegisterSectionOnStartup() if that type/method exists inside the plugin assembly.
    public class Plugin
    {
        public Plugin() { }

        public void Start()
        {
            try
            {
                // Try to find PluginBootstrap in the current assembly first
                var asm = Assembly.GetExecutingAssembly();
                var bootstrapType = asm.GetType("My.Custom.Section.Plugin.PluginBootstrap", throwOnError: false);

                // If not found in the same assembly, try to locate it by name from loaded assemblies
                if (bootstrapType == null)
                {
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        bootstrapType = a.GetType("My.Custom.Section.Plugin.PluginBootstrap", throwOnError: false);
                        if (bootstrapType != null) break;
                    }
                }

                if (bootstrapType != null)
                {
                    // Create instance; if your PluginBootstrap ctor signature differs adjust the args array accordingly
                    object? instance = null;
                    var ctor = bootstrapType.GetConstructor(new Type[] { typeof(object) }) ?? bootstrapType.GetConstructor(Type.EmptyTypes);
                    if (ctor != null)
                    {
                        instance = ctor.GetParameters().Length == 1 ? ctor.Invoke(new object?[] { null }) : ctor.Invoke(Array.Empty<object>());
                    } 
                    else
                    {
                        // fallback to Activator
                        instance = Activator.CreateInstance(bootstrapType);
                    }

                    var method = bootstrapType.GetMethod("RegisterSectionOnStartup", BindingFlags.Public | BindingFlags.Instance);
                    method?.Invoke(instance, null);
                    Console.WriteLine("My Custom Section Plugin: RegisterSectionOnStartup invoked (reflection).");
                }
                else
                {
                    Console.WriteLine("My Custom Section Plugin: PluginBootstrap type not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"My Custom Section Plugin: Start error: {ex}");
            }
        }

        public void Stop()
        {
            // no-op
        }
    }
}
