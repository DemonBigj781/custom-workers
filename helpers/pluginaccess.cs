using System.Reflection;

namespace CustomWorkers;

internal static class PluginAccess
{
    internal static MethodInfo CustomerModelIndexGetter => AccessToolsShim.CustomerModelIndexGetter;

    private static class AccessToolsShim
    {
        internal static readonly MethodInfo CustomerModelIndexGetter = HarmonyLib.AccessTools.Method(typeof(Customer), "GetCustomerModelIndex");
    }
}
