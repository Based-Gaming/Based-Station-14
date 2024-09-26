using System.Reflection;
using HarmonyLib;

[HarmonyPatch]
static class LocalAdminPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        // Enables all commands and ViewVariables
        yield return AccessTools.Method(typeof(Content.Client.Administration.Managers.ClientAdminManager), "CanCommand");
        yield return AccessTools.Method(typeof(Content.Client.Administration.Managers.ClientAdminManager), "CanScript");

#if DEBUG
        // The admin menu is really only useful for the `Objects` Tab.
        yield return AccessTools.Method(AccessTools.TypeByName("Content.Client.Administration.Managers.ClientAdminManager"), "CanAdminMenu");
#endif
        // This enables sandbox menu, which we dont want
        //yield return AccessTools.Method(AccessTools.TypeByName("Content.Client.Administration.Managers.ClientAdminManager"), "IsActive");
    }

    [HarmonyPostfix]
    private static void Postfix(ref bool __result)
    {
        __result = true;
    }
}

#if DEBUG
[HarmonyPatch]
static class LocalAdminFlagsPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Content.Client.Administration.Managers.ClientAdminManager"), "HasFlag");

    }

    [HarmonyPostfix]
    private static void Postfix(ref bool __result, AdminFlags flag)
    {
        // Maintain the Ahelp feature
        if (flag == AdminFlags.Adminhelp)
        {
            __result = false;
        }
        else
        {
            __result = true;
        }
    }
}
#endif