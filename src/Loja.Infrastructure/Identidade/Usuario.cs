using Microsoft.AspNetCore.Identity;

namespace Loja.Infrastructure.Identidade;

/// <summary>Conta de acesso. Senha, bloqueio por tentativas e perfis ficam a cargo do Identity.</summary>
public class Usuario : IdentityUser
{
    public string Nome { get; set; } = "";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public static class Perfis
{
    public const string Cliente = "Cliente";
    public const string Admin = "Admin";
    public static readonly string[] Todos = [Cliente, Admin];
}
