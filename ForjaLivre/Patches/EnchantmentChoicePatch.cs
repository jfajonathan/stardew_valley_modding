using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Tools;

namespace ForjaLivre.Patches
{
    /// <summary>Substitui <see cref="BaseEnchantment.GetEnchantmentFromItem"/> — o método que decide
    /// qual estatística uma gema concede (normalmente fixo por tipo de gema) ou qual encantamento um
    /// Fragmento Prismático concede (normalmente aleatório) — pela escolha do jogador guardada em
    /// <see cref="ForgeChoiceState"/>, quando houver uma escolha pendente para a arma em questão.
    /// Sem escolha pendente, o comportamento original do jogo continua valendo normalmente.</summary>
    [HarmonyPatch(typeof(BaseEnchantment), nameof(BaseEnchantment.GetEnchantmentFromItem))]
    internal static class EnchantmentChoicePatch
    {
        private static readonly HashSet<string> BasicGemIds = new()
        {
            "(O)60", // Esmeralda
            "(O)62", // Água-marinha
            "(O)64", // Rubi
            "(O)66", // Ametista
            "(O)68", // Topázio
            "(O)70", // Jade
        };

        private const string PrismaticShardId = "(O)74";

        private static bool Prefix(Item base_item, Item item, ref BaseEnchantment __result)
        {
            if (base_item is not MeleeWeapon weapon || weapon.isScythe())
                return true;

            if (!ReferenceEquals(weapon, ForgeChoiceState.TargetWeapon))
                return true;

            string? qualifiedId = item?.QualifiedItemId;

            if (qualifiedId != null && BasicGemIds.Contains(qualifiedId) && ForgeChoiceState.ChosenStatEnchantment != null)
            {
                __result = (BaseEnchantment)Activator.CreateInstance(ForgeChoiceState.ChosenStatEnchantment)!;
                return false;
            }

            if (qualifiedId == PrismaticShardId && ForgeChoiceState.ChosenWeaponEnchantment != null)
            {
                __result = (BaseEnchantment)Activator.CreateInstance(ForgeChoiceState.ChosenWeaponEnchantment)!;
                return false;
            }

            return true;
        }
    }
}
