using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace My.Custom.Section.Plugin
{
    // Small, static helper class used by ServerEntry to find the HomeScreenSections plugin and invoke its registration method.
    internal static class PluginBootstrap
    {
        private const string DebugFileName = "jellyfin_plugin_debug.txt";

        // Find a candidate assembly that looks like the HomeScreenSections plugin.
        public static Assembly? FindHomeScreenSectionsAssembly()
        {
            try
            {
                return AssemblyLoadContext.All
                    .SelectMany(ctx => ctx.Assemblies)
                    .FirstOrDefault(a => a.GetName().Name?.IndexOf("HomeScreenSections", StringComparison.OrdinalIgnoreCase) >= 0
                                         || a.GetName().Name?.IndexOf("Jellyfin.Plugin.HomeScreenSections", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            catch
            {
                TryWrite("FindHomeScreenSectionsAssembly: exception while scanning assemblies");
                return null;
            }
        }

        // Attempt to find a suitable method to register a section and invoke it with the provided payload.
        // payloadJson should be a compact JSON string describing the section.
        public static void TryRegisterSectionWithPayload(string payloadJson)
        {
            try
            {
                var asm = FindHomeScreenSectionsAssembly();
                if (asm == null)
                {
                    TryWrite("TryRegisterSectionWithPayload: HomeScreenSections assembly not found");
                    return;
                }

                // Look for any type that offers a RegisterSection or Register method
                var candidateType = asm.GetTypes().FirstOrDefault(t =>
                    t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                     .Any(m => string.Equals(m.Name, "RegisterSection", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(m.Name, "Register", StringComparison.OrdinalIgnoreCase)));

                if (candidateType == null)
                {
                    TryWrite("TryRegisterSectionWithPayload: No candidate type with RegisterSection/Register found");
                    return;
                }

                // Prefer a static method if available
                var method = candidateType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                    .FirstOrDefault(m => string.Equals(m.Name, "RegisterSection", StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(m.Name, "Register", StringComparison.OrdinalIgnoreCase));

                if (method == null)
                {
                    TryWrite($"TryRegisterSectionWithPayload: No Register method reflection for {candidateType.FullName}");
                    return;
                }

                object? instance = null;
                if (!method.IsStatic)
                {
                    // attempt to create an instance with a parameterless ctor
                    var ctor = candidateType.GetConstructor(Type.EmptyTypes);
                    if (ctor != null)
                    {
                        try { instance = ctor.Invoke(null); }
                        catch { instance = null; }
                    }
                }

                // Try invoking with the best match argument type
                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                {
                    method.Invoke(instance, null);
                    TryWrite($"Invoked {candidateType.FullName}.{method.Name} without parameters");
                    return;
                }

                var paramType = parameters[0].ParameterType;

                // If the method expects JObject (Newtonsoft), try that
                if (paramType.FullName == "Newtonsoft.Json.Linq.JObject" || paramType == typeof(JObject))
                {
                    var j = JObject.Parse(payloadJson);
                    method.Invoke(instance, new object[] { j });
                    TryWrite($"Invoked {candidateType.FullName}.{method.Name} with JObject");
                    return;
                }

                // If the method expects a string, pass the JSON string
                if (paramType == typeof(string))
                {
                    method.Invoke(instance, new object[] { payloadJson });
                    TryWrite($"Invoked {candidateType.FullName}.{method.Name} with JSON string");
                    return;
                }

                // Try to convert the JSON to the expected type via JObject -> ToObject
                try
                {
                    var j = JObject.Parse(payloadJson);
                    var converted = j.ToObject(paramType);
                    if (converted != null && paramType.IsAssignableFrom(converted.GetType()))
                    {
                        method.Invoke(instance, new object[] { converted });
                        TryWrite($"Invoked {candidateType.FullName}.{method.Name} with converted typed payload ({paramType.FullName})");
                        return;
                    }
                }
                catch (Exception convEx)
                {
                    TryWrite($"Conversion attempt failed: {convEx}");
                }

                // Fallback: try passing the raw JSON string
                try
                {
                    method.Invoke(instance, new object[] { payloadJson });
                    TryWrite($"Invoked {candidateType.FullName}.{method.Name} fallback with JSON string");
                }
                catch (Exception invokeEx)
                {
                    TryWrite($"Final invoke attempt failed: {invokeEx}");
                }
            }
            catch (Exception ex)
            {
                TryWrite($"TryRegisterSectionWithPayload top-level exception: {ex}");
            }
        }

        // Write a short debug line next to the plugin DLL (non-fatal)
        private static void TryWrite(string text)
        {
            try
            {
                var asmPath = Assembly.GetExecutingAssembly().Location;
                var dir = Path.GetDirectoryName(asmPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var path = Path.Combine(dir, DebugFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? dir);
                File.AppendAllText(path, $"{DateTime.UtcNow:O} {text}{Environment.NewLine}");
            }
            catch { /* ignore */ }
        }
    }
}
