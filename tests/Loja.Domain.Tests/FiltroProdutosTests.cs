using Loja.Application.Catalogo;

namespace Loja.Domain.Tests;

public class FiltroProdutosTests
{
    [Fact]
    public void Filtro_padrao_e_valido()
    {
        Assert.Empty(new FiltroProdutos().Validar());
    }

    [Fact]
    public void Aponta_cada_campo_invalido()
    {
        var erros = new FiltroProdutos(PrecoMin: 50, PrecoMax: 10, Ordenacao: "x", Pagina: 0, TamanhoPagina: 100).Validar();
        Assert.Equal(["ordenacao", "pagina", "precoMax", "tamanhoPagina"], erros.Keys.Order(StringComparer.Ordinal));
    }
}
