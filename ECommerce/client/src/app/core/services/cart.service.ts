import { Injectable, computed, signal } from '@angular/core';
import { Product } from '../models/api.models';
import { readJson, writeJson } from '../utils/storage';

export interface CartLine {
  product: Product;
  quantity: number;
}

export interface CurrencyTotal {
  currency: string;
  total: number;
  lines: CartLine[];
}

const KEY = 'uludag.cart';

// Sepet yalnızca istemcide tutulur; "Siparişi ver" ile API'de Order + OrderItem'lara dönüşür.
@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly state = signal<CartLine[]>(readJson<CartLine[]>(localStorage, KEY) ?? []);

  readonly lines = this.state.asReadonly();
  readonly count = computed(() => this.state().reduce((sum, line) => sum + line.quantity, 0));
  readonly isEmpty = computed(() => this.state().length === 0);

  // Bir sipariş tek para birimiyle açılır; sepet para birimine göre gruplanır.
  readonly totals = computed<CurrencyTotal[]>(() => {
    const groups = new Map<string, CurrencyTotal>();
    for (const line of this.state()) {
      const currency = line.product.currency;
      const group = groups.get(currency) ?? { currency, total: 0, lines: [] };
      group.total += line.product.amount * line.quantity;
      group.lines.push(line);
      groups.set(currency, group);
    }
    return [...groups.values()];
  });

  quantityOf(productId: string): number {
    return this.state().find((line) => line.product.id === productId)?.quantity ?? 0;
  }

  add(product: Product, quantity = 1): void {
    const lines = this.state();
    const existing = lines.find((line) => line.product.id === product.id);
    const next = existing
      ? lines.map((line) =>
          line.product.id === product.id
            ? { product, quantity: Math.min(line.quantity + quantity, product.stock) }
            : line,
        )
      : [...lines, { product, quantity: Math.min(quantity, product.stock) }];
    this.save(next);
  }

  setQuantity(productId: string, quantity: number): void {
    if (quantity <= 0) {
      this.remove(productId);
      return;
    }
    this.save(
      this.state().map((line) =>
        line.product.id === productId
          ? { ...line, quantity: Math.min(quantity, line.product.stock) }
          : line,
      ),
    );
  }

  remove(productId: string): void {
    this.save(this.state().filter((line) => line.product.id !== productId));
  }

  removeCurrency(currency: string): void {
    this.save(this.state().filter((line) => line.product.currency !== currency));
  }

  clear(): void {
    this.save([]);
  }

  private save(lines: CartLine[]): void {
    this.state.set(lines);
    writeJson(localStorage, KEY, lines);
  }
}
