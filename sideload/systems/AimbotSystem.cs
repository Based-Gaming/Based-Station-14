using System.Numerics;
using Content.Client.Based;
using Content.Client.Interactable;
using Content.Client.Weapons.Melee;
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared.CombatMode;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using HarmonyLib;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Console;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Robust.Shared.GameObjects;
using System.Reflection;
using System;


[HarmonyPatch]
public static class AimbotPatch
{
    static bool IsAimbotEnabled()
    {
        IConsoleHost _consoleHost = IoCManager.Resolve<IConsoleHost>();
        IConsoleCommand? cmd = null;
        bool enabled = false;
        foreach (var kvp in _consoleHost.AvailableCommands)
        {
            if (kvp.Key.Equals("based.aimbot"))
            {
                cmd = kvp.Value;
                break;
            }
        }
        if (cmd == null)
            return false;

        enabled = Traverse.Create(cmd).Field("enabled").GetValue<bool>();
        return enabled;
    }

    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(Content.Client.Weapons.Ranged.Systems.GunSystem), "Update");
    }

    [HarmonyPrefix]
    static bool Prefix()
    {
        return !IsAimbotEnabled(); //skip if aimbot is enabled
    }
}

public sealed class AimbotSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly MeleeWeaponSystem _melee = default!;
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _ts = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly InteractionSystem _is = default!;

    IConsoleCommand? _cmd = null;

    public override void Initialize()
    {
        base.Initialize();
    }

    private HashSet<EntityUid> ArcRayCast(Vector2 position, Angle angle, Angle arcWidth, float range, MapId mapId, EntityUid ignore)
    {
        var widthRad = arcWidth;
        var increments = 1 + 35 * (int)Math.Ceiling(widthRad / (2 * Math.PI));
        var increment = widthRad / increments;
        var baseAngle = angle - widthRad / 2;
        int AttackMask = (int)(CollisionGroup.MobMask | CollisionGroup.Opaque);

        var resSet = new HashSet<EntityUid>();

        for (var i = 0; i < increments; i++)
        {
            var castAngle = new Angle(baseAngle + increment * i);
            var res = this._physics.IntersectRay(mapId,
                new CollisionRay(position, castAngle.ToWorldVec(),
                    AttackMask), range, ignore, false).ToList();

            if (res.Count != 0)
                resSet.Add(res[0].HitEntity);
        }

        return resSet;
    }

    // False if not enabled, or cannot resolve
    private bool GetAimbotMode(ref AimMode mode)
    {
        if (_cmd == null)
        {
            foreach (var kvp in _consoleHost.AvailableCommands)
            {
                if (kvp.Key.Equals("based.aimbot"))
                {
                    _cmd = kvp.Value;
                    break;
                }
            }
            if( _cmd == null)
                return false;
        }

        bool enabled = Traverse.Create(_cmd).Field("enabled").GetValue<bool>();
        if (!enabled)
            return false;

        mode = Traverse.Create(_cmd).Field("mode").GetValue<AimMode>();
        return true;
    }

    private bool GetMouseDistance(EntityUid target, out float distance)
    {
        distance = 0f;
        MapCoordinates mouseMapCoord = _eye.PixelToMap(_inputManager.MouseScreenPosition);
        if (mouseMapCoord.MapId == MapId.Nullspace)
            return false; // mouse in nullspace

        distance = Math.Abs(Vector2.Distance(mouseMapCoord.Position, _ts.GetWorldPosition(target)));
        //_inputManager.MouseScreenPosition.
        //distance = (mouseMapCoord.Position - _ts.ToMapCoordinates(targetXform.Coordinates).Position).Length();
        //return true;
        //distance = Angle.ShortestDistance(mouseMapCoord.Position.ToWorldAngle(),  targetXform.WorldPosition.ToWorldAngle());
        return true;
        /*
        EntityCoordinates mouseCoords = _ts.ToCoordinates(entity, mouseMapCoord);
        if (!targetXform.Coordinates.TryDistance(_entityManager, mouseCoords, out distance))
            return false;
        return true;
        */
    }

    public override void FrameUpdate(float frameTime)
    {
        AimMode mode = AimMode.NEAR_PLAYER;
        if (!this.GetAimbotMode(ref mode))
            return;

        // Early bail if no player
        EntityUid? p = _playerMan.LocalEntity;
        if (p == null) return;
        EntityUid player = (EntityUid)p;

        // Check is in combat mode
        if (!EntityManager.TryGetComponent<CombatModeComponent>(player, out CombatModeComponent? combat))
            return;

        if (!combat.IsInCombatMode)
            return;

        /* Get Active Weapon */

        MeleeWeaponComponent? meleeWeapon = null;
        EntityUid weaponUid;
        if (!_gun.TryGetGun(player, out weaponUid, out GunComponent? gun))
        {
            // No gun - maybe melee?
            if (!_melee.TryGetWeapon(player, out weaponUid, out meleeWeapon))
                return;
            if (meleeWeapon.NextAttack > _timing.CurTime)
                return;
        }


        var useDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.Use);
        var altDown = _inputSystem.CmdStates.GetState(EngineKeyFunctions.UseSecondary);

        if (useDown != BoundKeyState.Down && gun != null)
        {
            if (gun.ShotCounter != 0)
                EntityManager.RaisePredictiveEvent(new RequestStopShootEvent { Gun = GetNetEntity(weaponUid) });
        }

        if (!EntityManager.TryGetComponent<TransformComponent>(player, out TransformComponent? userXform))
            return;

        /* Get all targets in range */
        var targets = new HashSet<Entity<MobStateComponent>>();
        if (meleeWeapon != null)
            _entityLookup.GetEntitiesInRange(userXform.Coordinates, meleeWeapon.Range, targets);
        else
            _entityLookup.GetEntitiesInRange(userXform.Coordinates, 10f, targets);

        // Get closest target
        EntityUid? target = null;
        float lastDistance = 0f;
        TransformComponent? targetxform = null;

        foreach (var targ in targets)
        {
            float mouseDistance = 0f;
            if (targ.Owner.Id == player.Id)
                continue; // skip ourself...

            if (!EntityManager.TryGetComponent<TransformComponent>(targ.Owner, out TransformComponent? xform))
                continue;

            if (userXform.MapID != xform.MapID)
                continue;

            if (!userXform.Coordinates.TryDistance(_entityManager, xform.Coordinates, out var playerDistance))
                continue;


            if (mode == AimMode.NEAR_MOUSE)
            {
                if (!GetMouseDistance(targ.Owner, out mouseDistance))
                    continue;
            }

            if (target == null)
            {
                // only target if they are in line-of-sight for guns
                if (gun != null && !_is.InRangeUnobstructed(player, targ.Owner, playerDistance + 0.1f))
                    continue;
                target = targ.Owner;
                lastDistance = playerDistance;
                targetxform = xform;

            }
            else if ((mode == AimMode.NEAR_PLAYER) && (playerDistance < lastDistance))
            {
                // only target if they are in line-of-sight for guns
                if (gun != null && !_is.InRangeUnobstructed(player, targ.Owner, playerDistance + 0.1f))
                    continue;
                target = targ.Owner;
                lastDistance = playerDistance;
                targetxform = xform;
            }
            else if ((mode == AimMode.NEAR_MOUSE) && (mouseDistance < lastDistance))
            {
                // only target if they are in line-of-sight for guns
                if (gun != null && !_is.InRangeUnobstructed(player, targ.Owner, playerDistance + 0.1f))
                    continue;
                target = targ.Owner;
                lastDistance = mouseDistance;
                targetxform = xform;
            }
        } // foreach tar in targets

        if (target == null || targetxform == null)
            return;

        if (useDown == BoundKeyState.Down)
        {
            if (meleeWeapon != null)
            {
                // Swing batter batter, swing!
                _entityManager.RaisePredictiveEvent(new LightAttackEvent
                    (
                        EntityManager.GetNetEntity(target),
                        EntityManager.GetNetEntity(weaponUid),
                        EntityManager.GetNetCoordinates(targetxform.Coordinates)
                    )
                 );
                return;
            }
            else if (gun != null)
            {
                // Shoot
                // Define target coordinates relative to gunner entity, so that network latency on moving grids doesn't fuck up the target location.
                var coordinates = _ts.ToCoordinates(player, targetxform.MapPosition);
                this.shoot(player, gun, weaponUid, EntityManager.GetNetEntity(target).Value, coordinates);
                return;
            }
        }
        else if (meleeWeapon != null && altDown == BoundKeyState.Down)
        {
            // If it's an unarmed attack then do a disarm
            if (meleeWeapon.AltDisarm && weaponUid == player)
            {
                _entityManager.RaisePredictiveEvent(new DisarmAttackEvent
                    (
                        _entityManager.GetNetEntity(target),
                        _entityManager.GetNetCoordinates(targetxform.Coordinates)
                    )
                );
            }
            else
            {

                MapCoordinates targetMap = _ts.ToMapCoordinates(targetxform.Coordinates);

                if (targetMap.MapId != userXform.MapID)
                    return;
                var userPos = _ts.GetWorldPosition(userXform);
                var direction = targetMap.Position - userPos;
                var distance = MathF.Min(meleeWeapon.Range, direction.Length());

                // This should really be improved. GetEntitiesInArc uses pos instead of bounding boxes.
                // Server will validate it with InRangeUnobstructed.
                var entities = _entityManager.GetNetEntityList(this.ArcRayCast(
                    userPos, direction.ToWorldAngle(), meleeWeapon.Angle, distance, userXform.MapID, player).ToList());
                _entityManager.RaisePredictiveEvent(new HeavyAttackEvent(
                    _entityManager.GetNetEntity(weaponUid),
                    entities.GetRange(0, Math.Min(5, entities.Count)),
                    _entityManager.GetNetCoordinates(targetxform.Coordinates)
                    ));
            }
            return;
        } // usekey down
    }

    private void shoot(EntityUid player, GunComponent gun, EntityUid gunUid, NetEntity target, EntityCoordinates coordinates)
    {
        var entity = player;

        if (gun.NextFire > _timing.CurTime)
            return;

        EntityManager.RaisePredictiveEvent(new RequestShootEvent
        {
            Target = target,
            Coordinates = GetNetCoordinates(coordinates),
            Gun = GetNetEntity(gunUid),
        });
    }
}
