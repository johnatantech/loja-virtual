import { Component, computed, effect, input, signal } from '@angular/core';

const CORES: Record<string, string> = {
  eletronicos: '#2b4c7e',
  livros: '#7a3e48',
  'casa-e-cozinha': '#8a6a2f',
  'esporte-e-lazer': '#2f6b4f',
};

/**
 * Mostra a foto do produto. Sem foto cadastrada, ou se ela não carregar,
 * exibe uma capa gerada com as iniciais e a cor da categoria: a vitrine nunca
 * mostra imagem quebrada.
 */
@Component({
  selector: 'app-produto-imagem',
  template: `
    @if (imagemUrl() && !falhou()) {
      <!-- width/height reservam o espaço da foto e evitam que a página "pule" ao carregar -->
      <img [src]="imagemUrl()" [alt]="nome()" width="800" height="600" loading="lazy" decoding="async" (error)="falhou.set(true)" />
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

  /** Marca que a foto atual falhou; volta a tentar se o endereço mudar. */
  protected readonly falhou = signal(false);

  constructor() {
    effect(() => {
      this.imagemUrl();
      this.falhou.set(false);
    });
  }

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
