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
                // Defensive assembly/type discovery: swallow any failures
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var targetType = assemblies
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); }
                        catch { return Array.Empty<Type>(); }
                    })
                    .FirstOrDefault(t => t.FullName != null && t.FullName.Contains("HomeScreenSections") && t.GetMethods().Any(m => m.Name == "RegisterSection"));

                if (targetType == null)
                {
                    ServerEntry.TryWriteDebug("HomeScreenRegistrationInvoker: target type not found");
                    return;
                }

                // Prefer static RegisterSection method; if instance required, create one safely
                var method = targetType.GetMethod("RegisterSection", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                if (method == null)
                {
                    ServerEntry.TryWriteDebug("HomeScreenRegistrationInvoker: RegisterSection method not found");
                    return;
                }

                object? instance = null;
                if (!method.IsStatic)
                {
                    try { instance = Activator.CreateInstance(targetType); }
                    catch (Exception ex)
                    {
                        ServerEntry.TryWriteDebug($"HomeScreenRegistrationInvoker: failed to create instance of {targetType.FullName}: {ex.GetType().FullName}: {ex.Message}");
                        return;
                    }
                }

                try
                {
                    method.Invoke(instance, new object[] { payload });
                    ServerEntry.TryWriteDebug("HomeScreenRegistrationInvoker: invoked RegisterSection successfully");
                }
                catch (Exception ex)
                {
                    ServerEntry.TryWriteDebug($"HomeScreenRegistrationInvoker: invoke exception: {ex.GetType().FullName}: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                ServerEntry.TryWriteDebug($"HomeScreenRegistrationInvoker exception: {ex.GetType().FullName}: {ex.Message}");
            }
        }
    }
}
