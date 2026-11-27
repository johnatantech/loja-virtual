namespace Loja.Application.Pedidos;

public interface IPedidoServico
{
    Task<CotacaoFreteDto> CotarFreteAsync(string clienteId, string cep, CancellationToken ct = default);

    /// <summary>Transforma o carrinho em pedido: baixa o estoque, grava a entrega e gera a cobrança.</summary>
    Task<PedidoCriadoDto> CriarAsync(string clienteId, EnderecoDto endereco, CancellationToken ct = default);

    Task<IReadOnlyList<PedidoResumoDto>> ListarAsync(string clienteId, CancellationToken ct = default);
    Task<PedidoDetalheDto?> ObterAsync(string clienteId, Guid pedidoId, CancellationToken ct = default);

    /// <summary>Cancela um pedido ainda não pago e devolve os itens ao estoque.</summary>
    Task<PedidoDetalheDto?> CancelarAsync(string clienteId, Guid pedidoId, CancellationToken ct = default);

    /// <summary>Gera de novo o link de pagamento (por exemplo, se o gateway estava fora do ar).</summary>
    Task<PedidoCriadoDto?> GerarPagamentoAsync(string clienteId, Guid pedidoId, CancellationToken ct = default);

    /// <summary>Consulta o pagamento no gateway e confirma o pedido, se aprovado. Idempotente.</summary>
    Task<bool> ProcessarPagamentoAsync(string pagamentoId, CancellationToken ct = default);

    /// <summary>Só no gateway simulado: aprova ou recusa o pagamento de um pedido do próprio cliente.</summary>
    Task<PedidoDetalheDto?> SimularPagamentoAsync(string clienteId, Guid pedidoId, bool aprovar, CancellationToken ct = default);
}
