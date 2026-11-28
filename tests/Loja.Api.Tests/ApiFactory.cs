using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Loja.Api.Tests;

/// <summary>
/// Sobe um PostgreSQL real em container (Testcontainers) para os testes de integração.
/// Requer Docker, que já vem no Codespace e no GitHub Actions. Sem Docker, dá para apontar
/// para um PostgreSQL existente pela variável TESTES_POSTGRES (cada execução usa um banco novo).
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string EmailAdmin = "admin@teste.local";
    public const string SenhaAdmin = "AdminTeste123";

    private readonly PostgreSqlContainer? _banco =
        Environment.GetEnvironmentVariable("TESTES_POSTGRES") is null
            ? new PostgreSqlBuilder("postgres:17-alpine").Build()
            : null;

    // Chave só de teste, a mesma para todas as variações desta factory; a real fica em user-secrets.
    private readonly string _chaveJwt = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Guid.NewGuid();
    private string _conexao = "";

    public async Task InitializeAsync()
    {
        if (_banco is not null)
        {
            await _banco.StartAsync();
            _conexao = _banco.GetConnectionString();
        }
        else
        {
            _conexao = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("TESTES_POSTGRES"))
            {
                Database = $"loja_testes_{Guid.NewGuid():N}",
            }.ConnectionString;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development"); // aplica migrations e dados iniciais
        builder.UseSetting("ConnectionStrings:Loja", _conexao);
        builder.UseSetting("Jwt:Chave", _chaveJwt);
        // Os testes fazem muitos logins seguidos; o limite real continua valendo fora deles.
        builder.UseSetting("Seguranca:TentativasPorMinuto", "1000");
        // Administrador criado na inicialização, como no setup-dev.sh.
        builder.UseSetting("Admin:Email", EmailAdmin);
        builder.UseSetting("Admin:Senha", SenhaAdmin);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_banco is not null) await _banco.DisposeAsync();
        await base.DisposeAsync();
    }
}
