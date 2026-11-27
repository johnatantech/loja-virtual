import { Component, computed, input } from '@angular/core';

const CORES: Record<string, string> = {
  eletronicos: '#2b4c7e',
  livros: '#7a3e48',
  'casa-e-cozinha': '#8a6a2f',
  'esporte-e-lazer': '#2f6b4f',
};

/**
 * Mostra a foto do produto ou, enquanto não houver fotos cadastradas,
 * uma capa gerada com as iniciais e a cor da categoria.
 */
@Component({
  selector: 'app-produto-imagem',
  template: `
    @if (imagemUrl(); as url) {
      <img [src]="url" [alt]="nome()" loading="lazy" />
    } @else {
      <div class="capa" [style.--cor]="cor()" role="img" [attr.aria-label]="nome()">
        <span aria-hidden="true">{{ iniciais() }}</span>
      </div>
    }
  `,
  styleUrl: './produto-imagem.scss',
})
export class ProdutoImagem {
  readonly nome = input.required<string>();
  readonly categoriaSlug = input.required<string>();
  readonly imagemUrl = input<string | null>(null);

  protected readonly cor = computed(() => CORES[this.categoriaSlug()] ?? '#4a5560');
  protected readonly iniciais = computed(() =>
    this.nome()
      .split(/\s+/)
      .filter((p) => p.length > 2)
      .slice(0, 2)
      .map((p) => p[0].toUpperCase())
      .join(''),
  );
}
