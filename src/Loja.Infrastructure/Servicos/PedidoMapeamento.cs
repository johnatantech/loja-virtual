using Loja.Application.Pedidos;
using Loja.Domain.Pedidos;

namespace Loja.Infrastructure.Servicos;

internal static class PedidoMapeamento
{
    public static PedidoDetalheDto ParaDetalhe(Pedido p) => new(
        p.Id, p.CriadoEm, p.Status.ToString(),
        p.Itens.Select(i => new ItemPedidoDto(i.ProdutoId, i.NomeProduto, i.PrecoUnitario, i.Quantidade, i.Subtotal)).ToList(),
        p.Endereco is null ? null : new EnderecoDto(
            p.Endereco.Destinatario, p.Endereco.Cep, p.Endereco.Logradouro, p.Endereco.Numero,
            p.Endereco.Complemento, p.Endereco.Bairro, p.Endereco.Cidade, p.Endereco.Uf),
        p.Subtotal, p.ValorFrete, p.Total, p.PrazoEntregaDias,
        p.Status == StatusPedido.AguardandoPagamento ? p.LinkPagamento : null,
        p.PagoEm);
}
