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
using Robust.Shared.Timing;

public sealed class AntiSlipSystem : EntitySystem
{
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    private bool _changed = false;
    private bool _forcePressWalk;
    private IConsoleCommand? _cmd = null;

    public override void Initialize()
    {
        base.Initialize();
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

    private (float, float) GetPlayerSpeed(EntityUid player)
    {
        TryComp(player, out MovementSpeedModifierComponent? comp);
        return (comp?.CurrentWalkSpeed ?? MovementSpeedModifierComponent.DefaultBaseWalkSpeed,
            comp?.CurrentSprintSpeed ?? MovementSpeedModifierComponent.DefaultBaseSprintSpeed);
    }

    private bool IsNearSlip(EntityUid player)
    {
        foreach (var entity in _entityLookup.GetEntitiesInRange(player, 0.5f, LookupFlags.Uncontained).ToList()
             .Where(HasComp<SlipperyComponent>))
        {
            if (!TryComp<StepTriggerComponent>(entity, out var triggerComponent)) continue;
            if (!triggerComponent.Active)
                continue;
            var (walking, sprint) = GetPlayerSpeed(player);
            if (sprint <= triggerComponent.RequiredTriggeredSpeed) continue;
            if (walking >= triggerComponent.RequiredTriggeredSpeed) continue; // Ignore if we can't resist it
            return true;
        }
        return false;
    }

    public override void Update(float frameTime)
    {
        if (!_playerManager.LocalEntity.HasValue || !IsAntiSlipEnabled())
            return;

        bool onSlip = IsNearSlip(_playerManager.LocalEntity.Value);
        _changed = onSlip != _forcePressWalk;
        _forcePressWalk = onSlip;
    }

    public override void FrameUpdate(float frameTime)
    {
        if (_changed)
            PressWalk(_forcePressWalk ? BoundKeyState.Down : BoundKeyState.Up);
    }

    private void PressWalk(BoundKeyState state)
    {
        if (!_playerManager.LocalEntity.HasValue)
            return;

        var player = _playerManager.LocalEntity.Value;
        var playerCord = _transform.GetMoverCoordinates(player);
        var screenCord = _eyeManager.CoordinatesToScreen(_transform.GetMoverCoordinates(player));
        var keyFunctionId = _inputManager.NetworkBindMap.KeyFunctionID(EngineKeyFunctions.Walk);


        var message = new ClientFullInputCmdMessage(_gameTiming.CurTick, _gameTiming.TickFraction, keyFunctionId)
        {
            State = state,
            Coordinates = playerCord,
            ScreenCoordinates = screenCord,
            Uid = EntityUid.Invalid,
        };

        _inputSystem.HandleInputCommand(_playerManager.LocalSession, EngineKeyFunctions.Walk, message);
    }
}