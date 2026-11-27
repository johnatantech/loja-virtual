namespace Loja.Application.Catalogo;

/// <summary>
/// Consultas de leitura do catálogo. A Application define o contrato e a Infrastructure
/// implementa com EF Core, então as regras não dependem do banco escolhido.
/// </summary>
public interface ICatalogoConsultas
{
    Task<IReadOnlyList<CategoriaDto>> ListarCategoriasAsync(CancellationToken ct = default);
}
