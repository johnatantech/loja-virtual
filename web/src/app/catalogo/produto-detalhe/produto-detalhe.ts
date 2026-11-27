import { CurrencyPipe } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
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

  /** Vem do parâmetro da rota (/produto/:slug) via withComponentInputBinding. */
  readonly slug = input.required<string>();

  protected readonly estado = toSignal(
    toObservable(this.slug).pipe(switchMap((slug) => comoCarregavel(this.catalogo.obterProduto(slug)))),
    { initialValue: { tipo: 'carregando' } as Carregavel<Produto> },
  );
}
