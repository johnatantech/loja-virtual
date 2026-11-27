using Loja.Application.Catalogo;
using Loja.Application.Pedidos;

namespace Loja.Application.Admin;

/// <summary>Operações do painel administrativo. Toda rota que usa este serviço exige o perfil Admin.</summary>
public interface IAdminServico
{
    Task<DashboardDto> DashboardAsync(CancellationToken ct = default);

    Task<Pagina<ProdutoAdminDto>> ListarProdutosAsync(string? busca, int pagina, CancellationToken ct = default);
    Task<ProdutoAdminDto?> ObterProdutoAsync(int id, CancellationToken ct = default);
    Task<ProdutoAdminDto> CriarProdutoAsync(SalvarProdutoDto dados, CancellationToken ct = default);
    Task<ProdutoAdminDto?> AtualizarProdutoAsync(int id, SalvarProdutoDto dados, CancellationToken ct = default);
    Task<ProdutoAdminDto?> ReporEstoqueAsync(int id, int quantidade, CancellationToken ct = default);
    Task<ProdutoAdminDto?> DefinirAtivoAsync(int id, bool ativo, CancellationToken ct = default);

    Task<Pagina<PedidoAdminResumoDto>> ListarPedidosAsync(string? status, int pagina, CancellationToken ct = default);
    Task<PedidoDetalheDto?> ObterPedidoAsync(Guid id, CancellationToken ct = default);
    Task<PedidoDetalheDto?> AvancarPedidoAsync(Guid id, string acao, CancellationToken ct = default);

    Task<Pagina<ClienteAdminDto>> ListarClientesAsync(string? busca, int pagina, CancellationToken ct = default);
    Task<bool> DesbloquearClienteAsync(string id, CancellationToken ct = default);
}
