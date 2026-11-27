using Loja.Application.Catalogo;
using Loja.Infrastructure.Consultas;
using Loja.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<ICatalogoConsultas, CatalogoConsultas>();
        return services;
    }
}
