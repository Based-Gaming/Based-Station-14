using System.Reflection;
using HarmonyLib;

public static class Sedition
{
    private static bool tripped = false;

    public delegate void ForwardHide(Assembly asm);
    public static ForwardHide? hideDelegate;

    public static void Hide()
    {
        if (tripped) return;

        tripped = true;
        hideDelegate?.Invoke(Assembly.GetExecutingAssembly());
    }
}
public static class MarseyPatch
{
    public static string Name = "EnableVV";

    public static string Description =
        "Disable permission checking on all commands and enables VV menu verb";

    public static bool ignoreFields = true;
    public static void Entry()
    {
        Sedition.Hide();
    }
}


[HarmonyPatch]
/// <summary>
/// Sets the ability to run a command to always true via PostFix: https://github.com/space-wizards/RobustToolbox/blob/master/Robust.Client/Console/ClientConsoleHost.cs#L198
/// </summary>
static class EnableVV
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Robust.Client.Console.ClientConsoleHost"), "CanExecute");
    }

    [HarmonyPostfix]
    private static void Postfix(ref bool __result)
    {
        __result = true;
    }
}

[HarmonyPatch]
static class EnableVVPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Content.Client.Administration.Managers.ClientAdminManager"), "CanViewVar");
    }

    [HarmonyPostfix]
    private static void Postfix(ref bool __result)
    {
        __result = true;
    }
}