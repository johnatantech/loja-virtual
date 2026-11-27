import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Cabecalho } from './layout/cabecalho/cabecalho';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Cabecalho],
  template: `
    <app-cabecalho />
    <main class="pagina">
      <router-outlet />
    </main>
    <footer class="rodape">
      Projeto de portfólio. Loja fictícia: nenhuma compra é real.
    </footer>
  `,
  styles: `
    :host { display: flex; flex-direction: column; min-height: 100vh; }
    .pagina { flex: 1; width: 100%; max-width: var(--largura); margin: 0 auto; padding: 2rem 1.25rem 4rem; }
    .rodape { border-top: 1px solid var(--borda); padding: 1.5rem 1.25rem; text-align: center;
              font-size: 0.85rem; color: var(--texto-suave); }
  `,
})
export class App {}
