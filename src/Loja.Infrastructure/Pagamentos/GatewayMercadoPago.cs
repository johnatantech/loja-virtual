using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Loja.Application.Pedidos;
using Loja.Domain.Pedidos;
using Microsoft.Extensions.Options;

namespace Loja.Infrastructure.Pagamentos;

/// <summary>
/// Integração com o Checkout Pro do Mercado Pago pela API REST: cria uma "preferência" de
/// pagamento e consulta pagamentos. O cliente paga na página do Mercado Pago, então os dados
/// de cartão nunca passam pela loja.
/// </summary>
public class GatewayMercadoPago(HttpClient http, IOptions<MercadoPagoOpcoes> opcoes) : IGatewayPagamento
{
    private readonly MercadoPagoOpcoes _opcoes = opcoes.Value;

    public bool EhSimulado => false;

    public async Task<string> CriarCobrancaAsync(Pedido pedido, CancellationToken ct = default)
    {
        var itens = pedido.Itens
            .Select(i => new ItemPreferencia(i.ProdutoId.ToString(), i.NomeProduto, i.Quantidade, i.PrecoUnitario))
            .ToList();
        if (pedido.ValorFrete > 0) // frete como item, para o total pago bater com o total do pedido
            itens.Add(new ItemPreferencia("frete", "Frete", 1, pedido.ValorFrete));

        var urlRetorno = $"{_opcoes.UrlLoja.TrimEnd('/')}/pedidos/{pedido.Id}";
        var preferencia = new Preferencia(
            itens,
            pedido.Id.ToString(),
            new UrlsRetorno(urlRetorno, urlRetorno, urlRetorno),
            "approved",
            string.IsNullOrWhiteSpace(_opcoes.UrlPublicaApi)
                ? null
                : $"{_opcoes.UrlPublicaApi.TrimEnd('/')}/api/pagamentos/webhook/mercadopago",
            "LOJAVIRTUAL");

        using var requisicao = Requisicao(HttpMethod.Post, "checkout/preferences");
        requisicao.Headers.Add("X-Idempotency-Key", pedido.Id.ToString()); // reenvio não cria outra cobrança
        requisicao.Content = JsonContent.Create(preferencia);

        using var resposta = await http.SendAsync(requisicao, ct);
        resposta.EnsureSuccessStatusCode();
        var criada = await resposta.Content.ReadFromJsonAsync<PreferenciaCriada>(ct)
            ?? throw new InvalidOperationException("Resposta vazia do Mercado Pago.");

        // Credenciais de teste usam o link de sandbox, quando disponível.
        var teste = _opcoes.AccessToken.StartsWith("TEST-", StringComparison.Ordinal);
        return (teste ? criada.SandboxInitPoint ?? criada.InitPoint : criada.InitPoint)
            ?? throw new InvalidOperationException("O Mercado Pago não devolveu o link de pagamento.");
    }

    public async Task<PagamentoGateway?> ConsultarPagamentoAsync(string pagamentoId, CancellationToken ct = default)
    {
        using var requisicao = Requisicao(HttpMethod.Get, $"v1/payments/{Uri.EscapeDataString(pagamentoId)}");
        using var resposta = await http.SendAsync(requisicao, ct);
        if (resposta.StatusCode == HttpStatusCode.NotFound)
            return null;
        resposta.EnsureSuccessStatusCode();

        var pagamento = await resposta.Content.ReadFromJsonAsync<PagamentoMercadoPago>(ct);
        if (pagamento is null)
            return null;

        var situacao = pagamento.Status switch
        {
            "approved" => SituacaoPagamento.Aprovado,
            "rejected" or "cancelled" or "refunded" or "charged_back" => SituacaoPagamento.Recusado,
            _ => SituacaoPagamento.Pendente,
        };
        Guid? pedidoId = Guid.TryParse(pagamento.ExternalReference, out var id) ? id : null;
        return new PagamentoGateway(pagamento.Id.ToString(CultureInfo.InvariantCulture), situacao, pedidoId, pagamento.TransactionAmount);
    }

    private HttpRequestMessage Requisicao(HttpMethod metodo, string caminho)
    {
        if (string.IsNullOrWhiteSpace(_opcoes.AccessToken))
            throw new InvalidOperationException("MercadoPago:AccessToken não configurado (use dotnet user-secrets).");
        var requisicao = new HttpRequestMessage(metodo, caminho);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opcoes.AccessToken);
        return requisicao;
    }

    private record ItemPreferencia(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("title")] string Titulo,
        [property: JsonPropertyName("quantity")] int Quantidade,
        [property: JsonPropertyName("unit_price")] decimal PrecoUnitario)
    {
        [JsonPropertyName("currency_id")] public string Moeda => "BRL";
    }

    private record UrlsRetorno(
        [property: JsonPropertyName("success")] string Sucesso,
        [property: JsonPropertyName("failure")] string Falha,
        [property: JsonPropertyName("pending")] string Pendente);

    private record Preferencia(
        [property: JsonPropertyName("items")] IReadOnlyList<ItemPreferencia> Itens,
        [property: JsonPropertyName("external_reference")] string ReferenciaExterna,
        [property: JsonPropertyName("back_urls")] UrlsRetorno UrlsRetorno,
        [property: JsonPropertyName("auto_return")] string RetornoAutomatico,
        [property: JsonPropertyName("notification_url"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? UrlNotificacao,
        [property: JsonPropertyName("statement_descriptor")] string DescricaoFatura);

    private record PreferenciaCriada(
        [property: JsonPropertyName("init_point")] string? InitPoint,
        [property: JsonPropertyName("sandbox_init_point")] string? SandboxInitPoint);

    private record PagamentoMercadoPago(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("external_reference")] string? ExternalReference,
        [property: JsonPropertyName("transaction_amount")] decimal TransactionAmount);
}
