using Loja.Domain.Comum;
using Microsoft.EntityFrameworkCore;

namespace Loja.Api.Middleware;

/// <summary>
/// Regras de negócio violadas (estoque insuficiente, pedido em status errado) são situações
/// esperadas, não falhas do sistema: viram 400 e são registradas como aviso, sem stack trace.
/// Conflitos de concorrência viram 409. Só erros inesperados chegam ao tratamento de 500.
/// </summary>
public class ErrosDeNegocioMiddleware(RequestDelegate proximo, ILogger<ErrosDeNegocioMiddleware> log)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await proximo(ctx);
        }
        catch (DomainException ex) when (!ctx.Response.HasStarted)
        {
            log.LogWarning("Regra de negócio em {Metodo} {Caminho}: {Mensagem}", ctx.Request.Method, ctx.Request.Path, ex.Message);
            await Responder(ctx, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (DbUpdateConcurrencyException) when (!ctx.Response.HasStarted)
        {
            log.LogWarning("Conflito de concorrência em {Metodo} {Caminho}.", ctx.Request.Method, ctx.Request.Path);
            await Responder(ctx, StatusCodes.Status409Conflict,
                "Este registro foi alterado por outra operação ao mesmo tempo. Recarregue e tente de novo.");
        }
    }

    private static Task Responder(HttpContext ctx, int status, string titulo)
    {
        ctx.Response.Clear();
        ctx.Response.StatusCode = status;
        return Results.Problem(title: titulo, statusCode: status).ExecuteAsync(ctx);
    }
}
