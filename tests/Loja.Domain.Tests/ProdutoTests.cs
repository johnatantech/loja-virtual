using Loja.Domain.Catalogo;
using Loja.Domain.Comum;

namespace Loja.Domain.Tests;

public class ProdutoTests
{
    private static Produto NovoProduto(int estoque = 10) =>
        new("Café Especial", "Torra média", 39.90m, estoque, categoriaId: 1);

    [Fact]
    public void Gera_slug_sem_acentos()
    {
        Assert.Equal("cafe-especial", NovoProduto().Slug);
    }

    [Fact]
    public void Baixar_estoque_reduz_a_quantidade()
    {
        var produto = NovoProduto(estoque: 10);
        produto.BaixarEstoque(3);
        Assert.Equal(7, produto.Estoque);
    }

    [Fact]
    public void Nao_vende_mais_do_que_o_estoque()
    {
        var produto = NovoProduto(estoque: 2);
        var erro = Assert.Throws<DomainException>(() => produto.BaixarEstoque(3));
        Assert.Contains("Estoque insuficiente", erro.Message);
        Assert.Equal(2, produto.Estoque); // nada foi alterado
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Preco_precisa_ser_positivo(decimal preco)
    {
        Assert.Throws<DomainException>(() => NovoProduto().DefinirPreco(preco));
    }

    [Fact]
    public void Preco_e_arredondado_para_centavos()
    {
        var produto = NovoProduto();
        produto.DefinirPreco(10.555m);
        Assert.Equal(10.56m, produto.Preco);
    }
}
