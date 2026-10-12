using System;
using StardewValley.Enchantments;

namespace ForjaLivre
{
    /// <summary>Uma opção selecionável no menu de escolha da Forja: o tipo de encantamento real do jogo
    /// que será instanciado, e a chave de tradução usada para exibir seu nome.</summary>
    internal readonly struct ForgeOption
    {
        public readonly Type EnchantmentType;
        public readonly string TranslationKey;

        public ForgeOption(Type enchantmentType, string translationKey)
        {
            this.EnchantmentType = enchantmentType;
            this.TranslationKey = translationKey;
        }
    }

    /// <summary>Catálogo das estatísticas de forja aplicáveis por gema em armas corpo-a-corpo
    /// (Rubi, Esmeralda, Água-marinha, Jade, Topázio, Ametista), conforme
    /// https://pt.stardewvalleywiki.com/Forja. Esse mapeamento gema→estatística é fixo no próprio
    /// jogo (não depende de <c>BaseEnchantment.GetAvailableEnchantmentsForItem</c>), por isso continua
    /// sendo um catálogo escrito à mão.
    ///
    /// Já os encantamentos aplicados com o Fragmento Prismático — tanto os de arma (Hábil, Vampírico
    /// etc.) quanto os de qualquer outra ferramenta (Eficiente, Generoso, Arqueólogo etc.) — não têm
    /// mais um catálogo aqui: <see cref="Patches.ForgeMenuUiPatch"/> pergunta diretamente ao jogo, via
    /// <c>BaseEnchantment.GetAvailableEnchantmentsForItem</c>, quais encantamentos aquele item específico
    /// pode receber. Isso já vem filtrado pelo próprio jogo para nunca repetir um encantamento que o item
    /// já tem, e cobre automaticamente qualquer encantamento de ferramenta que exista (presente ou
    /// futuro), sem precisar manter uma lista fixa por tipo de ferramenta.</summary>
    internal static class ForgeOptions
    {
        /// <summary>As 6 estatísticas aplicáveis por gema (Rubi, Esmeralda, Água-marinha, Jade, Topázio, Ametista).</summary>
        public static readonly ForgeOption[] StatOptions =
        {
            new ForgeOption(typeof(RubyEnchantment), "stat.damage"),
            new ForgeOption(typeof(EmeraldEnchantment), "stat.speed"),
            new ForgeOption(typeof(AquamarineEnchantment), "stat.critchance"),
            new ForgeOption(typeof(JadeEnchantment), "stat.critpower"),
            new ForgeOption(typeof(TopazEnchantment), "stat.defense"),
            new ForgeOption(typeof(AmethystEnchantment), "stat.weight"),
        };
    }
}
