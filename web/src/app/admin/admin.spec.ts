import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { adminGuard } from '../core/auth/admin.guard';
import { provideLocalizacaoBrasil } from '../core/localizacao';
import { ProdutosAdmin } from './produtos/produtos-admin';

const sessao = (perfis: string[]) => JSON.stringify({
  token: 't', expiraEm: new Date(Date.now() + 3_600_000).toISOString(),
  usuario: { id: '1', nome: 'Ana', email: 'ana@exemplo.com', perfis },
});

const produto = { id: 7, nome: 'Tapete de Yoga', slug: 'tapete-de-yoga', descricao: '', preco: 79.9, estoque: 3,
  ativo: true, categoriaId: 4, categoria: 'Esporte e Lazer', imagemUrl: null };

describe('Painel administrativo', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting(), provideLocalizacaoBrasil()],
    });
  });

  function guard(url = '/admin') {
    const resultado = TestBed.runInInjectionContext(() => adminGuard({} as never, { url } as never));
    return typeof resultado === 'boolean' ? resultado : TestBed.inject(Router).serializeUrl(resultado as never);
  }

  it('libera o painel só para quem tem o perfil Admin', () => {
    expect(guard()).toBe('/entrar?retorno=%2Fadmin');

    localStorage.setItem('loja.sessao', sessao(['Cliente']));
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });
    expect(guard()).toBe('/');

    localStorage.setItem('loja.sessao', sessao(['Admin', 'Cliente']));
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()] });
    expect(guard()).toBe(true);
  });

  it('repor estoque atualiza só a linha do produto', async () => {
    const fixture = TestBed.createComponent(ProdutosAdmin);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne((r) => r.url === '/api/admin/produtos').flush({ itens: [produto], numeroPagina: 1, tamanhoPagina: 20, total: 1, totalPaginas: 1 });
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const input = el.querySelector<HTMLInputElement>('.repor input')!;
    input.value = '5';
    el.querySelector<HTMLButtonElement>('.repor button')!.click();

    const req = http.expectOne('/api/admin/produtos/7/estoque');
    expect(req.request.body).toEqual({ quantidade: 5 });
    req.flush({ ...produto, estoque: 8 });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).toContain('Estoque de "Tapete de Yoga" atualizado.');
    expect(el.querySelector('tbody td.numero:nth-child(4)')!.textContent!.trim()).toBe('8');
  });

  it('não envia reposição com quantidade inválida', async () => {
    const fixture = TestBed.createComponent(ProdutosAdmin);
    const http = TestBed.inject(HttpTestingController);
    http.expectOne((r) => r.url === '/api/admin/produtos').flush({ itens: [produto], numeroPagina: 1, tamanhoPagina: 20, total: 1, totalPaginas: 1 });
    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    el.querySelector<HTMLInputElement>('.repor input')!.value = '-2';

    // O navegador barra o envio pelo min="1" do campo...
    el.querySelector<HTMLButtonElement>('.repor button')!.click();
    http.expectNone('/api/admin/produtos/7/estoque');

    // ...e, se a validação do navegador for contornada, o componente também barra.
    el.querySelector('.repor')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
    fixture.detectChanges();
    http.expectNone('/api/admin/produtos/7/estoque');
    expect(el.textContent).toContain('quantidade inteira maior que zero');
  });
});
