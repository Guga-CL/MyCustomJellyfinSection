using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Reflection;

namespace My.Custom.Section.Plugin
{
    internal static class HomeScreenRegistrationInvoker
    {
        internal static void InvokeRegisterSection(JObject payload)
        {
            try
            {
                // Look for the HomeScreenSections plugin type that exposes the registration method.
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var type = assemblies
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); }
                        catch { return Array.Empty<Type>(); }
                    })
                    .FirstOrDefault(t => t.FullName != null && t.FullName.Contains("Jellyfin.Plugin.HomeScreenSections.PluginInterface"));

                if (type == null)
                {
                    Log("HomeScreenRegistrationInvoker: target type not found");
                    return;
                }

                var method = type.GetMethod("RegisterSection", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (method == null)
                {
                    Log("HomeScreenRegistrationInvoker: RegisterSection method not found");
                    return;
                }

                // If the method is static, invoke with null; otherwise create an instance.
                object? instance = null;
                if (!method.IsStatic)
                {
                    try { instance = Activator.CreateInstance(type); }
                    catch { Log("HomeScreenRegistrationInvoker: failed to create instance of target type"); }
                }

                method.Invoke(instance, new object[] { payload });
                Log("HomeScreenRegistrationInvoker: invoked RegisterSection successfully");
            }
            catch (Exception ex)
            {
                Log($"HomeScreenRegistrationInvoker exception: {ex.GetType().FullName}: {ex.Message}");
            }
        }

        private static void Log(string s)
        {
            try
            {
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "jellyfin",
                    "plugins",
                    "MyCustomSectionPlugin_1.0.0.0",
                    "jellyfin_plugin_debug.txt");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath) ?? ".");
                System.IO.File.AppendAllText(logPath, $"{DateTime.UtcNow:O} {s}{Environment.NewLine}");
            }
            catch { }
        }
    }
}
