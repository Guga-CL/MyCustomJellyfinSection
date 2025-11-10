using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace My.Custom.Section.Plugin
{
    public sealed class PluginBootstrap
    {
        private readonly string _debugFileName = "jellyfin_plugin_debug.txt";
        private readonly string _pluginId = "11111111-2222-3333-4444-555555555555";
        private readonly string _displayText = "My Custom Section";
        private readonly int _limit = 1;
        private readonly string _route = "/my-custom-section";
        private readonly string _resultsAssembly = "My.Custom.Section.Plugin, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
        private readonly string _resultsClass = "My.Custom.Section.Plugin.ResultsHandler";
        private readonly string _resultsMethod = "GetSectionResults";

        public PluginBootstrap(object? placeholder) { }

        // Public entry used by ServerEntry/BasePlugin
        public void RegisterSectionOnStartup()
        {
            TryWrite("[MyCustomSection] RegisterSectionOnStartup Entering");

            try
            {
                var payloadJson = BuildPayloadJson();
                TryWrite("INFO PluginBootstrap: building payload for Home Screen Sections");
                TryWrite("INFO PluginBootstrap: payload => " + payloadJson);

                // Find the HomeScreenSections plugin assembly / type/ method
                var candidate = FindHomeScreenSectionsMethod();
                if (candidate == null)
                {
                    TryWrite("WARN PluginBootstrap: HomeScreenSections Register method not found. Exiting.");
                    return;
                }

                var (pluginInstance, methodInfo) = candidate.Value;

                // First attempt: invoke with JObject (if method expects JObject)
                TryInvokeWithBestArgument(pluginInstance, methodInfo, payloadJson);

                TryWrite("[MyCustomSection] RegisterSectionOnStartup Exiting");
            }
            catch (Exception ex)
            {
                TryWrite("[MyCustomSection] RegisterSectionOnStartup Exception: " + ex);
            }
        }

        // Build the simple JSON string payload (kept identical shape used previously)
        private string BuildPayloadJson()
        {
            var payload = new
            {
                id = _pluginId,
                displayText = _displayText,
                limit = _limit,
                route = _route,
                additionalData = "{}",
                resultsAssembly = _resultsAssembly,
                resultsClass = _resultsClass,
                resultsMethod = _resultsMethod
            };

            return JsonConvert.SerializeObject(payload);
        }

        // Try to find a Register method in Jellyfin.Plugin.HomeScreenSections or compatible assembly.
        // Returns an instance (or null) and the MethodInfo to call.
        private (object? instance, MethodInfo method)? FindHomeScreenSectionsMethod()
        {
            try
            {
                // Search loaded assemblies for a candidate assembly name and types that contain RegisterSection
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var candidateAssemblies = assemblies
                    .Where(a => a.GetName().Name?.IndexOf("HomeScreenSections", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                a.GetName().Name?.IndexOf("Jellyfin.Plugin.HomeScreenSections", StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();

                // Fallback: search assemblies by types that mention SectionRegisterPayload or RegisterSection method
                if (candidateAssemblies.Length == 0)
                {
                    candidateAssemblies = assemblies
                        .Where(a => a.GetTypes().Any(t =>
                            t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                             .Any(m => m.Name.IndexOf("Register", StringComparison.OrdinalIgnoreCase) >= 0)))
                        .ToArray();
                }

                foreach (var asm in candidateAssemblies)
                {
                    try
                    {
                        var types = asm.GetTypes();
                        foreach (var type in types)
                        {
                            // Find a public/static/instance method named "RegisterSection" or "Register"
                            var method = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                                             .FirstOrDefault(m => string.Equals(m.Name, "RegisterSection", StringComparison.OrdinalIgnoreCase) ||
                                                                  string.Equals(m.Name, "Register", StringComparison.OrdinalIgnoreCase));
                            if (method == null) continue;

                            object? instance = null;
                            if (!method.IsStatic)
                            {
                                try
                                {
                                    // Try to create an instance if there's a parameterless constructor
                                    var ctor = type.GetConstructor(Type.EmptyTypes);
                                    if (ctor != null) instance = ctor.Invoke(null);
                                }
                                catch { instance = null; }
                            }

                            TryWrite($"INFO PluginBootstrap: invoking RegisterSection candidate: {type.FullName}.{method.Name}");
                            return (instance, method);
                        }
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // ignore assemblies that cannot be reflected fully
                    }
                }

                // If we fall through, no candidate found
                return null;
            }
            catch (Exception ex)
            {
                TryWrite("ERROR PluginBootstrap: FindHomeScreenSectionsMethod exception: " + ex);
                return null;
            }
        }

        // Central invocation helper: prepares the single argument value to match the method parameter
        private void TryInvokeWithBestArgument(object? pluginInstance, MethodInfo method, string payloadJson)
        {
            try
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 0)
                {
                    // No parameters; just invoke
                    method.Invoke(pluginInstance, null);
                    TryWrite("INFO PluginBootstrap: invoked method without parameters.");
                    return;
                }

                var paramType = parameters[0].ParameterType;

                // If parameter expects JObject, provide JObject
                if (paramType == typeof(JObject) || paramType.FullName == "Newtonsoft.Json.Linq.JObject")
                {
                    TryWrite("INFO PluginBootstrap: attempting RegisterSection with JObject payload.");
                    var j = JObject.Parse(payloadJson);
                    method.Invoke(pluginInstance, new object[] { j });
                    TryWrite("INFO PluginBootstrap: JObject invocation succeeded.");
                    return;
                }

                // If parameter is string
                if (paramType == typeof(string))
                {
                    TryWrite("INFO PluginBootstrap: attempting RegisterSection with string payload.");
                    method.Invoke(pluginInstance, new object[] { payloadJson });
                    TryWrite("INFO PluginBootstrap: string invocation succeeded.");
                    return;
                }

                // If parameter type is present in loaded assemblies, try to map to that typed object.
                try
                {
                    var typedPayload = ConstructTypedPayload(paramType);
                    if (typedPayload != null && paramType.IsAssignableFrom(typedPayload.GetType()))
                    {
                        TryWrite("INFO PluginBootstrap: attempting RegisterSection with typed payload instance.");
                        method.Invoke(pluginInstance, new object[] { typedPayload });
                        TryWrite("INFO PluginBootstrap: typed payload invocation succeeded.");
                        return;
                    }

                    // Last resort: convert typed payload to JObject if param wants JObject-like value (covered earlier),
                    // or try to convert from JObject to paramType via ToObject
                    var jFromTyped = JObject.FromObject(typedPayload ?? new { });
                    if (paramType.IsClass)
                    {
                        var converted = jFromTyped.ToObject(paramType);
                        if (converted != null)
                        {
                            TryWrite("INFO PluginBootstrap: attempting RegisterSection after converting JObject->typed param.");
                            method.Invoke(pluginInstance, new object[] { converted });
                            TryWrite("INFO PluginBootstrap: converted invocation succeeded.");
                            return;
                        }
                    }
                }
                catch (TargetInvocationException tie)
                {
                    // bubble to outer catch
                    throw tie;
                }

                // If nothing worked, try invoking by passing JObject.ToString() (fallback)
                TryWrite("WARN PluginBootstrap: no exact match for parameter type; attempting fallback with JSON string.");
                method.Invoke(pluginInstance, new object[] { payloadJson });
            }
            catch (TargetInvocationException ex)
            {
                // Unwrap inner exception to log clearer reason
                TryWrite("ERROR PluginBootstrap: invocation TargetInvocationException: " + ex.InnerException?.ToString() ?? ex.ToString());
                throw;
            }
            catch (ArgumentException argEx)
            {
                TryWrite("WARN PluginBootstrap: RegisterSection argument type mismatch: " + argEx);
                throw;
            }
            catch (Exception ex)
            {
                TryWrite("ERROR PluginBootstrap: unexpected error during invocation: " + ex);
                throw;
            }
        }

        // Build an instance of the payload type when possible (very small, best-effort mapper)
        private object? ConstructTypedPayload(Type targetType)
        {
            try
            {
                // Quick path: if targetType name contains SectionRegisterPayload, try to use its default ctor and set common props
                if (targetType.Name.IndexOf("SectionRegisterPayload", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var instance = Activator.CreateInstance(targetType);
                    if (instance == null) return null;

                    // Try to set common properties by name (best-effort)
                    SetPropertyIfExists(targetType, instance, "Id", _pluginId);
                    SetPropertyIfExists(targetType, instance, "DisplayText", _displayText);
                    SetPropertyIfExists(targetType, instance, "Limit", _limit);
                    SetPropertyIfExists(targetType, instance, "Route", _route);
                    SetPropertyIfExists(targetType, instance, "AdditionalData", "{}");
                    SetPropertyIfExists(targetType, instance, "ResultsAssembly", _resultsAssembly);
                    SetPropertyIfExists(targetType, instance, "ResultsClass", _resultsClass);
                    SetPropertyIfExists(targetType, instance, "ResultsMethod", _resultsMethod);

                    return instance;
                }

                // If target type is a simple POCO in another assembly, try JObject.FromObject -> ToObject
                var j = JObject.Parse(BuildPayloadJson());
                var converted = j.ToObject(targetType);
                return converted;
            }
            catch (Exception ex)
            {
                TryWrite("WARN PluginBootstrap: ConstructTypedPayload failed: " + ex);
                return null;
            }
        }

        private void SetPropertyIfExists(Type t, object instance, string propName, object value)
        {
            try
            {
                var p = t.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null && p.CanWrite)
                {
                    // Convert.ChangeType may throw for complex types; rely on JToken conversion if needed
                    var converted = ConvertIfNeeded(value, p.PropertyType);
                    p.SetValue(instance, converted);
                }
            }
            catch { /* best-effort */ }
        }

        private object? ConvertIfNeeded(object value, Type targetType)
        {
            try
            {
                if (value == null) return null;
                if (targetType.IsAssignableFrom(value.GetType())) return value;
                if (targetType == typeof(string)) return value.ToString();
                if (targetType.IsEnum && value is string s)
                {
                    return Enum.Parse(targetType, s);
                }
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                try
                {
                    // Fallback via JObject conversion
                    var j = JToken.FromObject(value);
                    return j.ToObject(targetType);
                }
                catch
                {
                    return null;
                }
            }
        }

        // Debug/write helper: writes next to executing assembly if possible
        private void TryWrite(string text)
        {
            try
            {
                var asmPath = Assembly.GetExecutingAssembly().Location;
                var dir = Path.GetDirectoryName(asmPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var path = Path.Combine(dir, _debugFileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? dir);
                File.AppendAllText(path, $"{DateTime.Now:O} {text}{Environment.NewLine}");
            }
            catch { }
        }
    }
}
