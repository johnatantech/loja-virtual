import { CurrencyPipe } from '@angular/common';
import { Component, inject, input, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { switchMap, tap } from 'rxjs';
import { QUANTIDADE_MAXIMA } from '../../carrinho/carrinho.models';
import { CarrinhoService } from '../../carrinho/carrinho.service';
import { Carregavel, ProdutoDetalhe as Produto } from '../catalogo.models';
import { CatalogoService, comoCarregavel } from '../catalogo.service';
import { ProdutoImagem } from '../produto-imagem/produto-imagem';

@Component({
  selector: 'app-produto-detalhe',
  imports: [RouterLink, CurrencyPipe, ProdutoImagem],
  templateUrl: './produto-detalhe.html',
  styleUrl: './produto-detalhe.scss',
})
export class ProdutoDetalhe {
  private readonly catalogo = inject(CatalogoService);
  protected readonly carrinho = inject(CarrinhoService);

  /** Vem do parâmetro da rota (/produto/:slug) via withComponentInputBinding. */
  readonly slug = input.required<string>();

  protected readonly quantidade = signal(1);
  protected readonly retorno = signal<{ tipo: 'ok' | 'erro'; texto: string } | null>(null);

  protected readonly estado = toSignal(
    toObservable(this.slug).pipe(
      tap(() => {
        this.quantidade.set(1);
        this.retorno.set(null);
      }),
      switchMap((slug) => comoCarregavel(this.catalogo.obterProduto(slug))),
    ),
    { initialValue: { tipo: 'carregando' } as Carregavel<Produto> },
  );

  /** Quantas unidades ainda cabem: o menor entre estoque e limite, descontando o que já está no carrinho. */
  protected disponivel(produto: Produto): number {
    return Math.max(0, Math.min(produto.estoque, QUANTIDADE_MAXIMA) - this.carrinho.quantidadeDe(produto.id));
  }

  protected opcoes(produto: Produto): number[] {
    return Array.from({ length: this.disponivel(produto) }, (_, i) => i + 1);
  }

  protected async adicionar(produto: Produto): Promise<void> {
    const erro = await this.carrinho.adicionar(produto.id, this.quantidade());
    this.retorno.set(erro ? { tipo: 'erro', texto: erro } : { tipo: 'ok', texto: 'Adicionado ao carrinho.' });
    this.quantidade.set(1);
  }
}
