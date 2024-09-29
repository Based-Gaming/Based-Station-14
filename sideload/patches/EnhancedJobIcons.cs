using System.Reflection;
using HarmonyLib;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.GameObjects;
using Content.Shared.StoreDiscount.Components;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Content.Shared.Access.Systems;
using Robust.Shared.IoC;
using Content.Shared.CombatMode.Pacification;


[HarmonyPatch]
public static class EnhancedJobIconsPatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(Content.Client.Overlays.ShowJobIconsSystem), "OnGetStatusIconsEvent");
    }

    [HarmonyPostfix]
    private static void TypePostfix(object __instance, EntityUid uid, StatusIconComponent _, ref GetStatusIconsEvent ev)
    {

        bool? isActive = Traverse.Create(__instance).Field("IsActive").GetValue<bool>();
        if (isActive == null)
        {
            isActive = true;
        }
        if (!isActive.Value) return;

        //
        // Check for an uplink-enabled PDA on the person
        //
        IPrototypeManager _prototype = Traverse.Create(__instance).Field("_prototype").GetValue<IPrototypeManager>();
        AccessReaderSystem _accessReader = Traverse.Create(__instance).Field("_accessReader").GetValue<AccessReaderSystem>();
        IEntityManager _entityManager = IoCManager.Resolve<IEntityManager>();

        if (_accessReader.FindAccessItemsInventory(uid, out var items))
        {

            foreach (EntityUid item in items)
            {
                if (_entityManager.TryGetComponent<StoreDiscountComponent>(item, out var disComp))
                {

                    // If their PDA has StoreDiscount then they have an uplink
                    if (_prototype.TryIndex<FactionIconPrototype>("SyndicateFaction", out var iconPrototype))
                    {
                        ev.StatusIcons.Add(iconPrototype);
                    }
                    break;
                }
            }
        }
        //
        // Check for a pacifist (thief)
        //
        if (_entityManager.TryGetComponent<PacifiedComponent>(uid, out var pacComp))
        {
            if (_prototype.TryIndex<JobIconPrototype>("StationAi", out var iconPrototype))
            {
                ev.StatusIcons.Add(iconPrototype);
            }
        }
    }
}