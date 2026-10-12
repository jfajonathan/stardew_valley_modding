using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using StatusDosOrgaos.Core;

namespace StatusDosOrgaos;

[BepInPlugin(Guid, Name, Version)]
public class Plugin : BaseUnityPlugin
{
    public const string Guid = "jonathan.gyk.statusdosorgaos";
    public const string Name = "Status dos Órgãos";
    public const string Version = "1.0.0";

    internal static ManualLogSource Log;
    internal static ConfigEntry<bool> Enabled;
    internal static ConfigEntry<OrganScope> Scope;
    internal static ConfigEntry<bool> ShowIcons;
    internal static ConfigEntry<bool> ShowSummary;

    private void Awake()
    {
        Log = Logger;

        Enabled = Config.Bind("1. Geral", "Ativado", true, "Mostra as caveiras dos órgãos no tooltip.");
        Scope = Config.Bind("1. Geral", "Escopo", OrganScope.Orgaos,
            "Orgaos = coração, cérebro, intestino, pulmões, olho, língua, couro cabeludo, músculo. " +
            "TodasPartesDoCorpo = inclui carne, sangue, gordura, pele, osso e caveira.");
        ShowIcons = Config.Bind("2. Exibição", "Mostrar ícones de caveira", true,
            "Libera a linha de ícones que o jogo normalmente esconde até mais tarde na história.");
        ShowSummary = Config.Bind("2. Exibição", "Mostrar linha com números", true,
            "Acrescenta a linha '(ícone) Vermelhas: X   (ícone) Brancas: Y' no tooltip.");

        Harmony.CreateAndPatchAll(typeof(Plugin).Assembly, Guid);
        Log.LogInfo($"{Name} {Version} carregado.");
    }

    internal static bool IsTargetOrgan(Item item)
    {
        if (!Enabled.Value || item == null || item.IsEmpty()) return false;
        var def = item.definition;
        return def != null && OrganClassifier.IsOrgan(def.id, def.type.ToString(), Scope.Value);
    }
}
