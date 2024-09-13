using HarmonyLib;

public static class SubverterPatch
{
    public static string Name = "Based.Fullbright.Command";
    public static string Description = "Adds 'based.fullbright' command";
    public static Harmony Harm = new("org.ValidHunter.Example.Subverter.Fullbright.Command");
}