using System;
using StardewValley;

namespace ForjaLivre
{
    /// <summary>Guarda, enquanto a Forja está aberta, qual estatística/encantamento o jogador escolheu
    /// para a próxima gema/Fragmento Prismático que ele encaixar. É lido pelos patches em
    /// <see cref="Patches.EnchantmentChoicePatch"/> no lugar do comportamento fixo/aleatório do jogo.</summary>
    internal static class ForgeChoiceState
    {
        /// <summary>A arma para a qual a escolha atual é válida. Usado para invalidar a escolha
        /// automaticamente se o jogador trocar a arma no slot esquerdo da Forja.</summary>
        public static Item? TargetWeapon { get; set; }

        /// <summary>Tipo de encantamento de gema (Rubi, Esmeralda, etc.) escolhido pelo jogador.</summary>
        public static Type? ChosenStatEnchantment { get; set; }

        /// <summary>Tipo de encantamento de arma (Hábil, Vampírico, etc.) escolhido pelo jogador.</summary>
        public static Type? ChosenWeaponEnchantment { get; set; }

        public static void Clear()
        {
            TargetWeapon = null;
            ChosenStatEnchantment = null;
            ChosenWeaponEnchantment = null;
        }
    }
}
