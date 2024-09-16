using HarmonyLib;
using System.Reflection;

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
public static class SubverterPatch
{
    public static string Name = "Based.Commands";
    public static string Description = "Adds 'based.*' commands";
    public static Harmony Harm = new("org.Based.Commands");
    public static void Entry()
    {
        Sedition.Hide();
    }
}