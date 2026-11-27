using System.Security.Claims;
using Loja.Api.Autenticacao;
using Loja.Application.Pedidos;
using Loja.Infrastructure.Pagamentos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Loja.Api.Endpoints;

public static class PagamentosEndpoints
{
    public static IEndpointRouteBuilder MapPagamentosEndpoints(this IEndpointRouteBuilder app)
    {
        var pagamentos = app.MapGroup("/api/pagamentos").WithTags("Pagamentos");

        pagamentos.MapPost("/webhook/mercadopago", ReceberNotificacaoMercadoPago)
            .WithSummary("Notificações do Mercado Pago. Exige assinatura válida no cabeçalho x-signature.");

        pagamentos.MapPost("/simulado/{id:guid}", async Task<Results<Ok<PedidoDetalheDto>, NotFound>> (
                Guid id, SimulacaoRequest req, ClaimsPrincipal u, IPedidoServico s, CancellationToken ct) =>
                await s.SimularPagamentoAsync(u.IdUsuario(), id, req.Aprovar, ct) is { } p
                    ? TypedResults.Ok(p)
                    : TypedResults.NotFound())
            .RequireAuthorization()
            .WithSummary("Só com o gateway simulado: aprova ou recusa o pagamento de um pedido.");

        return app;
    }

    private static async Task<IResult> ReceberNotificacaoMercadoPago(
        HttpRequest requisicao, IPedidoServico pedidos, IOptions<MercadoPagoOpcoes> opcoes,
        ILoggerFactory logs, CancellationToken ct)
    {
        var log = logs.CreateLogger("Pagamentos.Webhook");
        var dataId = requisicao.Query["data.id"].ToString();
        var tipo = requisicao.Query["type"].ToString();

        // 1. Origem: sem assinatura válida, a notificação é descartada antes de qualquer processamento.
        if (!AssinaturaWebhookMercadoPago.Valida(
                requisicao.Headers["x-signature"], requisicao.Headers["x-request-id"], dataId, opcoes.Value.SegredoWebhook))
        {
            log.LogWarning("Notificação com assinatura inválida recusada (data.id={DataId}).", dataId);
            return TypedResults.Unauthorized();
        }

        // 2. Só interessam notificações de pagamento; as demais são confirmadas e ignoradas.
        if (tipo != "payment" || string.IsNullOrWhiteSpace(dataId))
            return TypedResults.Ok();

        // 3. O pagamento é consultado no gateway; reenvios da mesma notificação não têm efeito.
        await pedidos.ProcessarPagamentoAsync(dataId, ct);
        return TypedResults.Ok();
    }
}
