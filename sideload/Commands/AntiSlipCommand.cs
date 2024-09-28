#if DEBUG
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Commands.Generic;

namespace BasedCommands.ShowJobsCommand;

[AnyCommand] 
public sealed class AntiSlipCommand : IConsoleCommand
{
    public string Command => "based.antislip";
    public string Description => "Toggles antislip mode";
    public string Help => "HELP!";
    public bool enabled = false;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {   
        if (this.enabled)
        {
            shell.WriteLine("Antislip [Disabled]");
        }
        else
        {
            shell.WriteLine("Antislip [Enabled]");
        }
        this.enabled = !this.enabled;
    }
}
#endif