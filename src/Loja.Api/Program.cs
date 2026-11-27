using Loja.Api.Autenticacao;
using Loja.Api.Endpoints;
using Loja.Api.Middleware;
using Loja.Infrastructure;
using Loja.Infrastructure.Identidade;
using Loja.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAutenticacaoJwt(builder.Configuration);
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AdminEndpoints.PoliticaAdmin, p => p.RequireRole(Perfis.Admin));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddDbContextCheck<LojaDbContext>("banco");

var origensFront = builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origensFront).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Erros inesperados viram 500 genérico (sem detalhes internos para o cliente).
app.UseExceptionHandler(erro => erro.Run(ctx =>
    Results.Problem(title: "Erro inesperado no servidor.", statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(ctx)));
// Regras de negócio violadas viram 400 (e conflitos de concorrência, 409), registradas como aviso.
app.UseMiddleware<ErrosDeNegocioMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.WithTitle("Loja Virtual API"));

    // Em desenvolvimento, aplica as migrations e popula perfis e catálogo de demonstração.
    using var escopo = app.Services.CreateScope();
    var db = escopo.ServiceProvider.GetRequiredService<LojaDbContext>();
    await db.Database.MigrateAsync();
    await DadosIniciais.PopularAsync(db,
        escopo.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(),
        escopo.ServiceProvider.GetRequiredService<UserManager<Usuario>>(),
        builder.Configuration["Admin:Email"], builder.Configuration["Admin:Senha"]);
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapCatalogoEndpoints();
app.MapContasEndpoints();
app.MapCarrinhoEndpoints();
app.MapPedidosEndpoints();
app.MapPagamentosEndpoints();
app.MapAdminEndpoints();

app.Run();

public partial class Program; // permite que os testes de integração acessem a API
