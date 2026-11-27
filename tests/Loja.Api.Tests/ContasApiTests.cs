using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Loja.Api.Autenticacao;

namespace Loja.Api.Tests;

[Collection("Api")]
public class ContasApiTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Cadastro_devolve_sessao_com_perfil_de_cliente()
    {
        var email = ApiCliente.EmailUnico();
        var resposta = await _client.PostAsJsonAsync("/api/contas/cadastro",
            new { nome = "Maria", email, senha = ApiCliente.SenhaValida });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var sessao = await resposta.Content.ReadFromJsonAsync<SessaoDto>();
        Assert.False(string.IsNullOrEmpty(sessao!.Token));
        Assert.Equal(email, sessao.Usuario.Email);
        Assert.Equal(["Cliente"], sessao.Usuario.Perfis);
    }

    [Fact]
    public async Task Cadastro_rejeita_email_repetido()
    {
        var email = ApiCliente.EmailUnico();
        var dados = new { nome = "Maria", email, senha = ApiCliente.SenhaValida };
        await _client.PostAsJsonAsync("/api/contas/cadastro", dados);

        var repetido = await _client.PostAsJsonAsync("/api/contas/cadastro", dados);
        Assert.Equal(HttpStatusCode.BadRequest, repetido.StatusCode);
        Assert.Contains("Já existe uma conta", await repetido.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("curta1A")]
    [InlineData("semnumeroAA")]
    [InlineData("SEMMINUSCULA1")]
    public async Task Cadastro_exige_senha_forte(string senha)
    {
        var resposta = await _client.PostAsJsonAsync("/api/contas/cadastro",
            new { nome = "Maria", email = ApiCliente.EmailUnico(), senha });
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_com_credenciais_corretas_devolve_token()
    {
        var (_, sessao) = await factory.CriarContaAsync();
        var resposta = await _client.PostAsJsonAsync("/api/contas/login",
            new { email = sessao.Usuario.Email, senha = ApiCliente.SenhaValida });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_nao_revela_se_o_email_existe()
    {
        var (_, sessao) = await factory.CriarContaAsync();
        var senhaErrada = await _client.PostAsJsonAsync("/api/contas/login",
            new { email = sessao.Usuario.Email, senha = "Errada123" });
        var emailInexistente = await _client.PostAsJsonAsync("/api/contas/login",
            new { email = ApiCliente.EmailUnico(), senha = "Errada123" });

        Assert.Equal(HttpStatusCode.Unauthorized, senhaErrada.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, emailInexistente.StatusCode);
        // Mesma mensagem nos dois casos (o corpo inteiro difere só no traceId de cada requisição).
        var titulo1 = (await senhaErrada.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>())!.Title;
        var titulo2 = (await emailInexistente.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>())!.Title;
        Assert.Equal("E-mail ou senha inválidos.", titulo1);
        Assert.Equal(titulo1, titulo2);
    }

    [Fact]
    public async Task Conta_e_bloqueada_apos_cinco_senhas_erradas()
    {
        var (_, sessao) = await factory.CriarContaAsync();
        for (var i = 0; i < 5; i++)
            await _client.PostAsJsonAsync("/api/contas/login", new { email = sessao.Usuario.Email, senha = "Errada123" });

        // Mesmo com a senha certa, a conta fica bloqueada temporariamente.
        var resposta = await _client.PostAsJsonAsync("/api/contas/login",
            new { email = sessao.Usuario.Email, senha = ApiCliente.SenhaValida });
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Contains("Muitas tentativas", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Dados_do_usuario_exigem_token_valido()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/contas/eu")).StatusCode);

        var forjado = factory.CreateClient();
        forjado.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token.forjado.invalido");
        Assert.Equal(HttpStatusCode.Unauthorized, (await forjado.GetAsync("/api/contas/eu")).StatusCode);

        var (autenticado, sessao) = await factory.CriarContaAsync();
        var eu = await autenticado.GetFromJsonAsync<UsuarioDto>("/api/contas/eu");
        Assert.Equal(sessao.Usuario.Email, eu!.Email);
    }
}
