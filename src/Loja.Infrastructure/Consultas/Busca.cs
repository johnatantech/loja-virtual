namespace Loja.Infrastructure.Consultas;

internal static class Busca
{
    /// <summary>
    /// Monta o padrão para ILIKE ("contém", sem diferenciar maiúsculas), escapando % e _ para
    /// que o termo digitado seja tratado como texto, e não como curinga.
    /// </summary>
    public static string Contem(string termo) =>
        "%" + termo.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
}
