using HarmonyLib;
using StardewValley.Tools;

namespace ForjaLivre.Patches
{
    /// <summary>Remove o limite de 3 níveis de forja por arma (<see cref="MeleeWeapon.GetMaxForges"/>),
    /// permitindo empilhar quantos níveis de estatística o jogador quiser, até
    /// <see cref="ModConfig.MaxForgeLevel"/>.</summary>
    [HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.GetMaxForges))]
    internal static class ForgeCapPatch
    {
        private static void Postfix(ref int __result)
        {
            __result = ModEntry.Config.MaxForgeLevel;
        }
    }
}
