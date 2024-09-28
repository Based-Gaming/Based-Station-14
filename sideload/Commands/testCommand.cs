#if DEBUG
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Commands.Generic;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Robust.Client.Player;
using Content.Shared.Overlays;
using System.Reflection;
using Robust.Shared.Map;
using Content.Client.Station;
using Content.Shared.Stacks;
using Content.Shared.Station;
using Robust.Shared.Map.Components;

[AnyCommand]
public sealed class TestCommand : IConsoleCommand
{
    public string Command => "based.test";
    public string Description => "test";
    public string Help => "HELP!";

    [Dependency] IEntityManager _entityManager = default!;
    //[Dependency] private readonly IPlayerManager _playerManager = default!;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        // Testing
    }
}
#endif