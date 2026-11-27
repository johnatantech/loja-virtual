using System.Globalization;

namespace Loja.Domain.Comum;

/// <summary>
/// Formata valores em reais para mensagens ao cliente ("R$ 1.234,56") sem depender da
/// cultura do servidor, que em contêineres costuma ser a invariante.
/// </summary>
public static class Moeda
{
    private static readonly NumberFormatInfo Formato = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberDecimalDigits = 2,
    };

    public static string Formatar(decimal valor) => "R$ " + valor.ToString("N2", Formato);
}
