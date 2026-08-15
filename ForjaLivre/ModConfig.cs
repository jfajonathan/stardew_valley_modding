namespace ForjaLivre
{
    /// <summary>Configurações do mod, editáveis em config.json depois da primeira execução.</summary>
    internal sealed class ModConfig
    {
        /// <summary>Número máximo de níveis de forja (estatísticas) que uma arma pode acumular no total.
        /// O jogo original limita a 3; um valor alto na prática remove o limite.</summary>
        public int MaxForgeLevel { get; set; } = 999;
    }
}
