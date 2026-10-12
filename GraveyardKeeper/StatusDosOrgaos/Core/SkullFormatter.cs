using System;
using System.Text;

namespace StatusDosOrgaos.Core;

public static class SkullFormatter
{
    /// <summary>Reproduz o formato de Item.GetItemBodyModificators(): "+(rskull)…, +(skull)…".</summary>
    public static string Icons(int red, int white)
    {
        var text = "";
        if (red != 0) text = Append(text, red, "(rskull)");
        if (white != 0) text = Append(text, white, "(skull)");
        return text;
    }

    public static string Summary(int red, int white) =>
        $"(rskull) Vermelhas: {red}   (skull) Brancas: {white}";

    private static string Append(string text, int value, string icon)
    {
        var sb = new StringBuilder(text);
        if (sb.Length > 0) sb.Append(", ");
        sb.Append(value < 0 ? "-" : "+");
        for (var i = 0; i < Math.Abs(value); i++) sb.Append(icon);
        return sb.ToString();
    }
}
