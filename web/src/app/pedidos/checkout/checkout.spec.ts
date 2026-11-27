import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { CarrinhoService } from '../../carrinho/carrinho.service';
import { provideLocalizacaoBrasil } from '../../core/localizacao';
import { Checkout } from './checkout';

const carrinho = {
  itens: [{ produtoId: 5, nome: 'Código Limpo', slug: 'codigo-limpo', categoriaSlug: 'livros', imagemUrl: null,
    precoUnitario: 89.9, quantidade: 1, estoqueDisponivel: 30, subtotal: 89.9, aviso: null }],
  quantidadeTotal: 1, total: 89.9, podeFinalizar: true,
};

describe('Checkout', () => {
  async function abrir() {
    localStorage.clear();
    localStorage.setItem('loja.carrinho', JSON.stringify([{ produtoId: 5, quantidade: 1 }]));
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'checkout', component: Checkout }, { path: '**', children: [] }]),
        provideHttpClient(), provideHttpClientTesting(), provideLocalizacaoBrasil(),
      ],
    });
    const http = TestBed.inject(HttpTestingController);
    TestBed.inject(CarrinhoService);
    TestBed.tick();
    http.expectOne('/api/carrinho/cotacao').flush(carrinho);
    const harness = await RouterTestingHarness.create('/checkout');
    return { harness, http, el: harness.routeNativeElement as HTMLElement };
  }

  function digitarCep(el: HTMLElement, cep: string) {
    const input = el.querySelector<HTMLInputElement>('input[formcontrolname="cep"]')!;
    input.value = cep;
    input.dispatchEvent(new Event('input'));
  }

  it('preenche o endereço pelo ViaCEP e mostra o frete calculado pela API', async () => {
    const { harness, http, el } = await abrir();
    digitarCep(el, '01310-100');

    http.expectOne('https://viacep.com.br/ws/01310100/json/')
      .flush({ logradouro: 'Avenida Paulista', bairro: 'Bela Vista', localidade: 'São Paulo', uf: 'SP' });
    http.expectOne((r) => r.url === '/api/checkout/frete' && r.params.get('cep') === '01310100')
      .flush({ uf: 'SP', subtotal: 89.9, valorFrete: 15.9, prazoDias: 2, freteGratis: false, total: 105.8 });
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(el.querySelector<HTMLInputElement>('input[formcontrolname="logradouro"]')!.value).toBe('Avenida Paulista');
    expect(el.textContent).toContain('Entrega em até 2 dias úteis');
    expect(el.textContent!.replace(/\s/g, ' ')).toMatch(/R\$\s?105,80/);
  });

  it('fecha o pedido e vai para a página de pagamento', async () => {
    const { harness, http, el } = await abrir();
    digitarCep(el, '01310100');
    http.expectOne((r) => r.url.startsWith('https://viacep.com.br/')).flush({ logradouro: 'Av. Paulista', bairro: 'Bela Vista', localidade: 'São Paulo', uf: 'SP' });
    http.expectOne((r) => r.url === '/api/checkout/frete')
      .flush({ uf: 'SP', subtotal: 89.9, valorFrete: 15.9, prazoDias: 2, freteGratis: false, total: 105.8 });
    await harness.fixture.whenStable();
    harness.detectChanges();

    for (const [campo, valor] of [['numero', '1000'], ['destinatario', 'Maria Souza']]) {
      const input = el.querySelector<HTMLInputElement>(`input[formcontrolname="${campo}"]`)!;
      input.value = valor;
      input.dispatchEvent(new Event('input'));
    }
    harness.detectChanges();

    const navegar = vi.spyOn(TestBed.inject(Router), 'navigateByUrl');
    (el.querySelector('aside .botao-principal') as HTMLButtonElement).click();

    const pedido = http.expectOne('/api/pedidos');
    expect(pedido.request.body).toMatchObject({ cep: '01310100', numero: '1000', logradouro: 'Av. Paulista' });
    pedido.flush({ id: 'abc', linkPagamento: '/pagamento-simulado/abc' });
    await Promise.resolve();
    http.expectOne('/api/carrinho/cotacao').flush({ itens: [], quantidadeTotal: 0, total: 0, podeFinalizar: false });
    await harness.fixture.whenStable();

    expect(navegar).toHaveBeenCalledWith('/pagamento-simulado/abc');
  });
});
