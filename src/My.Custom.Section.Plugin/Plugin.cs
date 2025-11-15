using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace MyCustomJellyfinSection
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        // Use the same GUID from meta.json
        public override Guid Id => new Guid("aef0c16b-7e00-456c-b4df-0dc38c42e942");

        public override string Name => "My Custom Jellyfin Section";
        public override string Description => "Adds a custom section to the Jellyfin home screen.";

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Console.WriteLine("[MyCustomSection] Plugin constructor running...");

            try
            {
                // Determine runtime assembly/type values explicitly and log them
                var assemblyName = Assembly.GetExecutingAssembly().GetName().Name; // expected "MyCustomSectionPlugin"
                var resultsClass = typeof(SectionResults).FullName; // expected "MyCustomJellyfinSection.SectionResults"
                Console.WriteLine("[MyCustomSection] Computed assemblyName=" + assemblyName + ", resultsClass=" + resultsClass);

                var payloadDict = new Dictionary<string, object>
                {
                    ["id"] = "aef0c16b-7e00-456c-b4df-0dc38c42e942",
                    ["displayText"] = "My Custom Section",
                    ["route"] = "/web/index.html#!/mycustomsection",
                    ["resultsAssembly"] = assemblyName,
                    ["resultsClass"] = resultsClass,
                    ["limit"] = 1
                };

                // Find HomeScreenSections assembly loaded into the AppDomain
                var hsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Jellyfin.Plugin.HomeScreenSections");

                Console.WriteLine("[MyCustomSection] Found HomeScreenSections assembly: " + (hsAssembly?.FullName ?? "<null>"));

                var pluginInterfaceType = hsAssembly?.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");
                Console.WriteLine("[MyCustomSection] PluginInterface type: " + (pluginInterfaceType?.FullName ?? "<null>"));

                var registerMethod = pluginInterfaceType?.GetMethod("RegisterSection", BindingFlags.Public | BindingFlags.Static);
                Console.WriteLine("[MyCustomSection] RegisterSection method: " + (registerMethod?.ToString() ?? "<null>"));

                if (registerMethod != null)
                {
                    var paramType = registerMethod.GetParameters().FirstOrDefault()?.ParameterType;
                    Console.WriteLine("[MyCustomSection] RegisterSection expects parameter: " + (paramType?.FullName ?? "<null>"));

                    if (paramType != null && paramType.FullName == "Newtonsoft.Json.Linq.JObject")
                    {
                        try
                        {
                            var jPayload = JObject.FromObject(payloadDict);
                            Console.WriteLine("[MyCustomSection] Registering payload: resultsAssembly=" + assemblyName + ", resultsClass=" + resultsClass);
                            registerMethod.Invoke(null, new object[] { jPayload });
                            Console.WriteLine("[MyCustomSection] Section registration invoked with JObject payload.");
                        }
                        catch (TargetInvocationException tie)
                        {
                            Console.WriteLine("[MyCustomSection] Invocation error with JObject payload: " + (tie.InnerException?.Message ?? tie.Message));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[MyCustomSection] ERROR invoking RegisterSection with JObject payload: " + ex);
                        }
                    }
                    else
                    {
                        Console.WriteLine("[MyCustomSection] Unexpected parameter type for RegisterSection: " + (paramType?.FullName ?? "<null>") + ". Skipping invoke.");
                    }
                }
                else
                {
                    Console.WriteLine("[MyCustomSection] ERROR: RegisterSection method not found. Aborting registration.");
                }

                // DIAGNOSTIC: enumerate the runtime type and any GetResults candidates in our assembly
                try
                {
                    var runtimeAssembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == assemblyName);

                    Console.WriteLine("[MyCustomSection] Diagnostic: runtimeAssembly found: " + (runtimeAssembly?.FullName ?? "<null>"));

                    if (runtimeAssembly != null)
                    {
                        var t = runtimeAssembly.GetType(resultsClass);
                        Console.WriteLine("[MyCustomSection] Diagnostic: runtimeType: " + (t?.FullName ?? "<null>"));

                        if (t != null)
                        {
                            var methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)
                                           .Where(m => m.Name.IndexOf("GetResults", StringComparison.OrdinalIgnoreCase) >= 0)
                                           .ToArray();

                            Console.WriteLine("[MyCustomSection] Diagnostic: Found GetResults methods count: " + methods.Length);
                            foreach (var m in methods)
                            {
                                var paramList = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name));
                                Console.WriteLine($"[MyCustomSection] Diagnostic: Method: {m.Name} Return: {m.ReturnType.FullName} Params: {paramList} IsStatic: {m.IsStatic}");
                            }

                            // Attempt to invoke any found static candidates with a best-effort argument list
                            var dummyJ = JObject.FromObject(new { test = "ping" });
                            foreach (var m in methods)
                            {
                                try
                                {
                                    var parameters = m.GetParameters();
                                    var args = parameters.Select(p =>
                                    {
                                        if (p.ParameterType.FullName == "Newtonsoft.Json.Linq.JObject") return (object)dummyJ;
                                        if (!p.ParameterType.IsValueType) return null;
                                        return Activator.CreateInstance(p.ParameterType);
                                    }).ToArray();

                                    Console.WriteLine("[MyCustomSection] Diagnostic: Invoking method: " + m.ToString());
                                    var result = m.IsStatic ? m.Invoke(null, args) : m.Invoke(Activator.CreateInstance(t), args);
                                    Console.WriteLine("[MyCustomSection] Diagnostic: Invoke result type: " + (result?.GetType().FullName ?? "<null>"));
                                }
                                catch (TargetInvocationException tie)
                                {
                                    Console.WriteLine("[MyCustomSection] Diagnostic: TargetInvocationException invoking method: " + (tie.InnerException?.ToString() ?? tie.ToString()));
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("[MyCustomSection] Diagnostic: Exception invoking method: " + ex);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[MyCustomSection] Diagnostic ERROR enumerating/invoking methods: " + ex);
                }
            }
            catch (Exception exOuter)
            {
                Console.WriteLine("[MyCustomSection] Unexpected error during registration: " + exOuter);
            }
        }
    }
}
