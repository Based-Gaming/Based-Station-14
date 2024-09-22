using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Commands.Generic;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Content.Client.SubFloor;


namespace BasedCommands.SubfloorCommand;

[AnyCommand] 
public sealed class SubfloorCommand : IConsoleCommand
{
    public string Command => "based.subfloor";
    public string Description => "Toggles subfloor mode";
    public string Help => "HELP!";
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _entitySystemManager.GetEntitySystem<SubFloorHideSystem>().ShowAll ^= true;
    }
}