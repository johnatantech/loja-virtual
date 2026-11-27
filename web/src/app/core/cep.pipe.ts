import { Pipe, PipeTransform } from '@angular/core';

/** "01310100" vira "01310-100". */
@Pipe({ name: 'cep' })
export class CepPipe implements PipeTransform {
  transform(valor: string | null | undefined): string {
    const digitos = (valor ?? '').replace(/\D/g, '');
    return digitos.length === 8 ? `${digitos.slice(0, 5)}-${digitos.slice(5)}` : (valor ?? '');
  }
}
