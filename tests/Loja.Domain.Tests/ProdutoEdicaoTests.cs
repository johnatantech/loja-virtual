using Loja.Domain.Catalogo;
using Loja.Domain.Comum;

namespace Loja.Domain.Tests;

public class ProdutoEdicaoTests
{
    private static Produto Novo() => new("Café Especial", "Torra média", 39.90m, 10, categoriaId: 1);

    [Fact]
    public void Atualizar_troca_os_dados_e_o_slug()
    {
        var produto = Novo();
        produto.Atualizar("Café Gourmet Moído", "Torra escura", 2, null);
        Assert.Equal("cafe-gourmet-moido", produto.Slug);
        Assert.Equal(2, produto.CategoriaId);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("http://site.com/foto.jpg")]
    [InlineData("nao-e-url")]
    public void Imagem_precisa_ser_https(string url)
    {
        Assert.Throws<DomainException>(() => Novo().Atualizar("Café", "", 1, url));
    }

    [Fact]
    public void Aceita_imagem_https_e_imagem_vazia()
    {
        var produto = Novo();
        produto.Atualizar("Café", "", 1, "https://exemplo.com/cafe.jpg");
        Assert.Equal("https://exemplo.com/cafe.jpg", produto.ImagemUrl);
        produto.Atualizar("Café", "", 1, "  ");
        Assert.Null(produto.ImagemUrl);
    }
}
