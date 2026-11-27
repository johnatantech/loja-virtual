using Loja.Application.Catalogo;

namespace Loja.Api.Endpoints;

public static class CatalogoEndpoints
{
    public static IEndpointRouteBuilder MapCatalogoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/categorias").WithTags("Catálogo");

        grupo.MapGet("/", async (ICatalogoConsultas consultas, CancellationToken ct) =>
                TypedResults.Ok(await consultas.ListarCategoriasAsync(ct)))
            .WithName("ListarCategorias")
            .WithSummary("Lista as categorias com a quantidade de produtos ativos.");

        return app;
    }
}
