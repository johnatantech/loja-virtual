import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of, shareReplay, startWith } from 'rxjs';
import { Carregavel, Categoria, FiltroProdutos, Pagina, ProdutoDetalhe, ProdutoResumo } from './catalogo.models';

@Injectable({ providedIn: 'root' })
export class CatalogoService {
  private readonly http = inject(HttpClient);

  /** As categorias mudam pouco: uma requisição serve a sessão inteira. */
  readonly categorias$ = this.http.get<Categoria[]>('/api/categorias').pipe(shareReplay(1));

  listarProdutos(filtro: FiltroProdutos): Observable<Pagina<ProdutoResumo>> {
    let params = new HttpParams();
    for (const [chave, valor] of Object.entries(filtro)) {
      if (valor !== undefined && valor !== null && valor !== '') params = params.set(chave, String(valor));
    }
    return this.http.get<Pagina<ProdutoResumo>>('/api/produtos', { params });
  }

  obterProduto(slug: string): Observable<ProdutoDetalhe> {
    return this.http.get<ProdutoDetalhe>(`/api/produtos/${encodeURIComponent(slug)}`);
  }
}

/** Converte uma requisição em estados de tela: carregando, pronto, não encontrado ou erro. */
export function comoCarregavel<T>(origem: Observable<T>): Observable<Carregavel<T>> {
  return origem.pipe(
    map((dados): Carregavel<T> => ({ tipo: 'pronto', dados })),
    catchError((erro: unknown) =>
      of<Carregavel<T>>(
        erro instanceof HttpErrorResponse && erro.status === 404 ? { tipo: 'nao-encontrado' } : { tipo: 'erro' },
      ),
    ),
    startWith<Carregavel<T>>({ tipo: 'carregando' }),
  );
}
