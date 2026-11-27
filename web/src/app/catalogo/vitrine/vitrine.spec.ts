import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideLocalizacaoBrasil } from '../../core/localizacao';
import { Pagina, ProdutoResumo } from '../catalogo.models';
import { Vitrine } from './vitrine';

const produto = (id: number, nome: string, preco: number): ProdutoResumo => ({
  id, nome, slug: `produto-${id}`, preco, imagemUrl: null,
  categoria: 'Livros', categoriaSlug: 'livros', emEstoque: true,
});

const pagina = (itens: ProdutoResumo[], totalPaginas = 1): Pagina<ProdutoResumo> => ({
  itens, numeroPagina: 1, tamanhoPagina: 12, total: itens.length, totalPaginas,
});

describe('Vitrine', () => {
  async function abrir(url: string) {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: '', component: Vitrine }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideLocalizacaoBrasil(),
      ],
    });
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/categorias').flush([
      { id: 1, nome: 'Livros', slug: 'livros', quantidadeProdutos: 2 },
    ]);
    return { harness, http };
  }

  it('envia os filtros da URL para a API', async () => {
    const { http } = await abrir('/?categoria=livros&precoMax=100&ordenacao=menor-preco');
    const req = http.expectOne((r) => r.url === '/api/produtos');
    expect(req.request.params.get('categoria')).toBe('livros');
    expect(req.request.params.get('precoMax')).toBe('100');
    expect(req.request.params.get('ordenacao')).toBe('menor-preco');
    expect(req.request.params.get('pagina')).toBe('1');
  });

  it('mostra os produtos e o total encontrado', async () => {
    const { harness, http } = await abrir('/');
    http.expectOne((r) => r.url === '/api/produtos').flush(pagina([
      produto(1, 'Código Limpo', 89.9),
      produto(2, 'Arquitetura Limpa', 94.5),
    ]));
    harness.detectChanges();

    const texto = harness.routeNativeElement!.textContent!;
    expect(texto).toContain('Código Limpo');
    expect(texto).toContain('2 produtos');
    expect(texto).toMatch(/R\$\s?89,90/);
  });

  it('orienta o usuário quando nada é encontrado', async () => {
    const { harness, http } = await abrir('/?busca=xyz');
    http.expectOne((r) => r.url === '/api/produtos').flush(pagina([]));
    harness.detectChanges();

    const texto = harness.routeNativeElement!.textContent!;
    expect(texto).toContain('Resultados para "xyz"');
    expect(texto).toContain('Nenhum produto encontrado');
  });

  it('mostra a paginação quando há mais de uma página', async () => {
    const { harness, http } = await abrir('/');
    http.expectOne((r) => r.url === '/api/produtos').flush(pagina([produto(1, 'A', 10)], 3));
    harness.detectChanges();

    const botoes = harness.routeNativeElement!.querySelectorAll('.paginacao button');
    expect(botoes.length).toBe(5); // anterior, 1, 2, 3, próxima
  });
});
