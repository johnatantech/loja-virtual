using Loja.Domain.Catalogo;
using Loja.Domain.Comum;
using Loja.Domain.Pedidos;

namespace Loja.Domain.Tests;

public class PedidoTests
{
    private static Produto Produto(decimal preco = 50m) =>
        new("Livro", "Descrição", preco, estoque: 10, categoriaId: 1);

    private static Pedido PedidoComItem()
    {
        var pedido = new Pedido("cliente-1");
        pedido.AdicionarItem(Produto(), 1);
        return pedido;
    }

    [Fact]
    public void Total_soma_os_itens()
    {
        var pedido = new Pedido("cliente-1");
        pedido.AdicionarItem(Produto(50m), 2);
        Assert.Equal(100m, pedido.Total);
    }

    [Fact]
    public void Mesmo_produto_soma_quantidade_em_vez_de_duplicar()
    {
        var pedido = new Pedido("cliente-1");
        var produto = Produto();
        pedido.AdicionarItem(produto, 1);
        pedido.AdicionarItem(produto, 2);
        Assert.Single(pedido.Itens);
        Assert.Equal(3, pedido.Itens.First().Quantidade);
    }

    [Fact]
    public void Item_guarda_o_preco_do_momento_da_compra()
    {
        var pedido = new Pedido("cliente-1");
        var produto = Produto(50m);
        pedido.AdicionarItem(produto, 1);
        produto.DefinirPreco(80m);
        Assert.Equal(50m, pedido.Total);
    }

    [Fact]
    public void Fluxo_completo_de_status()
    {
        var pedido = PedidoComItem();
        pedido.ConfirmarPagamento();
        pedido.Enviar();
        pedido.ConfirmarEntrega();
        Assert.Equal(StatusPedido.Entregue, pedido.Status);
    }

    [Fact]
    public void Nao_envia_pedido_sem_pagamento()
    {
        Assert.Throws<DomainException>(() => PedidoComItem().Enviar());
    }

    [Fact]
    public void Nao_paga_pedido_vazio()
    {
        Assert.Throws<DomainException>(() => new Pedido("cliente-1").ConfirmarPagamento());
    }

    [Fact]
    public void Nao_cancela_pedido_ja_enviado()
    {
        var pedido = PedidoComItem();
        pedido.ConfirmarPagamento();
        pedido.Enviar();
        Assert.Throws<DomainException>(() => pedido.Cancelar());
    }

    [Fact]
    public void Nao_altera_itens_depois_do_pagamento()
    {
        var pedido = PedidoComItem();
        pedido.ConfirmarPagamento();
        Assert.Throws<DomainException>(() => pedido.AdicionarItem(Produto(), 1));
    }

    [Fact]
    public void Nao_vende_produto_inativo()
    {
        var produto = Produto();
        produto.Desativar();
        Assert.Throws<DomainException>(() => new Pedido("cliente-1").AdicionarItem(produto, 1));
    }
}
