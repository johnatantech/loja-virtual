using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Loja.Api.Autenticacao;

public class JwtOpcoes
{
    public const string Secao = "Jwt";

    public string Emissor { get; set; } = "loja-virtual";
    public string Audiencia { get; set; } = "loja-virtual-web";
    public int ExpiracaoMinutos { get; set; } = 120;

    /// <summary>Chave de assinatura. Vem de user-secrets ou variável de ambiente, nunca do código.</summary>
    public string Chave { get; set; } = "";

    public SymmetricSecurityKey ChaveDeAssinatura()
    {
        var bytes = Encoding.UTF8.GetBytes(Chave);
        if (bytes.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Chave ausente ou curta demais (mínimo de 32 bytes). Rode scripts/setup-dev.sh " +
                "ou defina a variável de ambiente Jwt__Chave.");
        return new SymmetricSecurityKey(bytes);
    }
}
