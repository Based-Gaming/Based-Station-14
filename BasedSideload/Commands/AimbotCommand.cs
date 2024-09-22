using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Toolshed.Commands.Generic;

namespace BasedCommands.ShowJobsCommand;

[AnyCommand] 
public sealed class AimbotCommand : IConsoleCommand
{
    public string Command => "based.aimbot";
    public string Description => "Toggles aimbot mode";
    public string Help => "HELP!";
    public bool enabled = false;
    //private bool injected = false;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        /*
        if (!this.injected)
        {
            IoCManager.InjectDependencies(this);
            this.injected = true;
        }*/
        
        if (this.enabled)
        {
            shell.WriteLine("Aimbot [Disabled]");
        }
        else
        {
            shell.WriteLine("Aimbot [Enabled]");
        }
        this.enabled = !this.enabled;
    }
}
