using Robust.Shared.Map;
using Robust.Shared.GameObjects;
using Content.Client.Examine;
using Content.Shared.Electrocution;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;

public sealed class BudgetInsulSystem : EntitySystem
{
    [ValidatePrototypeId<EntityPrototype>]
    private const string Marker = "EffectEmpDisabled";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<InsulatedComponent, ClientExaminedEvent>(BudgetInsulExamined);
    }

    private void BudgetInsulExamined(EntityUid uid, InsulatedComponent component, ClientExaminedEvent args)
    {
        if (component.Coefficient == 0.0)
            Spawn(Marker, new EntityCoordinates(uid, 0, 0));
    }
}
