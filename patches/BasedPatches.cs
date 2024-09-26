using System.Reflection;

public static class MarseyLogger
{
    // Info enums are identical to those in the loader however they cant be easily casted between the two
    public enum LogType
    {
        INFO,
        WARN,
        FATL,
        DEBG
    }

    // Delegate gets casted to Marsey::Utility::Log(AssemblyName, string) at runtime by the loader
    public delegate void Forward(AssemblyName asm, string message);

    public static Forward? logDelegate;

    /// <see cref="BasePatch.Finalizer"/>
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

public static class MarseyPatch
{
    public static string Name = "Based.Patch";
    public static string Description =
        "Adds all based patches";
    public static bool ignoreFields = true;

    public static void Entry()
    {
        Sedition.Hide();
    }
}