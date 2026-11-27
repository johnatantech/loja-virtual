import { CurrencyPipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProdutoResumo } from '../catalogo.models';
import { ProdutoImagem } from '../produto-imagem/produto-imagem';

@Component({
  selector: 'app-produto-card',
  imports: [RouterLink, CurrencyPipe, ProdutoImagem],
  template: `
    <a [routerLink]="['/produto', produto().slug]" class="card">
      <app-produto-imagem
        [nome]="produto().nome"
        [categoriaSlug]="produto().categoriaSlug"
        [imagemUrl]="produto().imagemUrl"
      />
      <span class="categoria">{{ produto().categoria }}</span>
      <h3>{{ produto().nome }}</h3>
      <p class="preco">{{ produto().preco | currency }}</p>
      @if (!produto().emEstoque) {
        <p class="esgotado">Esgotado</p>
      }
    </a>
  `,
  styleUrl: './produto-card.scss',
})
export class ProdutoCard {
  readonly produto = input.required<ProdutoResumo>();
}
