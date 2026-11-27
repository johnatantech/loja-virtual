import { Routes } from '@angular/router';
import { adminGuard } from './core/auth/admin.guard';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    title: 'Loja Virtual',
    loadComponent: () => import('./catalogo/vitrine/vitrine').then((m) => m.Vitrine),
  },
  {
    path: 'produto/:slug',
    title: 'Produto | Loja Virtual',
    loadComponent: () => import('./catalogo/produto-detalhe/produto-detalhe').then((m) => m.ProdutoDetalhe),
  },
  {
    path: 'carrinho',
    title: 'Carrinho | Loja Virtual',
    loadComponent: () => import('./carrinho/carrinho-pagina').then((m) => m.CarrinhoPagina),
  },
  {
    path: 'entrar',
    title: 'Entrar | Loja Virtual',
    loadComponent: () => import('./contas/entrar/entrar').then((m) => m.Entrar),
  },
  {
    path: 'cadastro',
    title: 'Criar conta | Loja Virtual',
    loadComponent: () => import('./contas/cadastro/cadastro').then((m) => m.Cadastro),
  },
  {
    path: 'checkout',
    title: 'Finalizar compra | Loja Virtual',
    canActivate: [authGuard],
    loadComponent: () => import('./pedidos/checkout/checkout').then((m) => m.Checkout),
  },
  {
    path: 'pedidos',
    title: 'Meus pedidos | Loja Virtual',
    canActivate: [authGuard],
    loadComponent: () => import('./pedidos/lista/pedidos-lista').then((m) => m.PedidosLista),
  },
  {
    path: 'pedidos/:id',
    title: 'Pedido | Loja Virtual',
    canActivate: [authGuard],
    loadComponent: () => import('./pedidos/detalhe/pedido-detalhe').then((m) => m.PedidoDetalhe),
  },
  {
    path: 'pagamento-simulado/:id',
    title: 'Pagamento | Loja Virtual',
    canActivate: [authGuard],
    loadComponent: () => import('./pedidos/pagamento-simulado/pagamento-simulado').then((m) => m.PagamentoSimulado),
  },
  {
    path: 'minha-conta',
    title: 'Minha conta | Loja Virtual',
    canActivate: [authGuard],
    loadComponent: () => import('./contas/minha-conta/minha-conta').then((m) => m.MinhaConta),
  },
  {
    path: 'admin',
    title: 'Painel | Loja Virtual',
    canActivate: [adminGuard],
    loadComponent: () => import('./admin/admin-layout').then((m) => m.AdminLayout),
    children: [
      { path: '', loadComponent: () => import('./admin/painel/painel').then((m) => m.Painel) },
      { path: 'produtos', loadComponent: () => import('./admin/produtos/produtos-admin').then((m) => m.ProdutosAdmin) },
      { path: 'produtos/:id', loadComponent: () => import('./admin/produtos/produto-form').then((m) => m.ProdutoForm) },
      { path: 'pedidos', loadComponent: () => import('./admin/pedidos/pedidos-admin').then((m) => m.PedidosAdmin) },
      { path: 'pedidos/:id', loadComponent: () => import('./admin/pedidos/pedidos-admin').then((m) => m.PedidoAdminDetalhe) },
      { path: 'clientes', loadComponent: () => import('./admin/clientes/clientes-admin').then((m) => m.ClientesAdmin) },
    ],
  },
  {
    path: '**',
    title: 'Página não encontrada | Loja Virtual',
    loadComponent: () => import('./layout/nao-encontrada/nao-encontrada').then((m) => m.NaoEncontrada),
  },
];
