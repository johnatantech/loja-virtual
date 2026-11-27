using Loja.Domain.Pedidos;

namespace Loja.Application.Pedidos;

public enum SituacaoPagamento { Pendente, Aprovado, Recusado }

/// <summary>Pagamento como o gateway informa: a fonte da verdade, nunca o navegador do cliente.</summary>
public record PagamentoGateway(string Id, SituacaoPagamento Situacao, Guid? PedidoId, decimal Valor);

/// <summary>
/// Contrato do provedor de pagamento. Trocar Mercado Pago por outro gateway (ou pelo
/// simulador de desenvolvimento) não exige mudar nada no fluxo do pedido.
/// </summary>
public interface IGatewayPagamento
{
    bool EhSimulado { get; }

    /// <summary>Cria a cobrança e devolve o link onde o cliente paga.</summary>
    Task<string> CriarCobrancaAsync(Pedido pedido, CancellationToken ct = default);

    /// <summary>Consulta um pagamento direto no gateway. Null se não existir.</summary>
    Task<PagamentoGateway?> ConsultarPagamentoAsync(string pagamentoId, CancellationToken ct = default);
}
