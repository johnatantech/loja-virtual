using System.Security.Claims;
using Loja.Api.Autenticacao;
using Loja.Application.Carrinhos;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Loja.Api.Endpoints;

public record ItensRequest(IReadOnlyList<ItemSolicitadoDto> Itens);
public record QuantidadeRequest(int Quantidade);

public static class CarrinhoEndpoints
{
    private const int MaximoDeItensPorRequisicao = 50;

    public static IEndpointRouteBuilder MapCarrinhoEndpoints(this IEndpointRouteBuilder app)
    {
        var carrinho = app.MapGroup("/api/carrinho").WithTags("Carrinho");

        // Aberto: o carrinho de visitante fica no navegador e só é precificado aqui.
        carrinho.MapPost("/cotacao", Cotar)
            .WithSummary("Precifica itens com os dados atuais do catálogo, sem salvar nada.");

        var autenticado = carrinho.MapGroup("").RequireAuthorization();

        autenticado.MapGet("/", (ClaimsPrincipal u, ICarrinhoServico s, CancellationToken ct) =>
                s.ObterAsync(u.IdUsuario(), ct))
            .WithSummary("Carrinho do usuário autenticado.");

        autenticado.MapPut("/itens/{produtoId:int}", (int produtoId, QuantidadeRequest req,
                ClaimsPrincipal u, ICarrinhoServico s, CancellationToken ct) =>
                s.DefinirQuantidadeAsync(u.IdUsuario(), produtoId, req.Quantidade, ct))
            .WithSummary("Define a quantidade de um produto (zero remove).");

        autenticado.MapDelete("/itens/{produtoId:int}", (int produtoId,
                ClaimsPrincipal u, ICarrinhoServico s, CancellationToken ct) =>
                s.DefinirQuantidadeAsync(u.IdUsuario(), produtoId, 0, ct))
            .WithSummary("Remove um produto do carrinho.");

        autenticado.MapPost("/mesclar", Mesclar)
            .WithSummary("Junta o carrinho de visitante ao da conta, após o login.");

        autenticado.MapDelete("/", (ClaimsPrincipal u, ICarrinhoServico s, CancellationToken ct) =>
                s.EsvaziarAsync(u.IdUsuario(), ct))
            .WithSummary("Esvazia o carrinho.");

        return app;
    }

    private static async Task<Results<Ok<CarrinhoDto>, ValidationProblem>> Cotar(
        ItensRequest req, ICarrinhoServico servico, CancellationToken ct)
    {
        if (Invalido(req, out var erro)) return erro;
        return TypedResults.Ok(await servico.CotarAsync(req.Itens, ct));
    }

    private static async Task<Results<Ok<CarrinhoDto>, ValidationProblem>> Mesclar(
        ItensRequest req, ClaimsPrincipal usuario, ICarrinhoServico servico, CancellationToken ct)
    {
        if (Invalido(req, out var erro)) return erro;
        return TypedResults.Ok(await servico.MesclarAsync(usuario.IdUsuario(), req.Itens, ct));
    }

    private static bool Invalido(ItensRequest req, out ValidationProblem erro)
    {
        erro = null!;
        if (req.Itens is null || req.Itens.Count > MaximoDeItensPorRequisicao)
        {
            erro = TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["itens"] = [$"Envie de 0 a {MaximoDeItensPorRequisicao} itens."],
            });
            return true;
        }
        return false;
    }
}
