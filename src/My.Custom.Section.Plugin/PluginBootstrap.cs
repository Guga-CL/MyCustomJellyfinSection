using Newtonsoft.Json.Linq;
using System;

namespace My.Custom.Section.Plugin
{
    internal static class PluginBootstrap
    {
        internal static void TryRegisterSection()
        {
            try
            {
                var payload = SectionRegistrar.BuildPayload();
                HomeScreenRegistrationInvoker.InvokeRegisterSection(payload);
            }
            catch (Exception ex)
            {
                ServerEntry.TryWriteDebug($"PluginBootstrap.TryRegisterSection exception: {ex.GetType().FullName}: {ex.Message}");
            }
        }
    }
}
