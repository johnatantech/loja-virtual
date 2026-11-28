import { TestBed } from '@angular/core/testing';
import { ProdutoImagem } from './produto-imagem';

describe('Imagem do produto', () => {
  function criar(imagemUrl: string | null) {
    const fixture = TestBed.createComponent(ProdutoImagem);
    fixture.componentRef.setInput('nome', 'Cafeteira Italiana');
    fixture.componentRef.setInput('categoriaSlug', 'casa-e-cozinha');
    fixture.componentRef.setInput('imagemUrl', imagemUrl);
    fixture.detectChanges();
    return fixture;
  }

  it('mostra a foto quando o produto tem imagem', () => {
    const el = criar('/produtos/cafeteira-italiana.webp').nativeElement as HTMLElement;
    expect(el.querySelector('img')?.getAttribute('src')).toBe('/produtos/cafeteira-italiana.webp');
  });

  it('sem foto, mostra a capa com as iniciais', () => {
    const el = criar(null).nativeElement as HTMLElement;
    expect(el.querySelector('img')).toBeNull();
    expect(el.textContent).toContain('CI');
  });

  it('se a foto falhar ao carregar, volta para a capa', async () => {
    const fixture = criar('/produtos/inexistente.webp');
    const el = fixture.nativeElement as HTMLElement;
    el.querySelector('img')!.dispatchEvent(new Event('error'));
    await fixture.whenStable();
    fixture.detectChanges();
    expect(el.querySelector('img')).toBeNull();
    expect(el.textContent).toContain('CI');
  });
});
