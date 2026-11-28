using Loja.Domain.Catalogo;
using Loja.Infrastructure.Identidade;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Loja.Infrastructure.Persistencia;

/// <summary>Perfis de acesso e catálogo de demonstração, inseridos só quando ainda não existem.</summary>
public static class DadosIniciais
{
    public static async Task PopularAsync(
        LojaDbContext db, RoleManager<IdentityRole> perfis, UserManager<Usuario> usuarios,
        string? emailAdmin, string? senhaAdmin, CancellationToken ct = default)
    {
        foreach (var perfil in Perfis.Todos)
        {
            if (!await perfis.RoleExistsAsync(perfil))
                await perfis.CreateAsync(new IdentityRole(perfil));
        }

        await CriarAdminAsync(usuarios, emailAdmin, senhaAdmin);

        if (await db.Categorias.AnyAsync(ct))
            return;

        var eletronicos = new Categoria("Eletrônicos");
        var livros = new Categoria("Livros");
        var casa = new Categoria("Casa e Cozinha");
        var esporte = new Categoria("Esporte e Lazer");
        db.Categorias.AddRange(eletronicos, livros, casa, esporte);
        await db.SaveChangesAsync(ct);

        db.Produtos.AddRange(
            new Produto("Fone de Ouvido Bluetooth", "Cancelamento de ruído e 30 horas de bateria.", 349.90m, 25, eletronicos.Id, "/produtos/fone-de-ouvido-bluetooth.webp"),
            new Produto("Teclado Mecânico", "Switches táteis, layout ABNT2 e iluminação RGB.", 459.00m, 12, eletronicos.Id, "/produtos/teclado-mecanico.webp"),
            new Produto("Mouse Sem Fio", "Sensor de 16.000 DPI e bateria recarregável.", 189.90m, 40, eletronicos.Id, "/produtos/mouse-sem-fio.webp"),
            new Produto("Monitor 27 Polegadas", "Resolução QHD, 144 Hz e painel IPS.", 1899.00m, 5, eletronicos.Id, "/produtos/monitor-27-polegadas.webp"),
            new Produto("Código Limpo", "Clássico sobre boas práticas de programação.", 89.90m, 30, livros.Id, "/produtos/codigo-limpo.webp"),
            new Produto("Arquitetura Limpa", "Princípios de design de software duradouro.", 94.50m, 18, livros.Id, "/produtos/arquitetura-limpa.webp"),
            new Produto("Python para Análise de Dados", "Pandas, NumPy e Jupyter na prática.", 129.00m, 22, livros.Id, "/produtos/python-para-analise-de-dados.webp"),
            new Produto("Cafeteira Italiana", "Alumínio, 6 xícaras, para fogão a gás e elétrico.", 119.90m, 15, casa.Id, "/produtos/cafeteira-italiana.webp"),
            new Produto("Jogo de Panelas Antiaderente", "5 peças com tampas de vidro.", 399.00m, 8, casa.Id, "/produtos/jogo-de-panelas-antiaderente.webp"),
            new Produto("Garrafa Térmica 1 Litro", "Mantém a temperatura por até 24 horas.", 99.90m, 35, esporte.Id, "/produtos/garrafa-termica-1-litro.webp"),
            new Produto("Tapete de Yoga", "Antiderrapante, 6 mm de espessura.", 79.90m, 20, esporte.Id, "/produtos/tapete-de-yoga.webp"),
            new Produto("Bicicleta Aro 29", "Quadro de alumínio, 21 marchas e freio a disco.", 1590.00m, 3, esporte.Id, "/produtos/bicicleta-aro-29.webp"));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Cria a conta de administrador com e-mail e senha vindos da configuração (user-secrets),
    /// nunca do código. Sem configuração, nenhum admin é criado.
    /// </summary>
    private static async Task CriarAdminAsync(UserManager<Usuario> usuarios, string? email, string? senha)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return;
        if (await usuarios.FindByEmailAsync(email) is not null)
            return;

        var admin = new Usuario { Nome = "Administrador", Email = email, UserName = email };
        var resultado = await usuarios.CreateAsync(admin, senha);
        if (!resultado.Succeeded)
            throw new InvalidOperationException(
                "Não foi possível criar o administrador: " + string.Join(" ", resultado.Errors.Select(e => e.Description)));
        await usuarios.AddToRolesAsync(admin, [Perfis.Admin, Perfis.Cliente]);
    }
}
