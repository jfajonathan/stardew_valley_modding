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

    /// <summary>Catálogo das estatísticas de forja e dos encantamentos de arma disponíveis na Forja
    /// do Vulcão, conforme https://pt.stardewvalleywiki.com/Forja.</summary>
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

        /// <summary>Os 5 encantamentos de arma aplicáveis com Fragmento Prismático.</summary>
        public static readonly ForgeOption[] WeaponEnchantOptions =
        {
            new ForgeOption(typeof(ArtfulEnchantment), "enchant.artful"),
            new ForgeOption(typeof(BugKillerEnchantment), "enchant.bugkiller"),
            new ForgeOption(typeof(CrusaderEnchantment), "enchant.crusader"),
            new ForgeOption(typeof(VampiricEnchantment), "enchant.vampiric"),
            new ForgeOption(typeof(HaymakerEnchantment), "enchant.haymaker"),
        };
    }
}
