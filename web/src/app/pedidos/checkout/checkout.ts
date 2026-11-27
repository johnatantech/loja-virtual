import { CurrencyPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CarrinhoService } from '../../carrinho/carrinho.service';
import { AuthService } from '../../core/auth/auth.service';
import { lerErro } from '../../core/erros';
import { CotacaoFrete } from '../pedidos.models';
import { PedidosService, irParaPagamento } from '../pedidos.service';

@Component({
  selector: 'app-checkout',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss',
})
export class Checkout {
  private readonly pedidos = inject(PedidosService);
  private readonly router = inject(Router);
  protected readonly carrinho = inject(CarrinhoService);

  protected readonly buscandoCep = signal(false);
  protected readonly avisoCep = signal<string | null>(null);
  protected readonly frete = signal<CotacaoFrete | null>(null);
  protected readonly enviando = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    destinatario: [inject(AuthService).usuario()?.nome ?? '', [Validators.required, Validators.maxLength(100)]],
    cep: ['', [Validators.required, Validators.pattern(/^\d{5}-?\d{3}$/)]],
    logradouro: ['', [Validators.required, Validators.maxLength(150)]],
    numero: ['', [Validators.required, Validators.maxLength(20)]],
    complemento: ['', Validators.maxLength(60)],
    bairro: ['', [Validators.required, Validators.maxLength(80)]],
    cidade: ['', [Validators.required, Validators.maxLength(80)]],
    uf: [{ value: '', disabled: true }],
  });

  protected invalido(campo: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[campo];
    return c.touched && c.invalid;
  }

  /** Ao completar o CEP: preenche o endereço pelo ViaCEP e calcula o frete na nossa API. */
  protected async aoDigitarCep(valor: string): Promise<void> {
    const cep = valor.replace(/\D/g, '');
    this.frete.set(null);
    this.avisoCep.set(null);
    if (cep.length !== 8) return;

    this.buscandoCep.set(true);
    try {
      const [endereco, cotacao] = await Promise.all([
        firstValueFrom(this.pedidos.buscarCep(cep)).catch(() => null),
        firstValueFrom(this.pedidos.cotarFrete(cep)),
      ]);
      this.frete.set(cotacao);
      if (endereco && !endereco.erro) {
        this.form.patchValue({
          logradouro: endereco.logradouro || this.form.controls.logradouro.value,
          bairro: endereco.bairro || this.form.controls.bairro.value,
          cidade: endereco.localidade,
          uf: endereco.uf,
        });
      } else {
        // O frete vale mesmo assim (vem das faixas de CEP); só o endereço precisa ser digitado.
        this.form.patchValue({ uf: cotacao.uf });
        this.avisoCep.set('Não encontramos o endereço deste CEP. Preencha os campos manualmente.');
      }
    } catch (e) {
      this.avisoCep.set(lerErro(e, 'CEP inválido.').mensagem);
    } finally {
      this.buscandoCep.set(false);
    }
  }

  private async recotarFrete(): Promise<void> {
    const cep = this.form.controls.cep.value.replace(/\D/g, '');
    if (cep.length !== 8) return;
    this.frete.set(await firstValueFrom(this.pedidos.cotarFrete(cep)).catch(() => null));
  }

  protected async finalizar(): Promise<void> {
    if (this.form.invalid || !this.frete()) {
      this.form.markAllAsTouched();
      return;
    }
    this.enviando.set(true);
    this.erro.set(null);
    try {
      const v = this.form.getRawValue();
      const criado = await firstValueFrom(
        this.pedidos.criar({ ...v, complemento: v.complemento || null, totalEsperado: this.frete()!.total }),
      );
      await this.carrinho.recarregar(); // o carrinho foi esvaziado pela API
      if (criado.linkPagamento) {
        irParaPagamento(criado.linkPagamento, (url) => void this.router.navigateByUrl(url));
      } else {
        await this.router.navigate(['/pedidos', criado.id]);
      }
    } catch (e) {
      this.erro.set(lerErro(e, 'Não foi possível fechar o pedido.').mensagem);
      // Atualiza carrinho e frete na tela, para o cliente conferir o valor real antes de tentar de novo.
      await this.carrinho.recarregar();
      await this.recotarFrete();
    } finally {
      this.enviando.set(false);
    }
  }
}
