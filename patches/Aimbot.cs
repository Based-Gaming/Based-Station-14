using System.Reflection;
using HarmonyLib;

using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Shared.CombatMode;
using Content.Shared.Weapons.Melee;
using Robust.Client.Player;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Content.Shared.Weapons.Melee.Events;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Console;

[HarmonyPatch]
public static class AimbotPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        // This gets run a the start of every tick in the main GameLoop
        return AccessTools.Method(AccessTools.TypeByName("Robust.Shared.Timing.GameTiming"), "StartFrame");
    }

    [HarmonyPostfix]
    private static void TypePostfix()
    {
        IConsoleHost _consoleHost = IoCManager.Resolve<IConsoleHost>();
        IConsoleCommand? cmd = null;
        bool enabled = false;
        foreach ( var kvp in _consoleHost.AvailableCommands)
        {
            if (kvp.Key.Equals("based.aimbot"))
            {
                cmd = kvp.Value;
                break;
            }
        }
        if (cmd == null)
        {
            //MarseyLogger.Log(MarseyLogger.LogType.INFO, "NO AIMBOT COMMAND!!!");
            return;
        }
        enabled = Traverse.Create(cmd).Field("enabled").GetValue<bool>();
        if (!enabled) {
            return;
        }

        // Early bail if no player
        IPlayerManager _playerMan = IoCManager.Resolve<IPlayerManager>();
        EntityUid? p = _playerMan.LocalEntity;
        if (p == null) return;
        EntityUid player = (EntityUid)p;

        IEntityManager _entityManager = IoCManager.Resolve<IEntityManager>();
        IGameTiming _timing = IoCManager.Resolve<IGameTiming>();

        // Check is in combat mode
        if (!_entityManager.TryGetComponent<CombatModeComponent>(player, out CombatModeComponent? combat))
        {
            MarseyLogger.Log(MarseyLogger.LogType.INFO, "No combatmode component on player");
            return;
        }

        if (!combat.IsInCombatMode)
        {
            return;
        }

        SharedMeleeWeaponSystem _melee = _entityManager.System<SharedMeleeWeaponSystem>();
        EntityLookupSystem _entityLookup = _entityManager.System<EntityLookupSystem>();

        if (!_melee.TryGetWeapon(player, out var weaponUid, out var weapon))
        {
            MarseyLogger.Log(MarseyLogger.LogType.INFO, "No weapon");
            return;
        }

        if (!_entityManager.TryGetComponent<TransformComponent>(player, out TransformComponent? userXform))
        {
            MarseyLogger.Log(MarseyLogger.LogType.INFO, "No transform component on player");
            return;
        }

        // Get all targets in range
        var targets = new HashSet<Entity<MobStateComponent>>();
        _entityLookup.GetEntitiesInRange(userXform.Coordinates, weapon.Range, targets);

        // Get closest target
        EntityUid? target = null;
        float lastDistance = 0f;
        TransformComponent? targetxform = null;
        foreach (var targ in targets)
        {
            if (targ.Owner.Id == player.Id)
            {
                continue; // skip ourself...
            }
            if (!_entityManager.TryGetComponent<TransformComponent>(targ.Owner, out TransformComponent? xform))
            {
                //MarseyLogger.Log(MarseyLogger.LogType.INFO, "No transform component on target");
                continue;
            }
            if (!userXform.Coordinates.TryDistance(_entityManager, xform.Coordinates, out var distance))
            {
                //MarseyLogger.Log(MarseyLogger.LogType.INFO, "Failed to get distance from target");
                continue;
            }
            if (target == null)
            {
                target = targ.Owner;
                lastDistance = distance;
                targetxform = xform;
            }
            else if (distance < lastDistance)
            {
                target = targ.Owner;
                lastDistance = distance;
                targetxform = xform;
            }
        }

        if (target == null)
        {
            MarseyLogger.Log(MarseyLogger.LogType.INFO, "No targets in range");
            return;
        }

        if (weapon.NextAttack > _timing.CurTime)
        {
            //MarseyLogger.Log(MarseyLogger.LogType.INFO, "Weapon on cooldown");
            return;
        }

        IInputManager _input = IoCManager.Resolve<IInputManager>();
        InputSystem _inputSystem = _entityManager.System<InputSystem>();
        var useDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
        if (useDown == BoundKeyState.Down)
        {
            _entityManager.RaisePredictiveEvent(new LightAttackEvent(_entityManager.GetNetEntity(target),
                _entityManager.GetNetEntity(weaponUid),
                _entityManager.GetNetCoordinates(targetxform.Coordinates)));
        }


    }
}