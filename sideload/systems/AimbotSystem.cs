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
using Content.Shared.Mobs;
using Content.Shared.NukeOps;
using Content.Shared.RatKing;

namespace Based.Systems;

[HarmonyPatch]
public static class AimbotPatch
{
    static bool IsAimbotEnabled()
    {
        var sys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<BasedSystem>();
        return sys.AimbotEnabled;
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

    private EntityUid player;
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

    enum Teams
    {
        NONE,
        NUKIE,
        RATKING,
        SECURITY,
        PASSENGER,
        DRAGON,
        SLIME,
        REAGENTSLIME,
        CLOWNSPIDER,
        GIANTSPIDER

    }

    private Teams GetTeam(EntityUid ent)
    {
        if (TryComp(ent, out NukeOperativeComponent? n))
            return Teams.NUKIE;
        if (TryComp(ent, out RatKingComponent? r) || (TryComp(ent, out RatKingServantComponent? rs)))
            return Teams.RATKING;

        // Find team based on metadata
        if (TryComp(ent, out MetaDataComponent? meta) && meta.EntityPrototype != null)
        {
            if (meta.EntityPrototype.ID.Contains("Dragon"))
                return Teams.DRAGON;
            if (meta.EntityPrototype.ID.Contains("MobAdultSlimes"))
                return Teams.SLIME;
            if (meta.EntityPrototype.ID.Contains("ReagentSlime"))
                return Teams.REAGENTSLIME;
            if (meta.EntityPrototype.ID.Equals("MobClownSpider"))
                return Teams.CLOWNSPIDER;
            if (meta.EntityPrototype.ID.Contains("MobGiantSpider"))
                return Teams.GIANTSPIDER;
        }
        return Teams.NONE;
    }

    private bool IsOnMyTeam(EntityUid ent)
    {
        Teams myTeam = GetTeam(player);
        if (myTeam == Teams.NONE) return false;
        if (myTeam == GetTeam(ent)) return true;
        return false;
    }

    private bool Filter(Entity<TransformComponent> ent)
    {
        if (ent.Comp.MapID != _eye.CurrentMap) return false;
        if (!TryComp(ent, out MobStateComponent? state))
            return false;
        if (state.CurrentState != MobState.Alive) return false;
        if (IsOnMyTeam(ent.Owner)) return false;
        return true;
    }

    private float EntDistance(EntityUid e1, EntityUid e2)
    {
        var entityMapPos = _ts.GetMapCoordinates(Transform(e1));
        var pos2 = _ts.GetMapCoordinates(Transform(e2));
        var vector = pos2.Position - entityMapPos.Position;
        return vector.Length();
    }
    private EntityUid? GetClosestTo(MapCoordinates coordinates, HashSet<EntityUid> entities)
    {
        MapCoordinates? closestCoordinates = null;
        EntityUid player = _playerMan.LocalEntity.Value;
        EntityUid? closestEntity = null;
        var closestDistance = float.MaxValue;
        foreach (var ent in entities)
        {
            var transform = Transform(ent);
            if (!Filter((ent, transform)))
                continue;
            var entityMapPos = _ts.GetMapCoordinates(transform);
            var vector = coordinates.Position - entityMapPos.Position;
            var distance = vector.Length();
            if (!(distance < closestDistance)) continue;
            // Do not target if they are not in range un-obstructed from our player
            if (!_is.InRangeUnobstructed(player, ent, EntDistance(player, ent) + 0.1f))
                continue;
            closestCoordinates = entityMapPos;
            closestDistance = distance;
            closestEntity = ent;
        }

        if (closestEntity is null)
            return null;

        return closestEntity;
    }
    public EntityUid? GetClosestInRange(
    MapCoordinates coordinates,
    float range,
    HashSet<EntityUid>? exclude = null)
    {
        var entitiesInRange = _entityLookup.GetEntitiesInRange(coordinates, range, LookupFlags.Uncontained);

        if (exclude != null)
            entitiesInRange.ExceptWith(exclude);

        return GetClosestTo(coordinates, entitiesInRange);
    }

    private EntityUid? GetTarget(EntityUid player, AimMode mode, float playerRange)
    {
        var exclude = new HashSet<EntityUid>();
        exclude.Add(player); // never target ourself...

        switch (mode)
        {
            case AimMode.NEAR_PLAYER:
                return GetClosestInRange(_ts.GetMapCoordinates(Transform(player)), playerRange, exclude);
            case AimMode.NEAR_MOUSE:
                var entitiesInRange = _entityLookup.GetEntitiesInRange(Transform(player).Coordinates, playerRange, LookupFlags.Uncontained);
                entitiesInRange.ExceptWith(exclude);
                return GetClosestTo(_eye.PixelToMap(_inputManager.MouseScreenPosition), entitiesInRange);
            default:
                return null;
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        // Early bail if no player
        EntityUid? p = _playerMan.LocalEntity;
        if (p == null) return;
        this.player = p.Value;

        var sys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<BasedSystem>();
        AimMode mode = sys.curAimbotMode;
        bool enabled = sys.AimbotEnabled;

        if (!enabled)
            return;

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

        /* Get target  */
        EntityUid? target = null;
        if (meleeWeapon != null)
        {
            target = GetTarget(player, mode, meleeWeapon.Range);
        }
        else if (gun != null)
        {
            target = GetTarget(player, mode, 10f);
        }

        if (target == null) return;

        if (useDown == BoundKeyState.Down)
        {
            if (meleeWeapon != null)
            {
                // Swing batter batter, swing!
                _entityManager.RaisePredictiveEvent(new LightAttackEvent
                    (
                        EntityManager.GetNetEntity(target),
                        EntityManager.GetNetEntity(weaponUid),
                        EntityManager.GetNetCoordinates(Transform(target.Value).Coordinates)
                    )
                 );
                return;
            }
            else if (gun != null)
            {
                // Shoot
                // Define target coordinates relative to gunner entity, so that network latency on moving grids doesn't fuck up the target location.
                var coordinates = _ts.ToCoordinates(player, Transform(target.Value).MapPosition);
                this.shoot(gun, weaponUid, target.Value, coordinates);
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
                        _entityManager.GetNetCoordinates(Transform(target.Value).Coordinates)
                    )
                );
            }
            else
            {

                MapCoordinates targetMap = _ts.ToMapCoordinates(Transform(target.Value).Coordinates);

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
                    _entityManager.GetNetCoordinates(Transform(target.Value).Coordinates)
                    ));
            }
            return;
        } // usekey down
    }

    private void shoot(GunComponent gun, EntityUid gunUid, EntityUid target, EntityCoordinates coordinates)
    {
        if (gun.NextFire > _timing.CurTime)
            return;

        EntityManager.RaisePredictiveEvent(new RequestShootEvent
        {
            //Target = EntityManager.GetNetEntity(target),
            Coordinates = GetNetCoordinates(coordinates),
            Gun = GetNetEntity(gunUid),
        });
    }
}
