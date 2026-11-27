using System.Net;
using System.Net.Http.Json;
using Loja.Application.Carrinhos;
using Loja.Application.Catalogo;
using Loja.Application.Pedidos;

namespace Loja.Api.Tests;

[Collection("Api")]
public class PedidosApiTests(ApiFactory factory)
{
    private static readonly object Endereco = new
    {
        destinatario = "Maria Souza", cep = "01310-100", logradouro = "Av. Paulista", numero = "1000",
        complemento = "Ap. 12", bairro = "Bela Vista", cidade = "São Paulo", uf = "AM", // UF errada de propósito
    };

    private async Task<ProdutoDetalheDto> Produto(string slug) =>
        (await factory.CreateClient().GetFromJsonAsync<ProdutoDetalheDto>($"/api/produtos/{slug}"))!;

    private static async Task<PedidoCriadoDto> FecharPedido(HttpClient cliente)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/pedidos", Endereco);
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return (await resposta.Content.ReadFromJsonAsync<PedidoCriadoDto>())!;
    }

    [Fact]
    public async Task Frete_usa_o_cep_e_o_carrinho_atual()
    {
        var (cliente, _) = await factory.CriarContaAsync();
        var livro = await Produto("codigo-limpo");
        await cliente.PutAsJsonAsync($"/api/carrinho/itens/{livro.Id}", new { quantidade = 1 });

        var cotacao = await cliente.GetFromJsonAsync<CotacaoFreteDto>("/api/checkout/frete?cep=01310100");
        Assert.Equal("SP", cotacao!.Uf);
        Assert.Equal(livro.Preco, cotacao.Subtotal);
        Assert.Equal(cotacao.Subtotal + cotacao.ValorFrete, cotacao.Total);
    }

    [Fact]
    public async Task Fechar_pedido_baixa_estoque_esvazia_carrinho_e_gera_pagamento()
    {
        var (cliente, _) = await factory.CriarContaAsync();
        var garrafa = await Produto("garrafa-termica-1-litro");
        await cliente.PutAsJsonAsync($"/api/carrinho/itens/{garrafa.Id}", new { quantidade = 2 });

        var criado = await FecharPedido(cliente);

        Assert.Equal($"/pagamento-simulado/{criado.Id}", criado.LinkPagamento);
        Assert.Equal(garrafa.Estoque - 2, (await Produto("garrafa-termica-1-litro")).Estoque);
        Assert.Empty((await cliente.GetFromJsonAsync<CarrinhoDto>("/api/carrinho"))!.Itens);

        var pedido = await cliente.GetFromJsonAsync<PedidoDetalheDto>($"/api/pedidos/{criado.Id}");
        Assert.Equal("AguardandoPagamento", pedido!.Status);
        Assert.Equal("SP", pedido.Endereco!.Uf); // a UF vem do CEP, não do formulário
        Assert.Equal(pedido.Subtotal + pedido.ValorFrete, pedido.Total);
    }

    [Fact]
    public async Task Carrinho_vazio_nao_vira_pedido()
    {
        var (cliente, _) = await factory.CriarContaAsync();
        var resposta = await cliente.PostAsJsonAsync("/api/pedidos", Endereco);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Pagamento_simulado_aprovado_confirma_o_pedido_uma_unica_vez()
    {
        var (cliente, _) = await factory.CriarContaAsync();
        var livro = await Produto("arquitetura-limpa");
        await cliente.PutAsJsonAsync($"/api/carrinho/itens/{livro.Id}", new { quantidade = 1 });
        var criado = await FecharPedido(cliente);

        await cliente.PostAsJsonAsync($"/api/pagamentos/simulado/{criado.Id}", new { aprovar = true });
        var repetido = await cliente.PostAsJsonAsync($"/api/pagamentos/simulado/{criado.Id}", new { aprovar = true });

        var pedido = await repetido.Content.ReadFromJsonAsync<PedidoDetalheDto>();
        Assert.Equal("Pago", pedido!.Status);
        Assert.NotNull(pedido.PagoEm);
        Assert.Null(pedido.LinkPagamento); // pedido pago não oferece mais link de pagamento
    }

    [Fact]
    public async Task Pagamento_recusado_mantem_o_pedido_aguardando()
    {
        var (cliente, _) = await factory.CriarContaAsync();
        var livro = await Produto("arquitetura-limpa");
        await cliente.PutAsJsonAsync($"/api/carrinho/itens/{livro.Id}", new { quantidade = 1 });
        var criado = await FecharPedido(cliente);

        var resposta = await cliente.PostAsJsonAsync($"/api/pagamentos/simulado/{criado.Id}", new { aprovar = false });
        Assert.Equal("AguardandoPagamento", (await resposta.Content.ReadFromJsonAsync<PedidoDetalheDto>())!.Status);
    }

    [Fact]
    public async Task Cancelar_devolve_os_itens_ao_estoque()
    {
        var (cliente, _) = await factory.CriarContaAsync();
        var antes = await Produto("tapete-de-yoga");
        await cliente.PutAsJsonAsync($"/api/carrinho/itens/{antes.Id}", new { quantidade = 3 });
        var criado = await FecharPedido(cliente);

        var resposta = await cliente.PostAsync($"/api/pedidos/{criado.Id}/cancelar", null);

        Assert.Equal("Cancelado", (await resposta.Content.ReadFromJsonAsync<PedidoDetalheDto>())!.Status);
        Assert.Equal(antes.Estoque, (await Produto("tapete-de-yoga")).Estoque);
    }

    [Fact]
    public async Task Pedido_pago_nao_pode_ser_cancelado_pelo_cliente()
    {
        var (cliente, _) = await factory.CriarContaAsync();
        var livro = await Produto("codigo-limpo");
        await cliente.PutAsJsonAsync($"/api/carrinho/itens/{livro.Id}", new { quantidade = 1 });
        var criado = await FecharPedido(cliente);
        await cliente.PostAsJsonAsync($"/api/pagamentos/simulado/{criado.Id}", new { aprovar = true });

        var resposta = await cliente.PostAsync($"/api/pedidos/{criado.Id}/cancelar", null);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Cliente_nao_ve_pedido_de_outra_pessoa()
    {
        var (ana, _) = await factory.CriarContaAsync();
        var (bruno, _) = await factory.CriarContaAsync();
        var livro = await Produto("codigo-limpo");
        await ana.PutAsJsonAsync($"/api/carrinho/itens/{livro.Id}", new { quantidade = 1 });
        var criado = await FecharPedido(ana);

        Assert.Equal(HttpStatusCode.NotFound, (await bruno.GetAsync($"/api/pedidos/{criado.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bruno.PostAsync($"/api/pedidos/{criado.Id}/cancelar", null)).StatusCode);
        Assert.Empty((await bruno.GetFromJsonAsync<List<PedidoResumoDto>>("/api/pedidos"))!);
    }

    [Fact]
    public async Task Compras_simultaneas_nao_vendem_alem_do_estoque()
    {
        // O monitor tem 5 unidades. Seis clientes tentam comprar 1 cada, ao mesmo tempo.
        var monitor = await Produto("monitor-27-polegadas");
        var clientes = new List<HttpClient>();
        for (var i = 0; i < monitor.Estoque + 1; i++)
        {
            var (cliente, _) = await factory.CriarContaAsync();
            var adicionou = await cliente.PutAsJsonAsync($"/api/carrinho/itens/{monitor.Id}", new { quantidade = 1 });
            if (adicionou.IsSuccessStatusCode) clientes.Add(cliente);
        }

        var respostas = await Task.WhenAll(clientes.Select(c => c.PostAsJsonAsync("/api/pedidos", Endereco)));

        var vendidos = respostas.Count(r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(monitor.Estoque, vendidos);
        Assert.All(respostas.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
        Assert.Equal(0, (await Produto("monitor-27-polegadas")).Estoque);
    }
}
