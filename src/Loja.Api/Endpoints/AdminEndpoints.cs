using Loja.Application.Admin;
using Loja.Application.Catalogo;
using Loja.Application.Pedidos;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Loja.Api.Endpoints;

public record ReposicaoRequest(int Quantidade);

public static class AdminEndpoints
{
    public const string PoliticaAdmin = "Admin";

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // Todo o grupo exige o perfil Admin: um cliente logado recebe 403.
        var admin = app.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization(PoliticaAdmin);

        admin.MapGet("/dashboard", async (IAdminServico s, CancellationToken ct) => TypedResults.Ok(await s.DashboardAsync(ct)))
            .WithSummary("Faturamento, pedidos por status, estoque baixo e mais vendidos.");

        // Produtos
        admin.MapGet("/produtos", async (string? busca, int? pagina, IAdminServico s, CancellationToken ct) =>
            TypedResults.Ok(await s.ListarProdutosAsync(busca, pagina ?? 1, ct)));

        admin.MapGet("/produtos/{id:int}", async Task<Results<Ok<ProdutoAdminDto>, NotFound>> (int id, IAdminServico s, CancellationToken ct) =>
            await s.ObterProdutoAsync(id, ct) is { } p ? TypedResults.Ok(p) : TypedResults.NotFound());

        admin.MapPost("/produtos", async (SalvarProdutoDto dados, IAdminServico s, CancellationToken ct) =>
        {
            var criado = await s.CriarProdutoAsync(dados, ct);
            return TypedResults.Created($"/api/admin/produtos/{criado.Id}", criado);
        });

        admin.MapPut("/produtos/{id:int}", async Task<Results<Ok<ProdutoAdminDto>, NotFound>> (
                int id, SalvarProdutoDto dados, IAdminServico s, CancellationToken ct) =>
            await s.AtualizarProdutoAsync(id, dados, ct) is { } p ? TypedResults.Ok(p) : TypedResults.NotFound());

        admin.MapPost("/produtos/{id:int}/estoque", async Task<Results<Ok<ProdutoAdminDto>, NotFound>> (
                int id, ReposicaoRequest req, IAdminServico s, CancellationToken ct) =>
            await s.ReporEstoqueAsync(id, req.Quantidade, ct) is { } p ? TypedResults.Ok(p) : TypedResults.NotFound())
            .WithSummary("Soma unidades ao estoque.");

        admin.MapPost("/produtos/{id:int}/ativar", async Task<Results<Ok<ProdutoAdminDto>, NotFound>> (int id, IAdminServico s, CancellationToken ct) =>
            await s.DefinirAtivoAsync(id, true, ct) is { } p ? TypedResults.Ok(p) : TypedResults.NotFound());

        admin.MapPost("/produtos/{id:int}/desativar", async Task<Results<Ok<ProdutoAdminDto>, NotFound>> (int id, IAdminServico s, CancellationToken ct) =>
            await s.DefinirAtivoAsync(id, false, ct) is { } p ? TypedResults.Ok(p) : TypedResults.NotFound())
            .WithSummary("Tira o produto da vitrine sem apagar o histórico de pedidos.");

        // Pedidos
        admin.MapGet("/pedidos", async (string? status, int? pagina, IAdminServico s, CancellationToken ct) =>
            TypedResults.Ok(await s.ListarPedidosAsync(status, pagina ?? 1, ct)));

        admin.MapGet("/pedidos/{id:guid}", async Task<Results<Ok<PedidoDetalheDto>, NotFound>> (Guid id, IAdminServico s, CancellationToken ct) =>
            await s.ObterPedidoAsync(id, ct) is { } p ? TypedResults.Ok(p) : TypedResults.NotFound());

        admin.MapPost("/pedidos/{id:guid}/{acao:regex(^(enviar|entregar|cancelar)$)}",
                async Task<Results<Ok<PedidoDetalheDto>, NotFound>> (Guid id, string acao, IAdminServico s, CancellationToken ct) =>
                await s.AvancarPedidoAsync(id, acao, ct) is { } p ? TypedResults.Ok(p) : TypedResults.NotFound())
            .WithSummary("Avança o pedido: enviar, entregar ou cancelar.");

        // Clientes
        admin.MapGet("/clientes", async (string? busca, int? pagina, IAdminServico s, CancellationToken ct) =>
            TypedResults.Ok(await s.ListarClientesAsync(busca, pagina ?? 1, ct)));

        admin.MapPost("/clientes/{id}/desbloquear", async Task<Results<NoContent, NotFound>> (string id, IAdminServico s, CancellationToken ct) =>
                await s.DesbloquearClienteAsync(id, ct) ? TypedResults.NoContent() : TypedResults.NotFound())
            .WithSummary("Remove o bloqueio temporário por tentativas de login.");

        return app;
    }
}
