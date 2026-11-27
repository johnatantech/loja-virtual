using System.Net;
using System.Net.Http.Json;
using Loja.Application.Catalogo;

namespace Loja.Api.Tests;

[Collection("Api")]
public class CatalogoApiTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_check_responde_com_banco_conectado()
    {
        var resposta = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Lista_as_categorias_do_catalogo_inicial()
    {
        var categorias = await _client.GetFromJsonAsync<List<CategoriaDto>>("/api/categorias");

        Assert.NotNull(categorias);
        Assert.Equal(4, categorias.Count);
        Assert.Contains(categorias, c => c.Slug == "eletronicos" && c.QuantidadeProdutos == 4);
        Assert.Equal(categorias.OrderBy(c => c.Nome).Select(c => c.Nome), categorias.Select(c => c.Nome));
    }
}

[Collection("Api")]
public class SaudeApiTests(ApiFactory factory)
{
    [Fact]
    public async Task Verificacao_de_vida_responde_sem_consultar_o_banco()
    {
        var resposta = await factory.CreateClient().GetAsync("/health/vivo");
        Assert.Equal(System.Net.HttpStatusCode.OK, resposta.StatusCode);
    }
}
