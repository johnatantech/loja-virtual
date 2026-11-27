using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;

namespace Loja.Api.Tests;

/// <summary>
/// Sobe um SQL Server real em container (Testcontainers) para os testes de integração.
/// Requer Docker, que já vem no Codespace do projeto.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _banco = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    // Chave só de teste, a mesma para todas as variações desta factory; a real fica em user-secrets.
    private readonly string _chaveJwt = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Guid.NewGuid();

    public const string EmailAdmin = "admin@teste.local";
    public const string SenhaAdmin = "AdminTeste123";

    public async Task InitializeAsync() => await _banco.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development"); // aplica migrations e dados iniciais
        builder.UseSetting("ConnectionStrings:Loja", _banco.GetConnectionString());
        builder.UseSetting("Jwt:Chave", _chaveJwt);
        // Os testes fazem muitos logins seguidos; o limite real continua valendo fora deles.
        builder.UseSetting("Seguranca:TentativasPorMinuto", "1000");
        // Administrador criado na inicialização, como no setup-dev.sh.
        builder.UseSetting("Admin:Email", EmailAdmin);
        builder.UseSetting("Admin:Senha", SenhaAdmin);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _banco.DisposeAsync();
        await base.DisposeAsync();
    }
}
