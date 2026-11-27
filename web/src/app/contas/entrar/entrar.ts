import { Component, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { lerErro } from '../../core/erros';

@Component({
  selector: 'app-entrar',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './entrar.html',
  styleUrl: '../contas.scss',
})
export class Entrar {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Query params da rota: para onde voltar e se a sessão anterior expirou. */
  readonly retorno = input<string>();
  readonly expirou = input<string>();

  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    senha: ['', Validators.required],
  });

  protected async enviar(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.enviando.set(true);
    this.erro.set(null);
    try {
      const { email, senha } = this.form.getRawValue();
      await this.auth.entrar(email, senha);
      await this.router.navigateByUrl(destinoSeguro(this.retorno()));
    } catch (e) {
      this.erro.set(lerErro(e).mensagem);
    } finally {
      this.enviando.set(false);
    }
  }
}

/** Só aceita caminhos internos, para um link malicioso não redirecionar para outro site. */
export function destinoSeguro(retorno: string | undefined): string {
  return retorno?.startsWith('/') && !retorno.startsWith('//') ? retorno : '/';
}
