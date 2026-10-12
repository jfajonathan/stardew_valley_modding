using HarmonyLib;
using StatusDosOrgaos.Core;

namespace StatusDosOrgaos.Patches;

/// <summary>
/// O jogo só mostra as caveiras quando show_q_hint (ex.: Ppar("p_cultist") > 0) é verdadeiro.
/// Para órgãos, devolvemos sempre o texto no mesmo formato, usando os valores reais do item
/// (incluindo modificadores do Organ Enhancer).
/// </summary>
[HarmonyPatch(typeof(Item), nameof(Item.GetItemBodyModificators))]
internal static class BodyModificatorsPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Item __instance, ref string __result)
    {
        if (!Plugin.ShowIcons.Value || !Plugin.IsTargetOrgan(__instance)) return true;

        __result = SkullFormatter.Icons(__instance.GetRedSkullsValue(), __instance.GetWhiteSkullsValue());
        return false;
    }
}
