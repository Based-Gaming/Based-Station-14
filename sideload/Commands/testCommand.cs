#if DEBUG
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Commands.Generic;
using Robust.Shared.IoC;
using Robust.Shared.GameObjects;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Client.Player;
using Content.Shared.CombatMode;
using Content.Shared.Weapons.Ranged.Components;
using Content.Client.Weapons.Ranged.Systems;


[AnyCommand]
public sealed class TestCommand : IConsoleCommand
{
    public string Command => "based.test";
    public string Description => "test";
    public string Help => "HELP!";

    [Dependency] IEntityManager _entityManager = default!;
    [Dependency] IPlayerManager _playerMan = default!;
    //[Dependency] private readonly IPlayerManager _playerManager = default!;

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {


        // SERVER CRASH WHEN HOLDING A GUN
        shell.WriteLine("Test BEGIN");
        var player = _playerMan.LocalEntity.Value;
        var _gun = _entityManager.System<GunSystem>();
        if (!_gun.TryGetGun(player, out var weaponUid, out GunComponent? gun))
            return;

            if (!_entityManager.TryGetComponent<TransformComponent>(player, out TransformComponent? xform))
            return;

        _entityManager.RaisePredictiveEvent(new RequestShootEvent
        {
            Target = _entityManager.GetNetEntity(player),
            Coordinates = _entityManager.GetNetCoordinates(xform.Coordinates),
            Gun = _entityManager.GetNetEntity(weaponUid),
        });
        shell.WriteLine("Test END");
    }
}
#endif