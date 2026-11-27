import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { Pagina } from '../../catalogo/catalogo.models';
import { lerErro } from '../../core/erros';
import { ClienteAdmin } from '../admin.models';
import { AdminService } from '../admin.service';

@Component({
  selector: 'app-clientes-admin',
  imports: [DatePipe],
  template: `
    <h1>Clientes</h1>
    <form class="busca" (submit)="$event.preventDefault(); buscar(termo.value)">
      <input #termo type="search" placeholder="Buscar por nome ou e-mail" aria-label="Buscar clientes" />
      <button type="submit" class="botao-secundario">Buscar</button>
    </form>

    @if (mensagem(); as m) {
      <p [class]="m.tipo" role="status">{{ m.texto }}</p>
    }

    @if (pagina(); as p) {
      <div class="tabela">
        <table>
          <thead><tr><th>Nome</th><th>E-mail</th><th>Cadastro</th><th>Perfil</th><th class="numero">Pedidos</th><th>Acesso</th></tr></thead>
          <tbody>
            @for (c of p.itens; track c.id) {
              <tr>
                <td>{{ c.nome }}</td>
                <td>{{ c.email }}</td>
                <td>{{ c.criadoEm | date: 'dd/MM/yyyy' }}</td>
                <td>{{ c.perfis.includes('Admin') ? 'Admin' : 'Cliente' }}</td>
                <td class="numero">{{ c.quantidadePedidos }}</td>
                <td>
                  @if (c.bloqueadoAte) {
                    <span class="bloqueado">Bloqueado até {{ c.bloqueadoAte | date: 'HH:mm' }}</span>
                    <button type="button" class="botao-secundario botao-pequeno" [disabled]="ocupado()" (click)="desbloquear(c)">Desbloquear</button>
                  } @else {
                    <span class="suave">Liberado</span>
                  }
                </td>
              </tr>
            } @empty {
              <tr><td colspan="6" class="suave">Nenhum cliente encontrado.</td></tr>
            }
          </tbody>
        </table>
      </div>
    } @else {
      <p class="suave">Carregando...</p>
    }
  `,
  styleUrls: ['../admin.scss'],
  styles: `
    .busca { display: flex; gap: 0.5rem; max-width: 420px; margin-bottom: 1.25rem; }
    .busca input { flex: 1; }
    .bloqueado { color: var(--alerta); font-weight: 600; margin-right: 0.5rem; }
  `,
})
export class ClientesAdmin {
  private readonly admin = inject(AdminService);
  protected readonly pagina = signal<Pagina<ClienteAdmin> | null>(null);
  protected readonly ocupado = signal(false);
  protected readonly mensagem = signal<{ tipo: 'sucesso' | 'alerta'; texto: string } | null>(null);
  private busca = '';

  constructor() {
    void this.carregar();
  }

  protected buscar(termo: string): void {
    this.busca = termo.trim();
    void this.carregar();
  }

  private async carregar(): Promise<void> {
    this.pagina.set(await firstValueFrom(this.admin.clientes(this.busca, 1)));
  }

  protected async desbloquear(cliente: ClienteAdmin): Promise<void> {
    this.ocupado.set(true);
    try {
      await firstValueFrom(this.admin.desbloquear(cliente.id));
      this.pagina.update((p) => p && { ...p, itens: p.itens.map((c) => (c.id === cliente.id ? { ...c, bloqueadoAte: null, tentativasFalhas: 0 } : c)) });
      this.mensagem.set({ tipo: 'sucesso', texto: `Acesso de ${cliente.email} liberado.` });
    } catch (e) {
      this.mensagem.set({ tipo: 'alerta', texto: lerErro(e).mensagem });
    } finally {
      this.ocupado.set(false);
    }
  }
}
