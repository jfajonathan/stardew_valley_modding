using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Tools;

namespace ForjaLivre.Patches
{
    /// <summary>Substitui <see cref="BaseEnchantment.GetEnchantmentFromItem"/> — o método que decide
    /// qual estatística uma gema concede a uma arma (normalmente fixo por tipo de gema), ou qual
    /// encantamento um Fragmento Prismático concede a uma arma OU a qualquer outra ferramenta
    /// (normalmente aleatório, sorteado entre os encantamentos que aquele item específico pode
    /// receber) — pela escolha do jogador guardada em <see cref="ForgeChoiceState"/>, quando houver
    /// uma escolha pendente para o item em questão. Sem escolha pendente, o comportamento original
    /// do jogo continua valendo normalmente (inclusive o sorteio aleatório em ferramentas).</summary>
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
            // No jogo original, gema/Fragmento Prismático só enchantam algo que seja uma Tool —
            // toda arma corpo-a-corpo já é uma Tool, então isso cobre os dois casos.
            if (base_item is not Tool tool)
                return true;

            // Foices nunca podem ser encantadas (nem no jogo original).
            if (tool is MeleeWeapon meleeWeapon && meleeWeapon.isScythe())
                return true;

            if (!ReferenceEquals(tool, ForgeChoiceState.TargetItem))
                return true;

            string? qualifiedId = item?.QualifiedItemId;

            // O mapeamento gema → estatística só existe para armas corpo-a-corpo no jogo original.
            if (tool is MeleeWeapon && qualifiedId != null && BasicGemIds.Contains(qualifiedId) && ForgeChoiceState.ChosenStatEnchantment != null)
            {
                __result = (BaseEnchantment)Activator.CreateInstance(ForgeChoiceState.ChosenStatEnchantment)!;
                return false;
            }

            // Fragmento Prismático: no jogo original sorteia um encantamento aleatório entre os
            // disponíveis pra esse item (arma ou qualquer outra ferramenta) — aqui usamos a escolha
            // do jogador em vez do sorteio.
            if (qualifiedId == PrismaticShardId && ForgeChoiceState.ChosenEnchantment != null)
            {
                __result = (BaseEnchantment)Activator.CreateInstance(ForgeChoiceState.ChosenEnchantment)!;
                return false;
            }

            return true;
        }
    }
}
