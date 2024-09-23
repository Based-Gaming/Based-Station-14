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
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Client.Interactable;
using Robust.Shared.Map;
using Robust.Client.Graphics;
using Robust.Shared.Physics;
using System.Numerics;
using Content.Client.Gameplay;
using Robust.Client.State;
using Content.Client.Weapons.Melee;
using System.ComponentModel;
using Robust.Shared.Maths;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using static Content.Shared.Interaction.SharedInteractionSystem;
using Content.Shared.Physics;
using Robust.Shared.Physics.Systems;

enum AimMode
{
    NEAR_PLAYER,
    NEAR_MOUSE
}


[HarmonyPatch]
public static class AimbotPatch
{
    private static HashSet<EntityUid> ArcRayCast(SharedPhysicsSystem _physics, Vector2 position, Angle angle, Angle arcWidth, float range, MapId mapId, EntityUid ignore)
    {
        // TODO: This is pretty sucky.
        var widthRad = arcWidth;
        var increments = 1 + 35 * (int)Math.Ceiling(widthRad / (2 * Math.PI));
        var increment = widthRad / increments;
        var baseAngle = angle - widthRad / 2;
        int AttackMask = (int)(CollisionGroup.MobMask | CollisionGroup.Opaque);

        var resSet = new HashSet<EntityUid>();

        for (var i = 0; i < increments; i++)
        {
            var castAngle = new Angle(baseAngle + increment * i);
            var res = _physics.IntersectRay(mapId,
                new CollisionRay(position, castAngle.ToWorldVec(),
                    AttackMask), range, ignore, false).ToList();

            if (res.Count != 0)
            {
                resSet.Add(res[0].HitEntity);
            }
        }

        return resSet;
    }

    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        // This gets run a the start of every tick in the main GameLoop
        return AccessTools.Method(AccessTools.TypeByName("Robust.Shared.Timing.GameTiming"), "StartFrame");
    }

    [HarmonyPostfix]
    private static void TypePostfix()
    {
        /* This code may not be pretty, but EVERYTHING here is processes in the active game loop.
         * So, don't do any extra compution. Bail as early as possible, and resolve only when needed
         * */
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
        AimMode mode = Traverse.Create(cmd).Field("mode").GetValue<AimMode>();
        //AimMode mode = AimMode.NEAR_PLAYER;

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
            //MarseyLogger.Log(MarseyLogger.LogType.INFO, "No combatmode component on player");
            return;
        }

        if (!combat.IsInCombatMode)
        {
            return;
        }

        /* Get Active Weapon */
        MeleeWeaponSystem _melee = _entityManager.System<MeleeWeaponSystem>();
        //SharedMeleeWeaponSystem _melee = _entityManager.System<SharedMeleeWeaponSystem>();
        GunSystem _gun = _entityManager.System<GunSystem>();
        EntityLookupSystem _entityLookup = _entityManager.System<EntityLookupSystem>();

        GunComponent? gun = null;
        MeleeWeaponComponent? meleeWeapon = null;
        EntityUid weaponUid;
        if (!_gun.TryGetGun(player, out weaponUid, out gun))
        {
            // No gun - maybe melee?
            if (!_melee.TryGetWeapon(player, out weaponUid, out meleeWeapon))
            {
                //MarseyLogger.Log(MarseyLogger.LogType.INFO, "No weapon");
                return;
            }
            if (meleeWeapon.NextAttack > _timing.CurTime)
            {
                //MarseyLogger.Log(MarseyLogger.LogType.INFO, "Weapon on cooldown");
                return;
            }
        }

        if (!_entityManager.TryGetComponent<TransformComponent>(player, out TransformComponent? userXform))
        {
            //MarseyLogger.Log(MarseyLogger.LogType.INFO, "No transform component on player");
            return;
        }

        /* Get all targets in range */
        var targets = new HashSet<Entity<MobStateComponent>>();
        if (meleeWeapon != null)
        {
            _entityLookup.GetEntitiesInRange(userXform.Coordinates, meleeWeapon.Range, targets);
        }
        else
        {
            _entityLookup.GetEntitiesInRange(userXform.Coordinates, 10f, targets);
        }

        // Get closest target
        EntityUid? target = null;
        float lastDistance = 0f;
        TransformComponent? targetxform = null;

        SharedTransformSystem _ts = _entityManager.System<SharedTransformSystem>();
        InteractionSystem _is = _entityManager.System<InteractionSystem>();
        IInputManager _input = IoCManager.Resolve<IInputManager>();
        IEyeManager _eye = IoCManager.Resolve<IEyeManager>();
        IMapManager _mapManager = IoCManager.Resolve<IMapManager>();
        foreach (var targ in targets)
        {
            float distance = 0f;
            if (targ.Owner.Id == player.Id)
            {
                continue; // skip ourself...
            }

            if (!_entityManager.TryGetComponent<TransformComponent>(targ.Owner, out TransformComponent? xform))
            {
                //MarseyLogger.Log(MarseyLogger.LogType.INFO, "No transform component on target");
                continue;
            }
            if (userXform.MapID != xform.MapID)
            {
                return;
            }

            if (mode == AimMode.NEAR_PLAYER)
            {
                if (!userXform.Coordinates.TryDistance(_entityManager, xform.Coordinates, out distance))
                {
                    //MarseyLogger.Log(MarseyLogger.LogType.INFO, "Failed to get distance from target");
                    continue;
                }
            } else if (mode == AimMode.NEAR_MOUSE)
            {
                MapCoordinates mouseMapCoord = _eye.PixelToMap(_input.MouseScreenPosition);
                if (mouseMapCoord.MapId == MapId.Nullspace)
                {
                    return; // mouse in nullspace
                }
       
                EntityCoordinates mouseCoords = _ts.ToCoordinates(player, mouseMapCoord);
                //EntityCoordinates mouseCoords = _ts.ToCoordinates(targ.Owner, mouseMapCoord); //relative to target!
                if (!mouseCoords.TryDistance(_entityManager, xform.Coordinates, out distance))
                {
                    //MarseyLogger.Log(MarseyLogger.LogType.INFO, "Failed to get distance from mouse");
                    continue;
                }
            }

            if (target == null)
            {
                // only target if they are in line-of-sight for guns
                if (gun != null && !_is.InRangeUnobstructed(player, targ.Owner, distance + 0.1f))
                {                    
                    continue;
                }
                target = targ.Owner;
                lastDistance = distance;
                targetxform = xform;

            }
            else if (distance < lastDistance)
            {
                // only target if they are in line-of-sight for guns
                if (gun != null && !_is.InRangeUnobstructed(player, targ.Owner, distance + 0.1f))
                {
                    continue;
                }
                target = targ.Owner;
                lastDistance = distance;
                targetxform = xform;
            }
        } // foreach tar in targets

        if (target == null)
        {
            //MarseyLogger.Log(MarseyLogger.LogType.INFO, "No targets in range");
            return;
        }

        InputSystem _inputSystem = _entityManager.System<InputSystem>();
        var useDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
        var altDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary);
        if (useDown == BoundKeyState.Down)
        {
            if (meleeWeapon != null)
            {
                // Swing batter batter, swing!
                _entityManager.RaisePredictiveEvent(new LightAttackEvent(
                    _entityManager.GetNetEntity(target),
                    _entityManager.GetNetEntity(weaponUid),
                    _entityManager.GetNetCoordinates(targetxform.Coordinates))
                 );
                return;
            }
            else if (gun != null)
            {
                // Shoot
                // Define target coordinates relative to gunner entity, so that network latency on moving grids doesn't fuck up the target location.
                var coordinates = _ts.ToCoordinates(player, targetxform.MapPosition);
                _entityManager.RaisePredictiveEvent(new RequestShootEvent
                {
                    Target = _entityManager.GetNetEntity(target),
                    Coordinates = _entityManager.GetNetCoordinates(targetxform.Coordinates),
                    Gun = _entityManager.GetNetEntity(weaponUid)
                }
                 );
                return;
                //_gun.AttemptShoot(player, weaponUid, gun, targetxform.Coordinates);
            }
        }
        else if (meleeWeapon != null && altDown == BoundKeyState.Down)
        {
            // If it's an unarmed attack then do a disarm
            if (meleeWeapon.AltDisarm && weaponUid == player)
            {
                _entityManager.RaisePredictiveEvent(new DisarmAttackEvent(
                    _entityManager.GetNetEntity(target),
                    _entityManager.GetNetCoordinates(targetxform.Coordinates)));
            }
            else
            {

                MapCoordinates targetMap = _ts.ToMapCoordinates(targetxform.Coordinates);

                if (targetMap.MapId != userXform.MapID)
                    return;
                SharedPhysicsSystem _physics = _entityManager.System<SharedPhysicsSystem>();
                var userPos = _ts.GetWorldPosition(userXform);
                var direction = targetMap.Position - userPos;
                var distance = MathF.Min(meleeWeapon.Range, direction.Length());

                // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
                // Server will validate it with InRangeUnobstructed.
                var entities = _entityManager.GetNetEntityList(ArcRayCast(_physics, 
                    userPos, direction.ToWorldAngle(), meleeWeapon.Angle, distance, userXform.MapID, player).ToList());
                _entityManager.RaisePredictiveEvent(new HeavyAttackEvent(
                    _entityManager.GetNetEntity(weaponUid),
                    entities.GetRange(0, Math.Min(5, entities.Count)),
                    _entityManager.GetNetCoordinates(targetxform.Coordinates)
                    ));
            }
            return;
        }
    }
}