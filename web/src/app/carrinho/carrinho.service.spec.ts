import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Carrinho } from './carrinho.models';
import { CarrinhoService } from './carrinho.service';

const cotacao = (quantidade: number): Carrinho => ({
  itens: [{
    produtoId: 5, nome: 'Código Limpo', slug: 'codigo-limpo', categoriaSlug: 'livros', imagemUrl: null,
    precoUnitario: 89.9, quantidade, estoqueDisponivel: 30, subtotal: 89.9 * quantidade, aviso: null,
  }],
  quantidadeTotal: quantidade,
  total: 89.9 * quantidade,
  podeFinalizar: true,
});

describe('CarrinhoService (visitante)', () => {
  let servico: CarrinhoService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    servico = TestBed.inject(CarrinhoService);
    http = TestBed.inject(HttpTestingController);
    TestBed.tick(); // roda o efeito inicial (carrinho vazio não chama a API)
  });

  it('guarda os itens no navegador e precifica pela API', async () => {
    const operacao = servico.adicionar(5, 2);
    const req = http.expectOne('/api/carrinho/cotacao');
    expect(req.request.body).toEqual({ itens: [{ produtoId: 5, quantidade: 2 }] });
    req.flush(cotacao(2));

    expect(await operacao).toBeNull();
    expect(servico.quantidadeTotal()).toBe(2);
    expect(JSON.parse(localStorage.getItem('loja.carrinho')!)).toEqual([{ produtoId: 5, quantidade: 2 }]);
  });

  it('não passa do limite de 10 unidades por produto', async () => {
    localStorage.setItem('loja.carrinho', JSON.stringify([{ produtoId: 5, quantidade: 9 }]));
    const operacao = servico.definirQuantidade(5, 15);
    const req = http.expectOne('/api/carrinho/cotacao');
    expect(req.request.body.itens[0].quantidade).toBe(10);
    req.flush(cotacao(10));
    await operacao;
  });

  it('devolve a mensagem da API quando algo falha', async () => {
    const operacao = servico.adicionar(5);
    http.expectOne('/api/carrinho/cotacao').flush({ title: 'Serviço indisponível.' }, { status: 503, statusText: 'x' });
    expect(await operacao).toBe('Serviço indisponível.');
  });
});
