using HarmonyLib;

public static class SubverterPatch
{
    public static string Name = "Based.Commands";
    public static string Description = "Adds 'based.*' commands";
    public static Harmony Harm = new("org.Based.Commands");
}