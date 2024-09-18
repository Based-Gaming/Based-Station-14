using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

[HarmonyPatch]
public class VielStealthPatch
{

    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
    {
        // just a very early function to hook
        return AccessTools.Method(AccessTools.TypeByName("Content.Client.Replay.ContentReplayPlaybackManager"), "Initialize");
    }

    [HarmonyPostfix]
    private static void TypePost()
    {
        Sedition.Hide();
    }
}

public static class Facade
{
    public delegate IEnumerable<Type> Forward();
    public static Forward? GetTypes;
}

//https://github.com/ValidHunters/Marseyloader/pull/48
[HarmonyPatch]
static class VielPatch1
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(AccessTools.TypeByName("Robust.Shared.Reflection.ReflectionManager"), "FindAllTypes");
        yield return AccessTools.Method(AccessTools.TypeByName("Robust.Shared.Reflection.ReflectionManager"), "GetAllChildren",
            new[] { typeof(Type), typeof(bool) });
        yield return AccessTools.Method(AccessTools.TypeByName("Robust.Shared.Reflection.ReflectionManager"), "FindTypesWithAttribute",
            new[] { typeof(Type) });

    }
    internal static bool FromContent()
    {
        StackTrace stackTrace = new();

        foreach (StackFrame frame in stackTrace.GetFrames())
        {
            MethodBase? method = frame.GetMethod();
            if (method == null || method.DeclaringType == null) continue;
            string? namespaceName = method.DeclaringType.Namespace;
            if (!string.IsNullOrEmpty(namespaceName) && namespaceName.StartsWith("Content."))
            {
                return true;
            }
        }

        return false;
    }

    [HarmonyPostfix]
    private static void TypePost(ref IEnumerable<Type> __result)
    {
        if (!FromContent()) return;
        IEnumerable<Type> ?hiddenTypes = Facade.GetTypes?.Invoke();
        if(hiddenTypes != null)
        {
            __result = __result.Except(hiddenTypes).AsEnumerable();
        }
    }
}

[HarmonyPatch]
static class VielPatch2
{
    private static List<string?> HiddenAssemblies = [
        "BasedCommands", "BasedPatches"];

    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.PropertyGetter(AccessTools.TypeByName("Robust.Shared.Reflection.ReflectionManager"), "Assemblies");
    }

    [HarmonyPrefix]
    private static bool TypePrefix(ref IReadOnlyList<Assembly> __result, object __instance)
    {

        List<Assembly>? originalAssemblies = Traverse.Create(__instance).Field("assemblies").GetValue<List<Assembly>>();
        if (originalAssemblies == null)
        {
            __result = new List<Assembly>().AsReadOnly();
            return false;
        }

        // Filter out assemblies whose names are in HiddenAssemblies
        List<Assembly> veiledAssemblies = originalAssemblies
            .Where(asm =>
            {
                string? value = asm.GetName().Name;
                return value != null && !HiddenAssemblies.Contains(value);
            })
            .ToList();

        // Return the filtered list as a read-only list
        __result = veiledAssemblies.AsReadOnly();

        return false;
    }
}