using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Tools;
using HarmonyLib;

namespace ForjaLivre.Patches
{
    /// <summary>No jogo original, <c>Tool.AddEnchantment</c> só empilha nível (em vez de substituir)
    /// para encantamentos "de forja" (<see cref="BaseEnchantment.IsForge"/> — as 6 estatísticas de
    /// gema, exclusivas de armas) e para os marcados como <see cref="BaseEnchantment.IsSecondaryEnchantment"/>,
    /// e só nesse caminho quando o item é uma <c>MeleeWeapon</c>. Qualquer outro encantamento — os
    /// "especiais" concedidos por Fragmento Prismático (Vampírico, Cruzado, Matador de Insetos,
    /// Crítico, Marreteiro, Sofisticado, Mágico... em armas; Eficiente, Generoso, Conservante,
    /// Arqueólogo, Pescador, Sem Fundo, Aparador... em qualquer outra ferramenta) usa um "slot único":
    /// aplicar um novo remove todos os anteriores desse grupo antes de adicionar o escolhido. Isso
    /// vale tanto pra armas (fora do caminho de forja) quanto — sempre, já que <c>this is MeleeWeapon</c>
    /// nunca é verdade pra elas — pra Machado, Enxada, Regador, Vara de Pescar, Picareta, Balde de
    /// Leite, Tesoura, Peneira etc., que por isso só aceitavam 1 encantamento por vez (ver
    /// <c>Tool.AddEnchantment</c> decompilado: o laço final que faz <c>UnapplyTo</c> + <c>RemoveAt</c>
    /// em qualquer encantamento que não seja <c>IsForge()</c> nem <c>IsSecondaryEnchantment()</c>).
    ///
    /// Este patch intercepta <c>Tool.AddEnchantment</c> antes desse laço rodar, pra QUALQUER
    /// <c>Tool</c> — arma ou não: se o encantamento sendo aplicado é um "especial", adiciona ele
    /// direto na lista sem remover os que já estavam — até o limite configurado em
    /// <see cref="ModConfig.MaxSpecialEnchantments"/>. As estatísticas de gema continuam funcionando
    /// exatamente como antes (esse caminho nem passa por aqui, retorna logo no início). O botão
    /// "Escolher Encantamento" da Forja (<see cref="ForgeMenuUiPatch"/>) já funcionava pra qualquer
    /// ferramenta antes deste patch — só a parte de empilhar (em vez de substituir) faltava.</summary>
    [HarmonyPatch(typeof(Tool), nameof(Tool.AddEnchantment))]
    internal static class SpecialEnchantmentStackPatch
    {
        private static bool Prefix(Tool __instance, BaseEnchantment enchantment, ref bool __result)
        {
            if (enchantment == null)
                return true; // comportamento original: nulo sempre falha.

            if (enchantment.IsForge() || enchantment.IsSecondaryEnchantment())
                return true; // estatísticas de gema (e qualquer futuro "secundário") já empilham normalmente.

            // Encantamento "especial" (Fragmento Prismático) — em qualquer Tool, arma ou não. Se já
            // existe um do mesmo tipo, tenta subir o nível dele em vez de duplicar a entrada (a
            // maioria não usa nível no efeito, então isso só evita duas linhas iguais na lista).
            foreach (BaseEnchantment existing in __instance.enchantments)
            {
                if (existing.GetType() == enchantment.GetType())
                {
                    if (existing.GetMaximumLevel() < 0 || existing.GetLevel() < existing.GetMaximumLevel())
                    {
                        existing.SetLevel(__instance, existing.GetLevel() + 1);
                        __result = true;
                        return false;
                    }
                    __result = false;
                    return false;
                }
            }

            int specialCount = 0;
            foreach (BaseEnchantment existing in __instance.enchantments)
            {
                if (!existing.IsForge() && !existing.IsSecondaryEnchantment())
                    specialCount++;
            }

            if (specialCount >= ModEntry.Config.MaxSpecialEnchantments)
                return true; // atingiu o limite configurado: cai no comportamento original (substitui o antigo).

            __instance.enchantments.Add(enchantment);
            enchantment.ApplyTo(__instance, Game1.player);
            __result = true;
            return false; // pula o AddEnchantment original: já adicionamos o encantamento nós mesmos.
        }
    }
}
