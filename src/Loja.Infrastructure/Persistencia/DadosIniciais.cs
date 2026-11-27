using Loja.Domain.Catalogo;
using Microsoft.EntityFrameworkCore;

namespace Loja.Infrastructure.Persistencia;

/// <summary>Catálogo de demonstração, inserido só quando o banco está vazio.</summary>
public static class DadosIniciais
{
    public static async Task PopularAsync(LojaDbContext db, CancellationToken ct = default)
    {
        if (await db.Categorias.AnyAsync(ct))
            return;

        var eletronicos = new Categoria("Eletrônicos");
        var livros = new Categoria("Livros");
        var casa = new Categoria("Casa e Cozinha");
        var esporte = new Categoria("Esporte e Lazer");
        db.Categorias.AddRange(eletronicos, livros, casa, esporte);
        await db.SaveChangesAsync(ct);

        db.Produtos.AddRange(
            new Produto("Fone de Ouvido Bluetooth", "Cancelamento de ruído e 30 horas de bateria.", 349.90m, 25, eletronicos.Id),
            new Produto("Teclado Mecânico", "Switches táteis, layout ABNT2 e iluminação RGB.", 459.00m, 12, eletronicos.Id),
            new Produto("Mouse Sem Fio", "Sensor de 16.000 DPI e bateria recarregável.", 189.90m, 40, eletronicos.Id),
            new Produto("Monitor 27 Polegadas", "Resolução QHD, 144 Hz e painel IPS.", 1899.00m, 5, eletronicos.Id),
            new Produto("Código Limpo", "Clássico sobre boas práticas de programação.", 89.90m, 30, livros.Id),
            new Produto("Arquitetura Limpa", "Princípios de design de software duradouro.", 94.50m, 18, livros.Id),
            new Produto("Python para Análise de Dados", "Pandas, NumPy e Jupyter na prática.", 129.00m, 22, livros.Id),
            new Produto("Cafeteira Italiana", "Alumínio, 6 xícaras, para fogão a gás e elétrico.", 119.90m, 15, casa.Id),
            new Produto("Jogo de Panelas Antiaderente", "5 peças com tampas de vidro.", 399.00m, 8, casa.Id),
            new Produto("Garrafa Térmica 1 Litro", "Mantém a temperatura por até 24 horas.", 99.90m, 35, esporte.Id),
            new Produto("Tapete de Yoga", "Antiderrapante, 6 mm de espessura.", 79.90m, 20, esporte.Id),
            new Produto("Bicicleta Aro 29", "Quadro de alumínio, 21 marchas e freio a disco.", 1590.00m, 3, esporte.Id));
        await db.SaveChangesAsync(ct);
    }
}
