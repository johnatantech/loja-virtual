using Loja.Application.Catalogo;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Loja.Api.Endpoints;

public static class CatalogoEndpoints
{
    public static IEndpointRouteBuilder MapCatalogoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/categorias", async (ICatalogoConsultas consultas, CancellationToken ct) =>
                TypedResults.Ok(await consultas.ListarCategoriasAsync(ct)))
            .WithTags("Catálogo")
            .WithName("ListarCategorias")
            .WithSummary("Lista as categorias com a quantidade de produtos ativos.");

        var produtos = app.MapGroup("/api/produtos").WithTags("Catálogo");

        produtos.MapGet("/", ListarProdutos)
            .WithName("ListarProdutos")
            .WithSummary("Lista produtos com busca, filtros, ordenação e paginação.");

        produtos.MapGet("/{slug}", ObterProduto)
            .WithName("ObterProduto")
            .WithSummary("Detalhes de um produto pelo slug.");

        return app;
    }

    private static async Task<Results<Ok<Pagina<ProdutoResumoDto>>, ValidationProblem>> ListarProdutos(
        ICatalogoConsultas consultas, CancellationToken ct,
        string? busca, string? categoria, decimal? precoMin, decimal? precoMax,
        string? ordenacao, int pagina = 1, int tamanhoPagina = 12)
    {
        var filtro = new FiltroProdutos(busca, categoria, precoMin, precoMax, ordenacao, pagina, tamanhoPagina);
        var erros = filtro.Validar();
        if (erros.Count > 0)
            return TypedResults.ValidationProblem(erros);

        return TypedResults.Ok(await consultas.ListarProdutosAsync(filtro, ct));
    }

    private static async Task<Results<Ok<ProdutoDetalheDto>, ProblemHttpResult>> ObterProduto(
        string slug, ICatalogoConsultas consultas, CancellationToken ct)
    {
        var produto = await consultas.ObterProdutoAsync(slug, ct);
        return produto is null
            ? TypedResults.Problem(title: "Produto não encontrado.", statusCode: StatusCodes.Status404NotFound)
            : TypedResults.Ok(produto);
    }
}
