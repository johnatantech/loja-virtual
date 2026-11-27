using System.Net.Http.Headers;
using System.Net.Http.Json;
using Loja.Api.Autenticacao;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Loja.Api.Tests;

/// <summary>Atalhos para os testes: criar uma conta nova e obter um cliente HTTP já autenticado.</summary>
public static class ApiCliente
{
    public const string SenhaValida = "Senha123";

    public static string EmailUnico() => $"teste-{Guid.NewGuid():N}@exemplo.com";

    public static async Task<HttpClient> EntrarComoAdminAsync(this WebApplicationFactory<Program> factory)
    {
        var cliente = factory.CreateClient();
        var resposta = await cliente.PostAsJsonAsync("/api/contas/login",
            new { email = ApiFactory.EmailAdmin, senha = ApiFactory.SenhaAdmin });
        resposta.EnsureSuccessStatusCode();
        var sessao = (await resposta.Content.ReadFromJsonAsync<SessaoDto>())!;
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessao.Token);
        return cliente;
    }

    public static async Task<(HttpClient Cliente, SessaoDto Sessao)> CriarContaAsync(this WebApplicationFactory<Program> factory)
    {
        var cliente = factory.CreateClient();
        var resposta = await cliente.PostAsJsonAsync("/api/contas/cadastro",
            new { nome = "Cliente de Teste", email = EmailUnico(), senha = SenhaValida });
        resposta.EnsureSuccessStatusCode();

        var sessao = (await resposta.Content.ReadFromJsonAsync<SessaoDto>())!;
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sessao.Token);
        return (cliente, sessao);
    }
}
