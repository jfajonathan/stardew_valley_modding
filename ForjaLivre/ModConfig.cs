namespace ForjaLivre
{
    /// <summary>Configurações do mod, editáveis em config.json depois da primeira execução.</summary>
    internal sealed class ModConfig
    {
        /// <summary>Número máximo de níveis de forja (estatísticas) que uma arma pode acumular no total.
        /// O jogo original limita a 3; um valor alto na prática remove o limite.</summary>
        public int MaxForgeLevel { get; set; } = 999;

        /// <summary>Número máximo de encantamentos especiais (os concedidos por Fragmento Prismático —
        /// Vampírico, Cruzado, Matador de Insetos, Crítico, Marreteiro, Sofisticado etc.) que uma arma
        /// pode acumular ao mesmo tempo. No jogo original esse número é sempre 1 (aplicar um novo
        /// substitui o anterior); um valor alto na prática remove o limite, deixando a arma acumular
        /// vários encantamentos especiais diferentes ao mesmo tempo. Veja
        /// <see cref="Patches.SpecialEnchantmentStackPatch"/>.</summary>
        public int MaxSpecialEnchantments { get; set; } = 999;
    }
}
