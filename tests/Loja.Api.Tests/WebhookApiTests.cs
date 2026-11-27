using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Loja.Application.Catalogo;
using Loja.Application.Pedidos;
using Loja.Domain.Pedidos;
using Loja.Infrastructure.Pagamentos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Loja.Api.Tests;

/// <summary>Gateway falso: devolve os pagamentos que o teste cadastrar, como se viessem do Mercado Pago.</summary>
public class GatewayFalso : IGatewayPagamento
{
    public ConcurrentDictionary<string, PagamentoGateway> Pagamentos { get; } = new();
    public bool EhSimulado => false;
    public Task<string> CriarCobrancaAsync(Pedido pedido, CancellationToken ct = default) =>
        Task.FromResult($"https://pagamento.exemplo/{pedido.Id}");
    public Task<PagamentoGateway?> ConsultarPagamentoAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(Pagamentos.TryGetValue(id, out var p) ? p : null);
}

[Collection("Api")]
public class WebhookApiTests(ApiFactory factory)
{
    private const string Segredo = "segredo-de-teste-do-webhook";
    private readonly GatewayFalso _gateway = new();

    private WebApplicationFactory<Program> ComGatewayFalso() => factory.WithWebHostBuilder(b =>
    {
        b.UseSetting("MercadoPago:SegredoWebhook", Segredo);
        b.ConfigureTestServices(s => s.AddSingleton<IGatewayPagamento>(_gateway));
    });

    private static HttpRequestMessage Notificacao(string pagamentoId, string? assinatura = null)
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var requestId = Guid.NewGuid().ToString();
        var v1 = assinatura ?? AssinaturaWebhookMercadoPago.Calcular(ts, requestId, pagamentoId, Segredo);

        var requisicao = new HttpRequestMessage(HttpMethod.Post,
            $"/api/pagamentos/webhook/mercadopago?data.id={pagamentoId}&type=payment");
        requisicao.Headers.Add("x-signature", $"ts={ts},v1={v1}");
        requisicao.Headers.Add("x-request-id", requestId);
        requisicao.Content = JsonContent.Create(new { type = "payment", data = new { id = pagamentoId } });
        return requisicao;
    }

    private static async Task<(HttpClient Cliente, PedidoCriadoDto Pedido, PedidoDetalheDto Detalhe)> PedidoAguardando(
        WebApplicationFactory<Program> app)
    {
        var (cliente, _) = await app.CriarContaAsync();
        var livro = await cliente.GetFromJsonAsync<ProdutoDetalheDto>("/api/produtos/codigo-limpo");
        await cliente.PutAsJsonAsync($"/api/carrinho/itens/{livro!.Id}", new { quantidade = 1 });
        var resposta = await cliente.PostAsJsonAsync("/api/pedidos", new
        {
            destinatario = "Maria", cep = "20040-002", logradouro = "Rua A", numero = "1",
            bairro = "Centro", cidade = "Rio de Janeiro",
        });
        var criado = (await resposta.Content.ReadFromJsonAsync<PedidoCriadoDto>())!;
        var detalhe = (await cliente.GetFromJsonAsync<PedidoDetalheDto>($"/api/pedidos/{criado.Id}"))!;
        return (cliente, criado, detalhe);
    }

    [Fact]
    public async Task Notificacao_valida_confirma_o_pedido_e_repeticoes_nao_tem_efeito()
    {
        var app = ComGatewayFalso();
        var (cliente, pedido, detalhe) = await PedidoAguardando(app);
        Assert.StartsWith("https://pagamento.exemplo/", pedido.LinkPagamento);

        var pagamentoId = Random.Shared.NextInt64(1, long.MaxValue).ToString();
        _gateway.Pagamentos[pagamentoId] = new PagamentoGateway(pagamentoId, SituacaoPagamento.Aprovado, pedido.Id, detalhe.Total);

        var webhook = app.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await webhook.SendAsync(Notificacao(pagamentoId))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await webhook.SendAsync(Notificacao(pagamentoId))).StatusCode); // reenvio

        var final = await cliente.GetFromJsonAsync<PedidoDetalheDto>($"/api/pedidos/{pedido.Id}");
        Assert.Equal("Pago", final!.Status);
    }

    [Fact]
    public async Task Notificacao_com_assinatura_invalida_e_recusada()
    {
        var app = ComGatewayFalso();
        var (cliente, pedido, detalhe) = await PedidoAguardando(app);
        var pagamentoId = Random.Shared.NextInt64(1, long.MaxValue).ToString();
        _gateway.Pagamentos[pagamentoId] = new PagamentoGateway(pagamentoId, SituacaoPagamento.Aprovado, pedido.Id, detalhe.Total);

        var resposta = await app.CreateClient().SendAsync(Notificacao(pagamentoId, assinatura: new string('0', 64)));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        var final = await cliente.GetFromJsonAsync<PedidoDetalheDto>($"/api/pedidos/{pedido.Id}");
        Assert.Equal("AguardandoPagamento", final!.Status);
    }

    [Fact]
    public async Task Pagamento_com_valor_adulterado_nao_confirma_o_pedido()
    {
        var app = ComGatewayFalso();
        var (cliente, pedido, _) = await PedidoAguardando(app);
        var pagamentoId = Random.Shared.NextInt64(1, long.MaxValue).ToString();
        _gateway.Pagamentos[pagamentoId] = new PagamentoGateway(pagamentoId, SituacaoPagamento.Aprovado, pedido.Id, 0.01m);

        await app.CreateClient().SendAsync(Notificacao(pagamentoId));

        var final = await cliente.GetFromJsonAsync<PedidoDetalheDto>($"/api/pedidos/{pedido.Id}");
        Assert.Equal("AguardandoPagamento", final!.Status);
    }

    [Fact]
    public void Assinatura_confere_com_o_exemplo_de_calculo()
    {
        var v1 = AssinaturaWebhookMercadoPago.Calcular("1704908010", "req-1", "123456", "segredo");
        Assert.True(AssinaturaWebhookMercadoPago.Valida($"ts=1704908010,v1={v1}", "req-1", "123456", "segredo"));
        Assert.False(AssinaturaWebhookMercadoPago.Valida($"ts=1704908010,v1={v1}", "req-1", "999999", "segredo"));
        Assert.False(AssinaturaWebhookMercadoPago.Valida($"ts=1704908010,v1={v1}", "req-1", "123456", "outro-segredo"));
        Assert.False(AssinaturaWebhookMercadoPago.Valida(null, "req-1", "123456", "segredo"));
    }
}
