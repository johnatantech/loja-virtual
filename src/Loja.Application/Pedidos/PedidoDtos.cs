namespace Loja.Application.Pedidos;

public record EnderecoDto(
    string Destinatario, string Cep, string Logradouro, string Numero,
    string? Complemento, string Bairro, string Cidade, string? Uf = null);

public record CotacaoFreteDto(string Uf, decimal Subtotal, decimal ValorFrete, int PrazoDias, bool FreteGratis, decimal Total);

public record PedidoCriadoDto(Guid Id, string? LinkPagamento);

public record ItemPedidoDto(int ProdutoId, string Nome, decimal PrecoUnitario, int Quantidade, decimal Subtotal);

public record PedidoResumoDto(Guid Id, DateTime CriadoEm, string Status, int QuantidadeItens, decimal Total);

public record PedidoDetalheDto(
    Guid Id, DateTime CriadoEm, string Status, IReadOnlyList<ItemPedidoDto> Itens, EnderecoDto? Endereco,
    decimal Subtotal, decimal ValorFrete, decimal Total, int PrazoEntregaDias, string? LinkPagamento, DateTime? PagoEm);
