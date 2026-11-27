using System.Security.Claims;
using Loja.Api.Autenticacao;
using Loja.Application.Pedidos;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Loja.Api.Endpoints;

public record SimulacaoRequest(bool Aprovar);

public static class PedidosEndpoints
{
    public static IEndpointRouteBuilder MapPedidosEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/checkout/frete", async (string cep, ClaimsPrincipal u, IPedidoServico s, CancellationToken ct) =>
                TypedResults.Ok(await s.CotarFreteAsync(u.IdUsuario(), cep, ct)))
            .RequireAuthorization()
            .WithTags("Checkout")
            .WithSummary("Calcula o frete do carrinho atual para um CEP.");

        var pedidos = app.MapGroup("/api/pedidos").WithTags("Pedidos").RequireAuthorization();

        pedidos.MapPost("/", async (NovoPedidoDto dados, ClaimsPrincipal u, IPedidoServico s, CancellationToken ct) =>
            {
                var criado = await s.CriarAsync(u.IdUsuario(), dados, ct);
                return TypedResults.Created($"/api/pedidos/{criado.Id}", criado);
            })
            .WithSummary("Fecha o pedido a partir do carrinho e devolve o link de pagamento.");

        pedidos.MapGet("/", async (ClaimsPrincipal u, IPedidoServico s, CancellationToken ct) =>
                TypedResults.Ok(await s.ListarAsync(u.IdUsuario(), ct)))
            .WithSummary("Pedidos do cliente, do mais recente para o mais antigo.");

        pedidos.MapGet("/{id:guid}", async Task<Results<Ok<PedidoDetalheDto>, NotFound>> (
                Guid id, ClaimsPrincipal u, IPedidoServico s, CancellationToken ct) =>
                await s.ObterAsync(u.IdUsuario(), id, ct) is { } p ? TypedResults.Ok(p) : TypedResults.NotFound())
            .WithSummary("Detalhes de um pedido do cliente (404 para pedidos de outras pessoas).");

        pedidos.MapPost("/{id:guid}/cancelar", async Task<Results<Ok<PedidoDetalheDto>, NotFound>> (
                Guid id, ClaimsPrincipal u, IPedidoServico s, CancellationToken ct) =>
                await s.CancelarAsync(u.IdUsuario(), id, ct) is { } p ? TypedResults.Ok(p) : TypedResults.NotFound())
            .WithSummary("Cancela um pedido que ainda aguarda pagamento e devolve os itens ao estoque.");

        pedidos.MapPost("/{id:guid}/pagamento", async Task<Results<Ok<PedidoCriadoDto>, NotFound>> (
                Guid id, ClaimsPrincipal u, IPedidoServico s, CancellationToken ct) =>
                await s.GerarPagamentoAsync(u.IdUsuario(), id, ct) is { } p ? TypedResults.Ok(p) : TypedResults.NotFound())
            .WithSummary("Gera novamente o link de pagamento.");

        return app;
    }
}
