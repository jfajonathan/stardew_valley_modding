using System.Collections.Generic;
using HarmonyLib;
using StatusDosOrgaos.Core;

namespace StatusDosOrgaos.Patches;

[HarmonyPatch(typeof(ItemDefinition), nameof(ItemDefinition.GetTooltipData))]
internal static class TooltipSummaryPatch
{
    [HarmonyPostfix]
    private static void Postfix(Item item, bool full_detail, List<BubbleWidgetData> __result)
    {
        if (!full_detail || __result == null) return;
        if (!Plugin.ShowSummary.Value || !Plugin.IsTargetOrgan(item)) return;

        var summary = SkullFormatter.Summary(item.GetRedSkullsValue(), item.GetWhiteSkullsValue());
        __result.Add(new BubbleWidgetTextData(summary, UITextStyles.TextStyle.TinyDescription));
    }
}
