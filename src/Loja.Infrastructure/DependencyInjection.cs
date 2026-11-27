using Loja.Application.Admin;
using Loja.Application.Carrinhos;
using Loja.Application.Catalogo;
using Loja.Application.Pedidos;
using Loja.Infrastructure.Consultas;
using Loja.Infrastructure.Identidade;
using Loja.Infrastructure.Pagamentos;
using Loja.Infrastructure.Persistencia;
using Loja.Infrastructure.Servicos;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Loja.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // A string de conexão nunca fica no código nem no appsettings:
        // vem de user-secrets (desenvolvimento) ou variável de ambiente (produção).
        var conexao = config.GetConnectionString("Loja")
            ?? throw new InvalidOperationException(
                "Connection string 'Loja' não configurada. Rode scripts/setup-dev.sh ou defina " +
                "a variável de ambiente ConnectionStrings__Loja.");

        services.AddDbContext<LojaDbContext>(o => o.UseSqlServer(conexao,
            sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddIdentityCore<Usuario>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = true;
                o.Password.RequireNonAlphanumeric = false;
                // Bloqueia a conta por 15 minutos após 5 senhas erradas seguidas.
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                o.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<LojaDbContext>()
            .AddErrorDescriber<ErrosIdentityPtBr>();

        // Sem isso, as chaves ficam numa pasta do contêiner e se perdem a cada deploy.
        services.AddDataProtection()
            .PersistKeysToDbContext<LojaDbContext>()
            .SetApplicationName("loja-virtual");

        services.AddScoped<ICatalogoConsultas, CatalogoConsultas>();
        services.AddScoped<ICarrinhoServico, CarrinhoServico>();
        services.AddScoped<IPedidoServico, PedidoServico>();
        services.AddScoped<IAdminServico, AdminServico>();
        services.TryAddSingleton(TimeProvider.System);

        // Gateway de pagamento escolhido por configuração: "Simulado" (padrão) ou "MercadoPago".
        services.Configure<MercadoPagoOpcoes>(config.GetSection(MercadoPagoOpcoes.Secao));
        if (string.Equals(config["Pagamentos:Gateway"], "MercadoPago", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IGatewayPagamento, GatewayMercadoPago>(c =>
            {
                c.BaseAddress = new Uri("https://api.mercadopago.com/");
                c.Timeout = TimeSpan.FromSeconds(15);
            });
        }
        else
        {
            services.AddSingleton<IGatewayPagamento, GatewaySimulado>();
        }
        return services;
    }
}
