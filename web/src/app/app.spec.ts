import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  function criar() {
    TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const fixture = TestBed.createComponent(App);
    const http = TestBed.inject(HttpTestingController);
    return { fixture, http };
  }

  it('mostra as categorias retornadas pela API', async () => {
    const { fixture, http } = criar();
    http.expectOne('/api/categorias').flush([
      { id: 1, nome: 'Livros', slug: 'livros', quantidadeProdutos: 3 },
      { id: 2, nome: 'Esporte', slug: 'esporte', quantidadeProdutos: 1 },
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const texto = fixture.nativeElement.textContent as string;
    expect(texto).toContain('Livros');
    expect(texto).toContain('3 produtos');
    expect(texto).toContain('1 produto');
  });

  it('avisa quando a API está fora do ar', async () => {
    const { fixture, http } = criar();
    http.expectOne('/api/categorias').flush('erro', { status: 500, statusText: 'Erro' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeTruthy();
  });
});
