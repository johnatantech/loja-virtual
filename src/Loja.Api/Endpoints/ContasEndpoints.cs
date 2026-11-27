using System.Net.Mail;
using System.Security.Claims;
using Loja.Api.Autenticacao;
using Loja.Infrastructure.Identidade;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;

namespace Loja.Api.Endpoints;

public record CadastroRequest(string Nome, string Email, string Senha);
public record LoginRequest(string Email, string Senha);

public static class ContasEndpoints
{
    // Mesma mensagem para e-mail inexistente e senha errada: não revela quais e-mails têm conta.
    private const string CredenciaisInvalidas = "E-mail ou senha inválidos.";

    public static IEndpointRouteBuilder MapContasEndpoints(this IEndpointRouteBuilder app)
    {
        var contas = app.MapGroup("/api/contas").WithTags("Contas");

        contas.MapPost("/cadastro", Cadastrar)
            .RequireRateLimiting(AutenticacaoExtensions.PoliticaLimiteAutenticacao)
            .WithSummary("Cria uma conta de cliente e já devolve a sessão.");

        contas.MapPost("/login", Entrar)
            .RequireRateLimiting(AutenticacaoExtensions.PoliticaLimiteAutenticacao)
            .WithSummary("Autentica com e-mail e senha e devolve um token JWT.");

        contas.MapGet("/eu", DadosDoUsuario)
            .RequireAuthorization()
            .WithSummary("Dados do usuário autenticado.");

        return app;
    }

    private static async Task<Results<Created<SessaoDto>, ValidationProblem>> Cadastrar(
        CadastroRequest req, UserManager<Usuario> usuarios, TokenServico tokens)
    {
        var erros = new Dictionary<string, string[]>();
        var nome = req.Nome?.Trim() ?? "";
        var email = req.Email?.Trim().ToLowerInvariant() ?? "";

        if (nome.Length is < 2 or > 100)
            erros["nome"] = ["Informe seu nome (de 2 a 100 caracteres)."];
        if (!MailAddress.TryCreate(email, out _) || email.Length > 256)
            erros["email"] = ["Informe um e-mail válido."];
        if (erros.Count > 0)
            return TypedResults.ValidationProblem(erros);

        var usuario = new Usuario { Nome = nome, Email = email, UserName = email };
        var resultado = await usuarios.CreateAsync(usuario, req.Senha ?? "");
        if (!resultado.Succeeded)
        {
            return TypedResults.ValidationProblem(resultado.Errors
                .GroupBy(e => e.Code.Contains("Password") ? "senha" : "email")
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).Distinct().ToArray()));
        }

        await usuarios.AddToRoleAsync(usuario, Perfis.Cliente);
        var sessao = tokens.CriarSessao(usuario, [Perfis.Cliente]);
        return TypedResults.Created("/api/contas/eu", sessao);
    }

    private static async Task<Results<Ok<SessaoDto>, ProblemHttpResult>> Entrar(
        LoginRequest req, UserManager<Usuario> usuarios, TokenServico tokens)
    {
        var usuario = await usuarios.FindByEmailAsync(req.Email?.Trim() ?? "");
        if (usuario is null)
            return NaoAutorizado(CredenciaisInvalidas);

        if (await usuarios.IsLockedOutAsync(usuario))
            return NaoAutorizado("Muitas tentativas sem sucesso. Tente novamente em alguns minutos.");

        if (!await usuarios.CheckPasswordAsync(usuario, req.Senha ?? ""))
        {
            await usuarios.AccessFailedAsync(usuario); // conta para o bloqueio temporário
            return NaoAutorizado(CredenciaisInvalidas);
        }

        await usuarios.ResetAccessFailedCountAsync(usuario);
        return TypedResults.Ok(tokens.CriarSessao(usuario, await usuarios.GetRolesAsync(usuario)));
    }

    private static async Task<Results<Ok<UsuarioDto>, UnauthorizedHttpResult>> DadosDoUsuario(
        ClaimsPrincipal principal, UserManager<Usuario> usuarios)
    {
        var usuario = await usuarios.FindByIdAsync(principal.IdUsuario());
        if (usuario is null)
            return TypedResults.Unauthorized();

        var perfis = await usuarios.GetRolesAsync(usuario);
        return TypedResults.Ok(new UsuarioDto(usuario.Id, usuario.Nome, usuario.Email!, [.. perfis]));
    }

    private static ProblemHttpResult NaoAutorizado(string mensagem) =>
        TypedResults.Problem(title: mensagem, statusCode: StatusCodes.Status401Unauthorized);
}
