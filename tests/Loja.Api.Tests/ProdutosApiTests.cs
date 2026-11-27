using System.Net;
using System.Net.Http.Json;
using Loja.Application.Catalogo;

namespace Loja.Api.Tests;

[Collection("Api")]
public class ProdutosApiTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<Pagina<ProdutoResumoDto>> Listar(string query = "")
    {
        var pagina = await _client.GetFromJsonAsync<Pagina<ProdutoResumoDto>>($"/api/produtos{query}");
        Assert.NotNull(pagina);
        return pagina;
    }

    [Fact]
    public async Task Sem_filtros_retorna_a_primeira_pagina_ordenada_por_nome()
    {
        var pagina = await Listar();
        Assert.Equal(12, pagina.Total);
        Assert.Equal(1, pagina.NumeroPagina);
        Assert.Equal(pagina.Itens.OrderBy(p => p.Nome).Select(p => p.Nome), pagina.Itens.Select(p => p.Nome));
    }

    [Fact]
    public async Task Filtra_por_categoria()
    {
        var pagina = await Listar("?categoria=livros");
        Assert.Equal(3, pagina.Total);
        Assert.All(pagina.Itens, p => Assert.Equal("livros", p.CategoriaSlug));
    }

    [Fact]
    public async Task Busca_ignora_maiusculas_e_procura_tambem_na_descricao()
    {
        var porNome = await Listar("?busca=MOUSE");
        Assert.Single(porNome.Itens);

        var porDescricao = await Listar("?busca=jupyter");
        Assert.Equal("python-para-analise-de-dados", Assert.Single(porDescricao.Itens).Slug);
    }

    [Fact]
    public async Task Filtra_por_faixa_de_preco()
    {
        var pagina = await Listar("?precoMin=100&precoMax=400");
        Assert.NotEmpty(pagina.Itens);
        Assert.All(pagina.Itens, p => Assert.InRange(p.Preco, 100m, 400m));
    }

    [Fact]
    public async Task Ordena_por_menor_e_maior_preco()
    {
        var menor = await Listar("?ordenacao=menor-preco&tamanhoPagina=48");
        Assert.Equal(menor.Itens.OrderBy(p => p.Preco).Select(p => p.Preco), menor.Itens.Select(p => p.Preco));

        var maior = await Listar("?ordenacao=maior-preco&tamanhoPagina=1");
        Assert.Equal("monitor-27-polegadas", Assert.Single(maior.Itens).Slug);
    }

    [Fact]
    public async Task Pagina_os_resultados_sem_repetir_itens()
    {
        var primeira = await Listar("?tamanhoPagina=5&pagina=1");
        var terceira = await Listar("?tamanhoPagina=5&pagina=3");

        Assert.Equal(3, primeira.TotalPaginas);
        Assert.Equal(5, primeira.Itens.Count);
        Assert.Equal(2, terceira.Itens.Count);
        Assert.Empty(primeira.Itens.Select(p => p.Id).Intersect(terceira.Itens.Select(p => p.Id)));
    }

    [Theory]
    [InlineData("?pagina=0")]
    [InlineData("?tamanhoPagina=500")]
    [InlineData("?precoMin=500&precoMax=100")]
    [InlineData("?ordenacao=aleatoria")]
    public async Task Filtros_invalidos_retornam_400(string query)
    {
        var resposta = await _client.GetAsync($"/api/produtos{query}");
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Detalhe_do_produto_pelo_slug()
    {
        var produto = await _client.GetFromJsonAsync<ProdutoDetalheDto>("/api/produtos/teclado-mecanico");
        Assert.NotNull(produto);
        Assert.Equal("Teclado Mecânico", produto.Nome);
        Assert.Equal(12, produto.Estoque);
    }

    [Fact]
    public async Task Produto_inexistente_retorna_404()
    {
        var resposta = await _client.GetAsync("/api/produtos/nao-existe");
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
