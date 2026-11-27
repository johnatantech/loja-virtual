using System.Security.Claims;
using Loja.Infrastructure.Identidade;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Loja.Api.Autenticacao;

public record UsuarioDto(string Id, string Nome, string Email, IReadOnlyList<string> Perfis);
public record SessaoDto(string Token, DateTime ExpiraEm, UsuarioDto Usuario);

public class TokenServico(IOptions<JwtOpcoes> opcoes, TimeProvider relogio)
{
    private readonly JwtOpcoes _opcoes = opcoes.Value;

    public SessaoDto CriarSessao(Usuario usuario, IList<string> perfis)
    {
        var expiraEm = relogio.GetUtcNow().UtcDateTime.AddMinutes(_opcoes.ExpiracaoMinutos);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id),
            new(JwtRegisteredClaimNames.Email, usuario.Email!),
            new(JwtRegisteredClaimNames.Name, usuario.Nome),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        claims.AddRange(perfis.Select(p => new Claim("role", p)));

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _opcoes.Emissor,
            Audience = _opcoes.Audiencia,
            Expires = expiraEm,
            SigningCredentials = new SigningCredentials(_opcoes.ChaveDeAssinatura(), SecurityAlgorithms.HmacSha256),
        });

        return new SessaoDto(token, expiraEm, new UsuarioDto(usuario.Id, usuario.Nome, usuario.Email!, [.. perfis]));
    }
}

public static class ClaimsExtensions
{
    /// <summary>Id do usuário autenticado (claim "sub" do token).</summary>
    public static string IdUsuario(this ClaimsPrincipal usuario) =>
        usuario.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? throw new InvalidOperationException("Token sem identificação do usuário.");
}
