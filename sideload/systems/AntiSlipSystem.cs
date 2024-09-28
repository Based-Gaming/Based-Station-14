#if DEBUG
using Content.Client.Interactable;
using Content.Client.Weapons.Melee;
using Content.Client.Weapons.Ranged.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Slippery;
using Content.Shared.StepTrigger.Components;
using HarmonyLib;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using System.Reflection;


public sealed class AntiSlipSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IPlayerManager _playerMan = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    protected EntityQuery<InputMoverComponent> MoverQuery;

    private bool _frozen = false;
    private IConsoleCommand? _cmd = null;

    public override void Initialize()
    {
        base.Initialize();

        MoverQuery = GetEntityQuery<InputMoverComponent>();

    }
    private bool IsAntiSlipEnabled()
    {
        if (_cmd == null)
        {
            foreach (var kvp in _consoleHost.AvailableCommands)
            {
                if (kvp.Key.Equals("based.antislip"))
                {
                    _cmd = kvp.Value;
                    break;
                }
            }
            if (_cmd == null)
                return false;
        }
        return Traverse.Create(_cmd).Field("enabled").GetValue<bool>();
    }

    private void Freeze(EntityUid player)
    {
        if (!MoverQuery.TryGetComponent(player, out var mover))
            return;
        MarseyLogger.Log(MarseyLogger.LogType.INFO, "SLIPPERY FREEZE");
        mover.CanMove = false;
        this.Dirty(player, mover);
        _frozen = true;
    }

    private void UnFreeze(EntityUid player)
    {
        if (!MoverQuery.TryGetComponent(player, out var mover))
            return;
        MarseyLogger.Log(MarseyLogger.LogType.INFO, "SLIPPERY UNFREEZE");
        mover.CanMove = true;
        this.Dirty(player, mover);
        _frozen = false;
    }

    public override void FrameUpdate(float frameTime)
    {
        // Early bail if no player
        EntityUid? p = _playerMan.LocalEntity;
        if (p == null) return;
        EntityUid player = (EntityUid)p;

        if (!this.IsAntiSlipEnabled())
        {
            if (_frozen)
                this.UnFreeze(player);
            return;
        }

        if (!_entityManager.TryGetComponent<TransformComponent>(player, out TransformComponent? playerXform))
            return;

        var closeEnts = new HashSet<Entity<SlipperyComponent>>();
        _entityLookup.GetEntitiesInRange(playerXform.Coordinates, 0.3f, closeEnts);

        foreach (var ent in closeEnts)
        {
            if (!_entityManager.TryGetComponent<SlipperyComponent>(ent, out var slipComp))
                continue;
            MarseyLogger.Log(MarseyLogger.LogType.INFO, "SLIPPERY CLOSE");

            this.Freeze(player);
            return;
        }
        // if we get here, make sure we are not frozen
        if (_frozen)
            this.UnFreeze(player);
    }
}
#endif