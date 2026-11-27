using Loja.Api.Endpoints;
using Loja.Domain.Comum;
using Loja.Infrastructure;
using Loja.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddDbContextCheck<LojaDbContext>("banco");

var origensFront = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origensFront).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Regras de negócio violadas viram 400 com ProblemDetails, em vez de erro 500.
app.UseExceptionHandler(erro => erro.Run(async ctx =>
{
    var excecao = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, titulo) = excecao is DomainException
        ? (StatusCodes.Status400BadRequest, excecao.Message)
        : (StatusCodes.Status500InternalServerError, "Erro inesperado no servidor.");
    ctx.Response.StatusCode = status;
    await Results.Problem(title: titulo, statusCode: status).ExecuteAsync(ctx);
}));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.WithTitle("Loja Virtual API"));

    // Em desenvolvimento, aplica as migrations e popula o catálogo de demonstração.
    using var escopo = app.Services.CreateScope();
    var db = escopo.ServiceProvider.GetRequiredService<LojaDbContext>();
    await db.Database.MigrateAsync();
    await DadosIniciais.PopularAsync(db);
}

app.UseCors();
app.MapHealthChecks("/health");
app.MapCatalogoEndpoints();

app.Run();

public partial class Program; // permite que os testes de integração acessem a API
