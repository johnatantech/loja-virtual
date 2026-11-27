import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProdutoImagem } from '../catalogo/produto-imagem/produto-imagem';
import { AuthService } from '../core/auth/auth.service';
import { ItemCarrinho, QUANTIDADE_MAXIMA } from './carrinho.models';
import { CarrinhoService } from './carrinho.service';

@Component({
  selector: 'app-carrinho-pagina',
  imports: [RouterLink, CurrencyPipe, ProdutoImagem],
  templateUrl: './carrinho-pagina.html',
  styleUrl: './carrinho-pagina.scss',
})
export class CarrinhoPagina {
  protected readonly carrinho = inject(CarrinhoService);
  protected readonly auth = inject(AuthService);
  protected readonly erro = signal<string | null>(null);

  /** Opções do seletor: até o limite por item e o estoque, sem esconder a quantidade atual. */
  protected opcoes(item: ItemCarrinho): number[] {
    const maximo = Math.max(Math.min(QUANTIDADE_MAXIMA, item.estoqueDisponivel), item.quantidade);
    return Array.from({ length: maximo }, (_, i) => i + 1);
  }

  protected async alterar(item: ItemCarrinho, valor: string): Promise<void> {
    this.erro.set(await this.carrinho.definirQuantidade(item.produtoId, Number(valor)));
  }

  protected async remover(item: ItemCarrinho): Promise<void> {
    this.erro.set(await this.carrinho.remover(item.produtoId));
  }
}
