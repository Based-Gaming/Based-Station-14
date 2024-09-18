using System.Reflection;
using HarmonyLib;

using Content.Shared.StatusIcon.Components;
using Robust.Shared.GameObjects;
using Content.Shared.StoreDiscount.Components;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Content.Shared.Access.Systems;
using Robust.Shared.IoC;

[HarmonyPatch]
public class UplinkIsSyndiePatch
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Content.Client.Overlays.ShowJobIconsSystem"), "OnGetStatusIconsEvent");
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

        IPrototypeManager _prototype = Traverse.Create(__instance).Field("_prototype").GetValue<IPrototypeManager>();
        AccessReaderSystem _accessReader = Traverse.Create(__instance).Field("_accessReader").GetValue<AccessReaderSystem>();


        if (_accessReader.FindAccessItemsInventory(uid, out var items))
        {

            foreach (EntityUid item in items)
            {
                IEntityManager _entityManager = IoCManager.Resolve<IEntityManager>();
                if (_entityManager.TryGetComponent<StoreDiscountComponent>(item, out var comp))
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
    }
}