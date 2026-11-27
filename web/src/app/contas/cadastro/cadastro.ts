import { Component, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { lerErro } from '../../core/erros';
import { destinoSeguro } from '../entrar/entrar';

/** Mesmas regras da API, para avisar antes de enviar. A API continua sendo quem decide. */
const SENHA_FORTE = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/;

@Component({
  selector: 'app-cadastro',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './cadastro.html',
  styleUrl: '../contas.scss',
})
export class Cadastro {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly retorno = input<string>();

  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);
  protected readonly errosApi = signal<Record<string, string[]>>({});

  protected readonly form = inject(FormBuilder).nonNullable.group({
    nome: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email]],
    senha: ['', [Validators.required, Validators.pattern(SENHA_FORTE)]],
  });

  protected invalido(campo: 'nome' | 'email' | 'senha'): boolean {
    const controle = this.form.controls[campo];
    return (controle.touched && controle.invalid) || !!this.errosApi()[campo];
  }

  protected async enviar(): Promise<void> {
    this.errosApi.set({});
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.enviando.set(true);
    this.erro.set(null);
    try {
      const { nome, email, senha } = this.form.getRawValue();
      await this.auth.cadastrar(nome, email, senha);
      await this.router.navigateByUrl(destinoSeguro(this.retorno()));
    } catch (e) {
      const { mensagem, campos } = lerErro(e);
      this.erro.set(mensagem);
      this.errosApi.set(campos);
    } finally {
      this.enviando.set(false);
    }
  }
}
