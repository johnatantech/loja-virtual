using System.Net;
using System.Net.Http.Json;
using Loja.Application.Admin;
using Loja.Application.Catalogo;
using Loja.Application.Pedidos;

namespace Loja.Api.Tests;

[Collection("Api")]
public class AdminApiTests(ApiFactory factory)
{
    private static object NovoProduto(string nome, int categoriaId, decimal preco = 50m) =>
        new { nome, descricao = "Criado no teste", preco, categoriaId, imagemUrl = (string?)null, estoqueInicial = 4 };

    private async Task<int> CategoriaLivros() =>
        (await factory.CreateClient().GetFromJsonAsync<List<CategoriaDto>>("/api/categorias"))!.Single(c => c.Slug == "livros").Id;

    [Fact]
    public async Task Painel_exige_perfil_admin()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/admin/dashboard")).StatusCode);

        var (cliente, _) = await factory.CriarContaAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/admin/dashboard")).StatusCode);

        var admin = await factory.EntrarComoAdminAsync();
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/admin/dashboard")).StatusCode);
    }

    [Fact]
    public async Task Cria_edita_e_desativa_produto()
    {
        var admin = await factory.EntrarComoAdminAsync();
        var livros = await CategoriaLivros();
        var nome = $"Livro de Teste {Guid.NewGuid():N}"[..30];

        var criado = await (await admin.PostAsJsonAsync("/api/admin/produtos", NovoProduto(nome, livros)))
            .Content.ReadFromJsonAsync<ProdutoAdminDto>();
        Assert.Equal(4, criado!.Estoque);

        var editado = await (await admin.PutAsJsonAsync($"/api/admin/produtos/{criado.Id}", NovoProduto(nome + " 2", livros, 75m)))
            .Content.ReadFromJsonAsync<ProdutoAdminDto>();
        Assert.Equal(75m, editado!.Preco);
        Assert.EndsWith("-2", editado.Slug);

        await admin.PostAsync($"/api/admin/produtos/{criado.Id}/desativar", null);
        var naVitrine = await factory.CreateClient().GetAsync($"/api/produtos/{editado.Slug}");
        Assert.Equal(HttpStatusCode.NotFound, naVitrine.StatusCode); // some da loja, continua no painel

        var noPainel = await admin.GetFromJsonAsync<ProdutoAdminDto>($"/api/admin/produtos/{criado.Id}");
        Assert.False(noPainel!.Ativo);
    }

    [Fact]
    public async Task Nao_permite_dois_produtos_com_o_mesmo_nome()
    {
        var admin = await factory.EntrarComoAdminAsync();
        var resposta = await admin.PostAsJsonAsync("/api/admin/produtos", NovoProduto("Código Limpo", await CategoriaLivros()));
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Repoe_estoque()
    {
        var admin = await factory.EntrarComoAdminAsync();
        var criado = await (await admin.PostAsJsonAsync("/api/admin/produtos",
            NovoProduto($"Reposicao {Guid.NewGuid():N}"[..25], await CategoriaLivros()))).Content.ReadFromJsonAsync<ProdutoAdminDto>();

        var reposto = await (await admin.PostAsJsonAsync($"/api/admin/produtos/{criado!.Id}/estoque", new { quantidade = 6 }))
            .Content.ReadFromJsonAsync<ProdutoAdminDto>();
        Assert.Equal(10, reposto!.Estoque);

        var invalido = await admin.PostAsJsonAsync($"/api/admin/produtos/{criado.Id}/estoque", new { quantidade = -3 });
        Assert.Equal(HttpStatusCode.BadRequest, invalido.StatusCode);
    }

    [Fact]
    public async Task Pedido_segue_a_maquina_de_estados_pelo_painel()
    {
        var (cliente, sessao) = await factory.CriarContaAsync();
        var livro = await cliente.GetFromJsonAsync<ProdutoDetalheDto>("/api/produtos/arquitetura-limpa");
        await cliente.PutAsJsonAsync($"/api/carrinho/itens/{livro!.Id}", new { quantidade = 1 });
        var pedido = await (await cliente.PostAsJsonAsync("/api/pedidos", new
        {
            destinatario = "Maria", cep = "01310-100", logradouro = "Av. Paulista", numero = "1", bairro = "Bela Vista", cidade = "São Paulo",
        })).Content.ReadFromJsonAsync<PedidoCriadoDto>();

        var admin = await factory.EntrarComoAdminAsync();

        // Não dá para enviar antes do pagamento.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync($"/api/admin/pedidos/{pedido!.Id}/enviar", null)).StatusCode);

        await cliente.PostAsJsonAsync($"/api/pagamentos/simulado/{pedido.Id}", new { aprovar = true });
        var aguardando = await admin.GetFromJsonAsync<Pagina<PedidoAdminResumoDto>>("/api/admin/pedidos?status=Pago");
        Assert.Contains(aguardando!.Itens, p => p.Id == pedido.Id && p.ClienteEmail == sessao.Usuario.Email);

        await admin.PostAsync($"/api/admin/pedidos/{pedido.Id}/enviar", null);
        var entregue = await (await admin.PostAsync($"/api/admin/pedidos/{pedido.Id}/entregar", null))
            .Content.ReadFromJsonAsync<PedidoDetalheDto>();
        Assert.Equal("Entregue", entregue!.Status);

        // Pedido entregue não pode mais ser cancelado.
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync($"/api/admin/pedidos/{pedido.Id}/cancelar", null)).StatusCode);
    }

    [Fact]
    public async Task Admin_desbloqueia_conta_bloqueada_por_tentativas()
    {
        var (_, sessao) = await factory.CriarContaAsync();
        var anonimo = factory.CreateClient();
        for (var i = 0; i < 5; i++)
            await anonimo.PostAsJsonAsync("/api/contas/login", new { email = sessao.Usuario.Email, senha = "Errada123" });

        var admin = await factory.EntrarComoAdminAsync();
        var clientes = await admin.GetFromJsonAsync<Pagina<ClienteAdminDto>>($"/api/admin/clientes?busca={sessao.Usuario.Email}");
        var bloqueado = Assert.Single(clientes!.Itens);
        Assert.NotNull(bloqueado.BloqueadoAte);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/admin/clientes/{bloqueado.Id}/desbloquear", null)).StatusCode);

        var login = await anonimo.PostAsJsonAsync("/api/contas/login", new { email = sessao.Usuario.Email, senha = ApiCliente.SenhaValida });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Dashboard_mostra_estoque_baixo_e_pedidos_por_status()
    {
        var admin = await factory.EntrarComoAdminAsync();
        var painel = await admin.GetFromJsonAsync<DashboardDto>("/api/admin/dashboard");

        Assert.All(painel!.EstoqueBaixo, p => Assert.True(p.Estoque <= 5));
        Assert.Contains("Pago", painel.PedidosPorStatus.Keys);
        Assert.True(painel.FaturamentoTotal >= 0);
    }
}
