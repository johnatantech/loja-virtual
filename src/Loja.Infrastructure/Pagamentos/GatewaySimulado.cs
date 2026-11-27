using Loja.Application.Pedidos;
using Loja.Domain.Pedidos;

namespace Loja.Infrastructure.Pagamentos;

/// <summary>
/// Gateway de mentira para desenvolvimento, testes e demo: a "página de pagamento" é uma
/// tela da própria loja onde o cliente escolhe aprovar ou recusar.
/// </summary>
public class GatewaySimulado : IGatewayPagamento
{
    public bool EhSimulado => true;

    public Task<string> CriarCobrancaAsync(Pedido pedido, CancellationToken ct = default) =>
        Task.FromResult($"/pagamento-simulado/{pedido.Id}");

    public Task<PagamentoGateway?> ConsultarPagamentoAsync(string pagamentoId, CancellationToken ct = default) =>
        Task.FromResult<PagamentoGateway?>(null); // pagamentos simulados não chegam por webhook

    public static string IdPagamento(Guid pedidoId) => $"SIM-{pedidoId:N}";
}
