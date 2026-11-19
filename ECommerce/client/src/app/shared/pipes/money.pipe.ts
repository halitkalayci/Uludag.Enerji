import { Pipe, PipeTransform } from '@angular/core';

const formatters = new Map<string, Intl.NumberFormat>();

function formatterFor(currency: string): Intl.NumberFormat {
  let formatter = formatters.get(currency);
  if (!formatter) {
    try {
      formatter = new Intl.NumberFormat('tr-TR', { style: 'currency', currency });
    } catch {
      // Geçersiz ISO kodu: sayıyı biçimlendirip kodu yanına yaz.
      formatter = new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }
    formatters.set(currency, formatter);
  }
  return formatter;
}

@Pipe({ name: 'money' })
export class MoneyPipe implements PipeTransform {
  transform(amount: number | null | undefined, currency = 'TRY'): string {
    if (amount === null || amount === undefined) return '';
    const formatter = formatterFor(currency);
    const text = formatter.format(amount);
    return formatter.resolvedOptions().style === 'currency' ? text : `${text} ${currency}`;
  }
}
