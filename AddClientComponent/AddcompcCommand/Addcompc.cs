using HarmonyLib;

public static class SubverterPatch
{
    public static string Name = "Based.Addcompc.Command";
    public static string Description = "Adds 'based.addcompc' command. Adds components client-side only (no netsync)";
    public static Harmony Harm = new("org.ValidHunter.Example.Subverter.Addcompc.Command");
}