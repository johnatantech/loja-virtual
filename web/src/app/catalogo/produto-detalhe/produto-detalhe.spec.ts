import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideLocalizacaoBrasil } from '../../core/localizacao';
import { ProdutoDetalhe } from './produto-detalhe';

describe('ProdutoDetalhe', () => {
  async function abrir(slug: string) {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'produto/:slug', component: ProdutoDetalhe }], withComponentInputBinding()),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideLocalizacaoBrasil(),
      ],
    });
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(`/produto/${slug}`);
    return { harness, http: TestBed.inject(HttpTestingController) };
  }

  it('mostra preço em reais e alerta de poucas unidades', async () => {
    const { harness, http } = await abrir('teclado-mecanico');
    http.expectOne('/api/produtos/teclado-mecanico').flush({
      id: 2, nome: 'Teclado Mecânico', slug: 'teclado-mecanico', descricao: 'Switches táteis.',
      preco: 459, imagemUrl: null, categoria: 'Eletrônicos', categoriaSlug: 'eletronicos', estoque: 3,
    });
    harness.detectChanges();

    const texto = harness.routeNativeElement!.textContent!.replace(/\s/g, ' ');
    expect(texto).toContain('Teclado Mecânico');
    expect(texto).toMatch(/R\$\s?459,00/);
    expect(texto).toContain('Últimas 3 unidades');
  });

  it('mostra uma página amigável quando o produto não existe', async () => {
    const { harness, http } = await abrir('nao-existe');
    http.expectOne('/api/produtos/nao-existe').flush(
      { title: 'Produto não encontrado.' },
      { status: 404, statusText: 'Not Found' },
    );
    harness.detectChanges();

    expect(harness.routeNativeElement!.textContent).toContain('Produto não encontrado');
  });
});
