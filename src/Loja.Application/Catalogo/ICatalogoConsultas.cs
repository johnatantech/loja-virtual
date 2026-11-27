namespace Loja.Application.Catalogo;

/// <summary>
/// Consultas de leitura do catálogo. A Application define o contrato e a Infrastructure
/// implementa com EF Core, então as regras não dependem do banco escolhido.
/// </summary>
public interface ICatalogoConsultas
{
    Task<IReadOnlyList<CategoriaDto>> ListarCategoriasAsync(CancellationToken ct = default);
    Task<Pagina<ProdutoResumoDto>> ListarProdutosAsync(FiltroProdutos filtro, CancellationToken ct = default);
    Task<ProdutoDetalheDto?> ObterProdutoAsync(string slug, CancellationToken ct = default);
}
