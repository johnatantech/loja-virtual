namespace Loja.Application.Catalogo;

/// <summary>Filtros da vitrine. Todos opcionais; a validação fica em Validar().</summary>
public record FiltroProdutos(
    string? Busca = null,
    string? Categoria = null,
    decimal? PrecoMin = null,
    decimal? PrecoMax = null,
    string? Ordenacao = null,
    int Pagina = 1,
    int TamanhoPagina = 12)
{
    public const int TamanhoMaximo = 48;
    public static readonly string[] OrdenacoesValidas = ["nome", "menor-preco", "maior-preco"];

    /// <summary>Retorna os erros por campo, no formato de ValidationProblem.</summary>
    public Dictionary<string, string[]> Validar()
    {
        var erros = new Dictionary<string, string[]>();
        if (Pagina < 1)
            erros["pagina"] = ["A página deve ser 1 ou maior."];
        if (TamanhoPagina is < 1 or > TamanhoMaximo)
            erros["tamanhoPagina"] = [$"O tamanho da página deve estar entre 1 e {TamanhoMaximo}."];
        if (PrecoMin < 0)
            erros["precoMin"] = ["O preço mínimo não pode ser negativo."];
        if (PrecoMin is not null && PrecoMax is not null && PrecoMin > PrecoMax)
            erros["precoMax"] = ["O preço máximo deve ser maior ou igual ao mínimo."];
        if (Ordenacao is not null && !OrdenacoesValidas.Contains(Ordenacao))
            erros["ordenacao"] = [$"Use uma destas opções: {string.Join(", ", OrdenacoesValidas)}."];
        return erros;
    }
}
