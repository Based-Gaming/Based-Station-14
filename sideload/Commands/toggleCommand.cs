using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Commands.Generic;

namespace BasedCommands.ShowJobsCommand;

[AnyCommand]
public sealed class ToggleCommand : IConsoleCommand
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