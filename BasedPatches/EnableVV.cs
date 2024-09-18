using System.Reflection;
using HarmonyLib;

// Added benefit of enabling MOST client commands
[HarmonyPatch]
static class EnableVVPatch
{
    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(AccessTools.TypeByName("Robust.Client.Console.ClientConsoleHost"), "CanExecute");
        yield return AccessTools.Method(AccessTools.TypeByName("Content.Client.Administration.Managers.ClientAdminManager"), "CanCommand");
    }

    [HarmonyPostfix]
    private static void Postfix(ref bool __result)
    {
        __result = true;
    }
}