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

    public async Task InitializeAsync() => await _banco.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development"); // aplica migrations e dados iniciais
        builder.UseSetting("ConnectionStrings:Loja", _banco.GetConnectionString());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _banco.DisposeAsync();
        await base.DisposeAsync();
    }
}
