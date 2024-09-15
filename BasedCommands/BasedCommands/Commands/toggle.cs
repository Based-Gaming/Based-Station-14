using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Commands.Generic;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Robust.Client.Player;
using Content.Shared.Overlays;


namespace BasedCommands.ShowJobsCommand;

[AnyCommand]
public class ToggleCommand : IConsoleCommand
{
    public string Command => "based.toggle";
    public string Description => "Toggles based mode. (all based enhancements)";
    public string Help => "HELP!";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        shell.ExecuteCommand("based.fullbright");
        shell.ExecuteCommand("based.showjobs");
        // TODO - more enhancements for based mode

    }
}