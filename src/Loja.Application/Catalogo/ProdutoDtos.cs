namespace Loja.Application.Catalogo;

public record ProdutoResumoDto(
    int Id, string Nome, string Slug, decimal Preco, string? ImagemUrl,
    string Categoria, string CategoriaSlug, bool EmEstoque);

public record ProdutoDetalheDto(
    int Id, string Nome, string Slug, string Descricao, decimal Preco, string? ImagemUrl,
    string Categoria, string CategoriaSlug, int Estoque);

public record Pagina<T>(IReadOnlyList<T> Itens, int NumeroPagina, int TamanhoPagina, int Total)
{
    public int TotalPaginas => Total == 0 ? 0 : (int)Math.Ceiling(Total / (double)TamanhoPagina);
}
