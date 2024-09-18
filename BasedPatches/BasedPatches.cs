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
    public static string Name = "BasedPatches";
    public static string Description =
        "Adds all based patches";
    public static bool ignoreFields = true;

    public static void Entry()
    {
        Sedition.Hide();
    }
}