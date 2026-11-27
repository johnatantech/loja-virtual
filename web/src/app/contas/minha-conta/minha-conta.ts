import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-minha-conta',
  imports: [RouterLink],
  template: `
    @if (auth.usuario(); as usuario) {
      <h1>Olá, {{ usuario.nome }}</h1>
      <dl>
        <dt>E-mail</dt>
        <dd>{{ usuario.email }}</dd>
        <dt>Perfil</dt>
        <dd>{{ usuario.perfis.join(', ') }}</dd>
      </dl>
      <section>
        <h2>Meus pedidos</h2>
        <p class="vazio">Acompanhe o status e os detalhes das suas compras.</p>
        <a routerLink="/pedidos" class="botao-secundario">Ver meus pedidos</a>
      </section>
      <button type="button" class="botao-secundario" (click)="sair()">Sair da conta</button>
    }
  `,
  styles: `
    :host { display: block; max-width: 640px; }
    h1 { margin: 0 0 1.5rem; letter-spacing: -0.02em; }
    dl { display: grid; grid-template-columns: max-content 1fr; gap: 0.5rem 1.5rem; margin: 0 0 2rem; }
    dt { color: var(--texto-suave); }
    dd { margin: 0; font-weight: 600; }
    h2 { font-size: 1.1rem; margin: 0 0 0.5rem; }
    .vazio { color: var(--texto-suave); margin: 0 0 0.75rem; }
    section { margin-bottom: 2.5rem; }
  `,
})
export class MinhaConta {
  protected readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected sair(): void {
    this.auth.encerrar();
    void this.router.navigateByUrl('/');
  }
}
