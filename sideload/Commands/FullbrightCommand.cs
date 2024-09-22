using Content.Shared.Administration;
using Robust.Client.Graphics;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Commands.Generic;
using Robust.Shared.IoC;


namespace BasedCommands.FullbrightCommand;

[AnyCommand] 
public sealed class FullbrightCommand : IConsoleCommand
{
    public string Command => "based.fullbright";
    public string Description => "Toggles fullbright mode";
    public string Help => "HELP!";
    [Dependency] private readonly ILightManager _light = default!;
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _light.Enabled = !_light.Enabled;
    }
}