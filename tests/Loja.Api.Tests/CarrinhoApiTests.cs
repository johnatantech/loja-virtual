using System.Net;
using System.Net.Http.Json;
using Loja.Application.Carrinhos;
using Loja.Application.Catalogo;

namespace Loja.Api.Tests;

[Collection("Api")]
public class CarrinhoApiTests(ApiFactory factory)
{
    private async Task<ProdutoDetalheDto> Produto(string slug) =>
        (await factory.CreateClient().GetFromJsonAsync<ProdutoDetalheDto>($"/api/produtos/{slug}"))!;

    [Fact]
    public async Task Carrinho_exige_login()
    {
        var resposta = await factory.CreateClient().GetAsync("/api/carrinho");
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Cotacao_de_visitante_usa_precos_atuais_e_agrupa_itens()
    {
        var mouse = await Produto("mouse-sem-fio");
        var resposta = await factory.CreateClient().PostAsJsonAsync("/api/carrinho/cotacao",
            new { itens = new[] { new { produtoId = mouse.Id, quantidade = 1 }, new { produtoId = mouse.Id, quantidade = 2 } } });

        var carrinho = (await resposta.Content.ReadFromJsonAsync<CarrinhoDto>())!;
        var item = Assert.Single(carrinho.Itens);
        Assert.Equal(3, item.Quantidade);
        Assert.Equal(mouse.Preco * 3, carrinho.Total);
        Assert.True(carrinho.PodeFinalizar);
    }

    [Fact]
    public async Task Adiciona_altera_e_remove_itens()
    {
        var (cliente, _) = await factory.CriarContaAsync();
        var livro = await Produto("codigo-limpo");

        var adicionado = await (await cliente.PutAsJsonAsync($"/api/carrinho/itens/{livro.Id}", new { quantidade = 2 }))
            .Content.ReadFromJsonAsync<CarrinhoDto>();
        Assert.Equal(livro.Preco * 2, adicionado!.Total);

        await cliente.PutAsJsonAsync($"/api/carrinho/itens/{livro.Id}", new { quantidade = 4 });
        var atual = await cliente.GetFromJsonAsync<CarrinhoDto>("/api/carrinho");
        Assert.Equal(4, Assert.Single(atual!.Itens).Quantidade);

        await cliente.DeleteAsync($"/api/carrinho/itens/{livro.Id}");
        Assert.Empty((await cliente.GetFromJsonAsync<CarrinhoDto>("/api/carrinho"))!.Itens);
    }

    [Fact]
    public async Task Nao_permite_mais_unidades_do_que_o_estoque()
    {
        var (cliente, _) = await factory.CriarContaAsync();
        var bicicleta = await Produto("bicicleta-aro-29"); // estoque de 3

        var resposta = await cliente.PutAsJsonAsync($"/api/carrinho/itens/{bicicleta.Id}", new { quantidade = 4 });
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("Só restam 3", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Mesclar_junta_o_carrinho_de_visitante_ao_da_conta()
    {
        var (cliente, _) = await factory.CriarContaAsync();
        var livro = await Produto("codigo-limpo");
        var tapete = await Produto("tapete-de-yoga");
        await cliente.PutAsJsonAsync($"/api/carrinho/itens/{livro.Id}", new { quantidade = 1 });

        var resposta = await cliente.PostAsJsonAsync("/api/carrinho/mesclar", new
        {
            itens = new[]
            {
                new { produtoId = livro.Id, quantidade = 2 },
                new { produtoId = tapete.Id, quantidade = 1 },
                new { produtoId = 999_999, quantidade = 1 }, // produto inexistente é ignorado
            },
        });

        var carrinho = (await resposta.Content.ReadFromJsonAsync<CarrinhoDto>())!;
        Assert.Equal(2, carrinho.Itens.Count);
        Assert.Equal(3, carrinho.Itens.Single(i => i.ProdutoId == livro.Id).Quantidade);
    }

    [Fact]
    public async Task Carrinhos_de_clientes_diferentes_sao_independentes()
    {
        var (ana, _) = await factory.CriarContaAsync();
        var (bruno, _) = await factory.CriarContaAsync();
        var mouse = await Produto("mouse-sem-fio");

        await ana.PutAsJsonAsync($"/api/carrinho/itens/{mouse.Id}", new { quantidade = 1 });
        Assert.Empty((await bruno.GetFromJsonAsync<CarrinhoDto>("/api/carrinho"))!.Itens);
    }
}
