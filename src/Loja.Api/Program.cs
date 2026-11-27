using Loja.Api.Autenticacao;
using Loja.Api.Endpoints;
using Loja.Api.Middleware;
using Loja.Infrastructure;
using Loja.Infrastructure.Identidade;
using Loja.Infrastructure.Persistencia;
using Microsoft.AspNetCore.HttpOverrides;
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

// Atrás do proxy do Azure, o IP do visitante chega no cabeçalho X-Forwarded-For.
// Sem isso, o limite de tentativas de login valeria para todos os visitantes somados.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.ForwardLimit = 1;      // usa só o IP acrescentado pelo proxy, não o que o cliente enviou
    o.KnownIPNetworks.Clear(); // os IPs do proxy do App Service não são fixos
    o.KnownProxies.Clear();
});

var app = builder.Build();
var confiarNoProxy = app.Configuration.GetValue<bool>("Proxy:ConfiarEmCabecalhosEncaminhados");
if (confiarNoProxy)
    app.UseForwardedHeaders();

// Erros inesperados viram 500 genérico (sem detalhes internos para o cliente).
app.UseExceptionHandler(erro => erro.Run(ctx =>
    Results.Problem(title: "Erro inesperado no servidor.", statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(ctx)));
// Regras de negócio violadas viram 400 (e conflitos de concorrência, 409), registradas como aviso.
app.UseMiddleware<ErrosDeNegocioMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.WithTitle("Loja Virtual API"));
}
else
{
    app.UseHsts(); // navegadores passam a usar só HTTPS com a API
    app.UseHttpsRedirection();
}

// Em desenvolvimento (e na demo publicada, por configuração), aplica as migrations e
// popula perfis, catálogo de demonstração e o administrador.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Banco:AplicarMigracoesNaInicializacao"))
{
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

// /health consulta o banco; /health/vivo só confirma que a API responde, sem acordar o banco
// (útil num plano gratuito, em que o banco pausa quando ninguém usa).
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/vivo", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false });
app.MapCatalogoEndpoints();
app.MapContasEndpoints();
app.MapCarrinhoEndpoints();
app.MapPedidosEndpoints();
app.MapPagamentosEndpoints();
app.MapAdminEndpoints();

app.Run();

public partial class Program; // permite que os testes de integração acessem a API
