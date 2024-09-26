using System.Reflection;
using HarmonyLib;


[HarmonyPatch]
public static class OverlaysPatch
{
    private static MethodInfo GetOverlayDraw(Type type)
    {
        return AccessTools.Method(type, "Draw");
    }

    [HarmonyTargetMethods]
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return GetOverlayDraw(typeof(Content.Client.Drunk.DrunkOverlay));
        yield return GetOverlayDraw(typeof(Content.Client.Drugs.RainbowOverlay));
        yield return GetOverlayDraw(typeof(Content.Client.Eye.Blinding.BlurryVisionOverlay));
        yield return GetOverlayDraw(typeof(Content.Client.Eye.Blinding.BlindOverlay));
    }

    [HarmonyPrefix]
    static bool Prefix() => false;
}