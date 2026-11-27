using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Loja.Api.Autenticacao;

public static class AutenticacaoExtensions
{
    public const string PoliticaLimiteAutenticacao = "autenticacao";

    public static IServiceCollection AddAutenticacaoJwt(this IServiceCollection services, IConfiguration config)
    {
        var opcoes = config.GetSection(JwtOpcoes.Secao).Get<JwtOpcoes>() ?? new JwtOpcoes();
        var chave = opcoes.ChaveDeAssinatura(); // falha na inicialização se a chave não estiver configurada

        services.Configure<JwtOpcoes>(config.GetSection(JwtOpcoes.Secao));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<TokenServico>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false; // mantém os nomes curtos das claims ("sub", "role")
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = opcoes.Emissor,
                    ValidAudience = opcoes.Audiencia,
                    IssuerSigningKey = chave,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };
            });
        services.AddAuthorization();

        // Limita tentativas de login e cadastro por IP, contra ataques de força bruta.
        var limite = config.GetValue("Seguranca:TentativasPorMinuto", 10);
        services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddPolicy(PoliticaLimiteAutenticacao, ctx => RateLimitPartition.GetFixedWindowLimiter(
                ctx.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = limite, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }
}
