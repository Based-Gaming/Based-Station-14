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
    public static string Name = "Overlays patch";

    public static string Description =
        "Disables Draw on multiple overlays";

    public static bool ignoreFields = true;

    public static void Entry()
    {
        Sedition.Hide();
    }
}


[HarmonyPatch]
/// <summary>
///  Disable overlays by disabling Draw. For example: https://github.com/space-wizards/space-station-14/blob/master/Content.Client/Drunk/DrunkOverlay.cs
/// </summary>
public static class OverlaysPatch
{
    private static MethodInfo GetOverlayDraw(string type)
    {
        return AccessTools.Method(AccessTools.TypeByName(type), "Draw");
    }

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return GetOverlayDraw("Content.Client.Drunk.DrunkOverlay");
        yield return GetOverlayDraw("Content.Client.Drugs.RainbowOverlay");
        yield return GetOverlayDraw("Content.Client.Eye.Blinding.BlurryVisionOverlay");
        yield return GetOverlayDraw("Content.Client.Eye.Blinding.BlindOverlay");
    }

    [HarmonyPrefix]
    static bool Prefix() => false;
}