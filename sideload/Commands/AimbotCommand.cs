using Content.Client.Based;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Commands.Generic;

namespace BasedCommands.ShowJobsCommand;

[AnyCommand] 
public sealed class AimbotCommand : IConsoleCommand
{
    public string Command => "based.aimbot";
    public string Description => "Toggles aimbot mode";
    public string Help => "HELP!";
    public bool enabled = false;
    public AimMode mode = AimMode.NEAR_PLAYER;

    // based.aimbot [MODE] [UPDATE_MODE_ONLY]
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {   
        if (args.Length > 1)
        {
            string _modeStr = args[0];
            if (_modeStr.Equals("NEAR_PLAYER"))
            {
                this.mode = AimMode.NEAR_PLAYER;
            } else if (_modeStr.Equals("NEAR_MOUSE"))
            {
                this.mode = AimMode.NEAR_MOUSE;
            }
            if (args.Length == 2)
            {
                // update mode ONLY
                return;
            }
        }

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
