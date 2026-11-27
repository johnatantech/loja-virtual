import { Routes } from '@angular/router';
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
    path: 'minha-conta',
    title: 'Minha conta | Loja Virtual',
    canActivate: [authGuard],
    loadComponent: () => import('./contas/minha-conta/minha-conta').then((m) => m.MinhaConta),
  },
  {
    path: '**',
    title: 'Página não encontrada | Loja Virtual',
    loadComponent: () => import('./layout/nao-encontrada/nao-encontrada').then((m) => m.NaoEncontrada),
  },
];
