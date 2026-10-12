using System;
using StardewValley;

namespace ForjaLivre
{
    /// <summary>Guarda, enquanto a Forja está aberta, qual estatística/encantamento o jogador escolheu
    /// para a próxima gema/Fragmento Prismático que ele encaixar. É lido pelos patches em
    /// <see cref="Patches.EnchantmentChoicePatch"/> no lugar do comportamento fixo/aleatório do jogo.
    /// Vale tanto para armas corpo-a-corpo (estatística + encantamento) quanto para qualquer outra
    /// ferramenta (só encantamento — ferramentas não têm o sistema de estatística por gema).</summary>
    internal static class ForgeChoiceState
    {
        /// <summary>O item (arma ou ferramenta) para o qual a escolha atual é válida. Usado para
        /// invalidar a escolha automaticamente se o jogador trocar o item no slot esquerdo da Forja.</summary>
        public static Item? TargetItem { get; set; }

        /// <summary>Tipo de encantamento de gema (Rubi, Esmeralda, etc.) escolhido pelo jogador.
        /// Só se aplica a armas corpo-a-corpo.</summary>
        public static Type? ChosenStatEnchantment { get; set; }

        /// <summary>Tipo de encantamento escolhido pelo jogador para aplicar com o Fragmento Prismático
        /// — vale tanto para o encantamento de arma (Hábil, Vampírico etc.) quanto para o de ferramenta
        /// (Eficiente, Generoso, Arqueólogo etc.).</summary>
        public static Type? ChosenEnchantment { get; set; }

        /// <summary>Nome de exibição (já traduzido pelo próprio jogo) do <see cref="ChosenEnchantment"/>,
        /// guardado no momento da escolha para não precisar recriar a instância só para exibir o rótulo
        /// do botão a cada frame.</summary>
        public static string? ChosenEnchantmentLabel { get; set; }

        public static void Clear()
        {
            TargetItem = null;
            ChosenStatEnchantment = null;
            ChosenEnchantment = null;
            ChosenEnchantmentLabel = null;
        }
    }
}
