export interface Categoria {
  id: number;
  nome: string;
  slug: string;
  quantidadeProdutos: number;
}

export interface ProdutoResumo {
  id: number;
  nome: string;
  slug: string;
  preco: number;
  imagemUrl: string | null;
  categoria: string;
  categoriaSlug: string;
  emEstoque: boolean;
}

export interface ProdutoDetalhe {
  id: number;
  nome: string;
  slug: string;
  descricao: string;
  preco: number;
  imagemUrl: string | null;
  categoria: string;
  categoriaSlug: string;
  estoque: number;
}

export interface Pagina<T> {
  itens: T[];
  numeroPagina: number;
  tamanhoPagina: number;
  total: number;
  totalPaginas: number;
}

export type Ordenacao = 'nome' | 'menor-preco' | 'maior-preco';

export interface FiltroProdutos {
  busca?: string;
  categoria?: string;
  precoMin?: number;
  precoMax?: number;
  ordenacao?: Ordenacao;
  pagina: number;
  tamanhoPagina: number;
}

/** Estado de uma tela que carrega dados: evita flags soltas como "carregando" e "erro". */
export type Carregavel<T> =
  | { tipo: 'carregando' }
  | { tipo: 'pronto'; dados: T }
  | { tipo: 'nao-encontrado' }
  | { tipo: 'erro' };
