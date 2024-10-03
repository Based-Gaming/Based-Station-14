using BasedCommands.ShowJobsCommand;
using Content.Client.UserInterface.Systems.Based;
using HarmonyLib;
using Robust.Shared.IoC;
using System.Reflection;

public static class MarseyLogger
{
    public enum LogType
    {
        INFO,
        WARN,
        FATL,
        DEBG
    }
    public delegate void Forward(AssemblyName asm, string message);
    public static Forward? logDelegate;
    public static void Log(LogType type, string message)
    {
        logDelegate?.Invoke(Assembly.GetExecutingAssembly().GetName(), $"[{type.ToString()}] {message}");
    }
}

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
    public static string Name = "BA$ED";
    public static string Description = "Adds based framework";
    public static Harmony Harm = new("org.Based");
    public static void Entry()
    {
        Sedition.Hide();
    }
}

#if false
public static class MarseyEntry
{
    // WARNING: You might want to wait until Content.Client is in the appdomain before executing PatchAll, if you're patching methods located in the content pack, as MarseyEntry is executed before the content packs are loaded!
    public static void Entry()
    {
        IoCManager.BuildGraph();
        IoCManager.InjectDependencies(this);
    }
}
#endif