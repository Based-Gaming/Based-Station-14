using Content.Client.UserInterface.Systems.Based;
using HarmonyLib;
using Robust.Shared.IoC;
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
    public static string Name = "Based Sideload";
    public static string Description = "Adds 'based.*' commands and more";
    public static Harmony Harm = new("org.Based.Sideload");
    public static void Entry()
    {
        Sedition.Hide();
    }
}

public static class MarseyEntry
{
    // WARNING: You might want to wait until Content.Client is in the appdomain before executing PatchAll, if you're patching methods located in the content pack, as MarseyEntry is executed before the content packs are loaded!
    public static void Entry()
    {
        IoCManager.Register<BasedUIController>();
        IoCManager.BuildGraph();
        Assembly subvmarsey = Assembly.GetExecutingAssembly();
        SubverterPatch.Harm.PatchAll(subvmarsey);
    }
}