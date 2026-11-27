import { StatusPedido } from '../pedidos/pedidos.models';

export interface Dashboard {
  faturamentoTotal: number;
  faturamentoUltimos30Dias: number;
  ticketMedio: number;
  pedidosAguardandoEnvio: number;
  pedidosPorStatus: Record<StatusPedido, number>;
  estoqueBaixo: { id: number; nome: string; estoque: number }[];
  maisVendidos: { produtoId: number; nome: string; quantidade: number; receita: number }[];
}

export interface ProdutoAdmin {
  id: number;
  nome: string;
  slug: string;
  descricao: string;
  preco: number;
  estoque: number;
  ativo: boolean;
  categoriaId: number;
  categoria: string;
  imagemUrl: string | null;
}

export interface SalvarProduto {
  nome: string;
  descricao: string;
  preco: number;
  categoriaId: number;
  imagemUrl: string | null;
  estoqueInicial: number;
}

export interface PedidoAdmin {
  id: string;
  criadoEm: string;
  status: StatusPedido;
  clienteNome: string;
  clienteEmail: string;
  quantidadeItens: number;
  total: number;
}

export interface ClienteAdmin {
  id: string;
  nome: string;
  email: string;
  criadoEm: string;
  perfis: string[];
  bloqueadoAte: string | null;
  tentativasFalhas: number;
  quantidadePedidos: number;
}
