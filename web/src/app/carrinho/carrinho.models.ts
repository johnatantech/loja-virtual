export interface ItemCarrinho {
  produtoId: number;
  nome: string;
  slug: string;
  categoriaSlug: string;
  imagemUrl: string | null;
  precoUnitario: number;
  quantidade: number;
  estoqueDisponivel: number;
  subtotal: number;
  aviso: string | null;
}

export interface Carrinho {
  itens: ItemCarrinho[];
  quantidadeTotal: number;
  total: number;
  podeFinalizar: boolean;
}

export interface ItemSolicitado {
  produtoId: number;
  quantidade: number;
}

export const QUANTIDADE_MAXIMA = 10;
export const CARRINHO_VAZIO: Carrinho = { itens: [], quantidadeTotal: 0, total: 0, podeFinalizar: false };
