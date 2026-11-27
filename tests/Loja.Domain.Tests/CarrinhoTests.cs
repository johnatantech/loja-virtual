using Loja.Domain.Carrinhos;
using Loja.Domain.Comum;

namespace Loja.Domain.Tests;

public class CarrinhoTests
{
    [Fact]
    public void Define_altera_e_remove_quantidades()
    {
        var carrinho = new Carrinho("cliente-1");
        carrinho.DefinirQuantidade(produtoId: 7, quantidade: 2);
        carrinho.DefinirQuantidade(7, 5);
        Assert.Equal(5, carrinho.QuantidadeDe(7));

        carrinho.DefinirQuantidade(7, 0);
        Assert.Empty(carrinho.Itens);
    }

    [Fact]
    public void Respeita_o_limite_por_produto()
    {
        var carrinho = new Carrinho("cliente-1");
        Assert.Throws<DomainException>(() => carrinho.DefinirQuantidade(7, Carrinho.QuantidadeMaximaPorItem + 1));
    }

    [Fact]
    public void Mesclar_soma_as_quantidades_sem_passar_do_limite()
    {
        var carrinho = new Carrinho("cliente-1");
        carrinho.DefinirQuantidade(1, 8);
        carrinho.Mesclar([(1, 5), (2, 3)]);

        Assert.Equal(Carrinho.QuantidadeMaximaPorItem, carrinho.QuantidadeDe(1));
        Assert.Equal(3, carrinho.QuantidadeDe(2));
    }

    [Fact]
    public void Mesclar_ignora_quantidades_zeradas_ou_negativas()
    {
        var carrinho = new Carrinho("cliente-1");
        carrinho.Mesclar([(1, 0), (2, -4)]);
        Assert.Empty(carrinho.Itens);
    }
}
