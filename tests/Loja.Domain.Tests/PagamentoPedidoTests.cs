using Loja.Domain.Catalogo;
using Loja.Domain.Comum;
using Loja.Domain.Entrega;
using Loja.Domain.Pedidos;

namespace Loja.Domain.Tests;

public class PagamentoPedidoTests
{
    private static Pedido PedidoComFrete()
    {
        var pedido = new Pedido("cliente-1");
        pedido.AdicionarItem(new Produto("Livro", "", 100m, 10, 1), 1);
        var cep = Cep.Criar("20040-002"); // RJ: frete pago
        pedido.DefinirEntrega(
            EnderecoEntrega.Criar("Maria", cep, "Rua A", "1", null, "Centro", "Rio de Janeiro"),
            CalculadoraFrete.Cotar(cep, pedido.Subtotal));
        return pedido;
    }

    [Fact]
    public void Total_inclui_o_frete()
    {
        var pedido = PedidoComFrete();
        Assert.Equal(pedido.Subtotal + pedido.ValorFrete, pedido.Total);
        Assert.True(pedido.ValorFrete > 0);
    }

    [Fact]
    public void Pagamento_aprovado_confirma_o_pedido()
    {
        var pedido = PedidoComFrete();
        Assert.True(pedido.RegistrarPagamentoAprovado("pg-1", pedido.Total, DateTime.UtcNow));
        Assert.Equal(StatusPedido.Pago, pedido.Status);
        Assert.Equal("pg-1", pedido.PagamentoExternoId);
    }

    [Fact]
    public void Mesma_notificacao_duas_vezes_nao_tem_efeito()
    {
        var pedido = PedidoComFrete();
        pedido.RegistrarPagamentoAprovado("pg-1", pedido.Total, DateTime.UtcNow);
        Assert.False(pedido.RegistrarPagamentoAprovado("pg-1", pedido.Total, DateTime.UtcNow));
        Assert.Equal(StatusPedido.Pago, pedido.Status);
    }

    [Fact]
    public void Recusa_pagamento_com_valor_diferente_do_total()
    {
        var pedido = PedidoComFrete();
        Assert.Throws<DomainException>(() => pedido.RegistrarPagamentoAprovado("pg-1", 1m, DateTime.UtcNow));
        Assert.Equal(StatusPedido.AguardandoPagamento, pedido.Status);
    }

    [Fact]
    public void Nao_aceita_pagamento_de_pedido_cancelado()
    {
        var pedido = PedidoComFrete();
        pedido.Cancelar();
        Assert.Throws<DomainException>(() => pedido.RegistrarPagamentoAprovado("pg-1", pedido.Total, DateTime.UtcNow));
    }
}
