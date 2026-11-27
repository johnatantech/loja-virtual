using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Loja.Domain.Comum;

/// <summary>Gera identificadores amigáveis para URLs: "Café Especial" vira "cafe-especial".</summary>
public static partial class Slug
{
    public static string Gerar(string texto)
    {
        var semAcento = new StringBuilder();
        foreach (var c in texto.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                semAcento.Append(c);
        }
        var slug = NaoAlfanumerico().Replace(semAcento.ToString().ToLowerInvariant(), "-");
        return slug.Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NaoAlfanumerico();
}
