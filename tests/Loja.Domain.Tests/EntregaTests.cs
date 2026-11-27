using Loja.Domain.Comum;
using Loja.Domain.Entrega;

namespace Loja.Domain.Tests;

public class EntregaTests
{
    [Theory]
    [InlineData("01310-100", "SP")] // Av. Paulista
    [InlineData("20040002", "RJ")]
    [InlineData("70040-010", "DF")]
    [InlineData("69005-040", "AM")]
    [InlineData("90010-150", "RS")]
    public void Descobre_a_uf_pelo_cep(string cep, string uf)
    {
        Assert.Equal(uf, Cep.Criar(cep).Uf);
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("abcdefgh")]
    [InlineData("00000-000")]
    public void Rejeita_cep_invalido(string cep)
    {
        Assert.Throws<DomainException>(() => Cep.Criar(cep));
    }

    [Fact]
    public void Frete_mais_caro_quanto_mais_longe_de_SP()
    {
        var sp = CalculadoraFrete.Cotar(Cep.Criar("01310-100"), 100m);
        var norte = CalculadoraFrete.Cotar(Cep.Criar("69005-040"), 100m);
        Assert.True(norte.Valor > sp.Valor);
        Assert.True(norte.PrazoDias > sp.PrazoDias);
    }

    [Fact]
    public void Frete_gratis_a_partir_do_valor_minimo()
    {
        var abaixo = CalculadoraFrete.Cotar(Cep.Criar("01310-100"), 298.99m);
        var acima = CalculadoraFrete.Cotar(Cep.Criar("01310-100"), CalculadoraFrete.ValorParaFreteGratis);
        Assert.False(abaixo.Gratis);
        Assert.True(acima.Gratis);
        Assert.Equal(0m, acima.Valor);
    }

    [Fact]
    public void Endereco_usa_sempre_a_uf_do_cep()
    {
        var endereco = EnderecoEntrega.Criar("Maria", Cep.Criar("01310-100"), "Av. Paulista", "1000", null, "Bela Vista", "São Paulo");
        Assert.Equal("SP", endereco.Uf);
    }

    [Fact]
    public void Endereco_exige_os_campos_obrigatorios()
    {
        Assert.Throws<DomainException>(() =>
            EnderecoEntrega.Criar("Maria", Cep.Criar("01310-100"), "", "1000", null, "Bela Vista", "São Paulo"));
    }
}

public class MoedaTests
{
    [Theory]
    [InlineData(1234.5, "R$ 1.234,50")]
    [InlineData(0, "R$ 0,00")]
    [InlineData(89.9, "R$ 89,90")]
    public void Formata_em_reais_independente_da_cultura(decimal valor, string esperado)
    {
        Assert.Equal(esperado, Moeda.Formatar(valor));
    }
}
