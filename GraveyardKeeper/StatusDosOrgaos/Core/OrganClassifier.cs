using System.Collections.Generic;

namespace StatusDosOrgaos.Core;

public enum OrganScope
{
    Orgaos,
    TodasPartesDoCorpo
}

public static class OrganClassifier
{
    private const string BodyPartType = "BodyUniversalPart";

    private static readonly HashSet<string> OrganBases = new HashSet<string>
    {
        "heart", "brain", "intestine", "lungs", "eye", "tongue", "scalp", "muscle"
    };

    public static bool IsOrgan(string id, string itemType, OrganScope scope)
    {
        if (string.IsNullOrEmpty(id) || itemType != BodyPartType) return false;
        if (scope == OrganScope.TodasPartesDoCorpo) return true;

        var baseId = id;
        var colon = baseId.IndexOf(':');
        if (colon >= 0) baseId = baseId.Substring(0, colon);
        if (baseId.EndsWith("_dark")) baseId = baseId.Substring(0, baseId.Length - "_dark".Length);

        return OrganBases.Contains(baseId);
    }
}
