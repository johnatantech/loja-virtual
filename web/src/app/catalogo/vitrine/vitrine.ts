import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { map, switchMap } from 'rxjs';
import { Carregavel, FiltroProdutos, Ordenacao, Pagina, ProdutoResumo } from '../catalogo.models';
import { CatalogoService, comoCarregavel } from '../catalogo.service';
import { ProdutoCard } from '../produto-card/produto-card';

const TAMANHO_PAGINA = 12;
const ORDENACOES: { valor: Ordenacao; rotulo: string }[] = [
  { valor: 'nome', rotulo: 'Nome (A a Z)' },
  { valor: 'menor-preco', rotulo: 'Menor preço' },
  { valor: 'maior-preco', rotulo: 'Maior preço' },
];

/** Os filtros vivem na URL: dá para recarregar, voltar e compartilhar um link já filtrado. */
function lerFiltro(params: ParamMap): FiltroProdutos {
  const numero = (chave: string) => {
    const valor = Number(params.get(chave));
    return params.has(chave) && Number.isFinite(valor) ? valor : undefined;
  };
  const ordenacao = params.get('ordenacao');
  return {
    busca: params.get('busca') ?? undefined,
    categoria: params.get('categoria') ?? undefined,
    precoMin: numero('precoMin'),
    precoMax: numero('precoMax'),
    ordenacao: ORDENACOES.some((o) => o.valor === ordenacao) ? (ordenacao as Ordenacao) : undefined,
    pagina: Math.max(1, numero('pagina') ?? 1),
    tamanhoPagina: TAMANHO_PAGINA,
  };
}

@Component({
  selector: 'app-vitrine',
  imports: [RouterLink, ProdutoCard, CurrencyPipe],
  templateUrl: './vitrine.html',
  styleUrl: './vitrine.scss',
})
export class Vitrine {
  private readonly catalogo = inject(CatalogoService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly ordenacoes = ORDENACOES;
  protected readonly categorias = toSignal(this.catalogo.categorias$, { initialValue: [] });

  protected readonly filtro = toSignal(this.route.queryParamMap.pipe(map(lerFiltro)), {
    initialValue: lerFiltro(this.route.snapshot.queryParamMap),
  });

  protected readonly estado = toSignal(
    this.route.queryParamMap.pipe(
      map(lerFiltro),
      switchMap((filtro) => comoCarregavel(this.catalogo.listarProdutos(filtro))),
    ),
    { initialValue: { tipo: 'carregando' } as Carregavel<Pagina<ProdutoResumo>> },
  );

  protected readonly temFiltros = computed(() => {
    const f = this.filtro();
    return !!(f.busca || f.categoria || f.precoMin !== undefined || f.precoMax !== undefined);
  });

  protected readonly titulo = computed(() => {
    const f = this.filtro();
    if (f.busca) return `Resultados para "${f.busca}"`;
    return this.categorias().find((c) => c.slug === f.categoria)?.nome ?? 'Todos os produtos';
  });

  /** Faixa de preço ativa, para o título deixar claro que há um filtro aplicado. */
  protected readonly faixaPreco = computed(() => {
    const { precoMin, precoMax } = this.filtro();
    if (precoMin === undefined && precoMax === undefined) return null;
    return { min: precoMin, max: precoMax };
  });

  protected paginas(total: number): number[] {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  protected aplicarPreco(min: string, max: string): void {
    this.atualizar({ precoMin: min || null, precoMax: max || null });
  }

  protected ordenar(ordenacao: string): void {
    this.atualizar({ ordenacao: ordenacao === 'nome' ? null : ordenacao });
  }

  protected irParaPagina(pagina: number): void {
    this.atualizar({ pagina: pagina === 1 ? null : pagina }, false);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  /** Mescla os parâmetros na URL. Mudar um filtro sempre volta para a página 1. */
  private atualizar(parametros: Record<string, string | number | null>, voltarParaPrimeira = true): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: voltarParaPrimeira ? { ...parametros, pagina: null } : parametros,
      queryParamsHandling: 'merge',
    });
  }
}
