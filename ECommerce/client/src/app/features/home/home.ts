import { ChangeDetectionStrategy, Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Product } from '../../core/models/api.models';
import { AuthService } from '../../core/services/auth.service';
import { ProductService } from '../../core/services/product.service';
import { errorMessage } from '../../core/utils/error-message';
import { Icon } from '../../shared/components/icon';
import { ProductCard } from './product-card';

type SortKey = 'name' | 'price-asc' | 'price-desc' | 'stock';

@Component({
  selector: 'app-home',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Icon, ProductCard],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private readonly productService = inject(ProductService);
  protected readonly auth = inject(AuthService);

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly query = signal('');
  protected readonly sort = signal<SortKey>('name');
  protected readonly inStockOnly = signal(false);

  private readonly products = computed(() => this.productService.products() ?? []);

  protected readonly visible = computed(() => {
    const query = this.query().trim().toLocaleLowerCase('tr-TR');
    const filtered = this.products().filter(
      (product) =>
        (!query || (product.name ?? '').toLocaleLowerCase('tr-TR').includes(query)) &&
        (!this.inStockOnly() || product.stock > 0),
    );
    return [...filtered].sort(comparer(this.sort()));
  });

  protected readonly stats = computed(() => {
    const products = this.products();
    return {
      total: products.length,
      inStock: products.filter((product) => product.stock > 0).length,
      units: products.reduce((sum, product) => sum + product.stock, 0),
    };
  });

  constructor() {
    // Ürün listesi giriş ister (backend AuthenticationBehavior). Oturum açılınca yükle.
    effect(() => {
      if (this.auth.isAuthenticated()) {
        untracked(() => this.load());
      } else {
        this.loading.set(false);
      }
    });
  }

  protected load(force = false): void {
    this.loading.set(true);
    this.error.set(null);
    this.productService.loadAll(force).subscribe({
      next: () => this.loading.set(false),
      error: (error) => {
        this.loading.set(false);
        this.error.set(errorMessage(error, 'Ürünler yüklenemedi.'));
      },
    });
  }

  protected onSearch(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
  }

  protected onSort(event: Event): void {
    this.sort.set((event.target as HTMLSelectElement).value as SortKey);
  }

  protected resetFilters(): void {
    this.query.set('');
    this.inStockOnly.set(false);
  }
}

function comparer(sort: SortKey): (a: Product, b: Product) => number {
  switch (sort) {
    case 'price-asc':
      return (a, b) => a.amount - b.amount;
    case 'price-desc':
      return (a, b) => b.amount - a.amount;
    case 'stock':
      return (a, b) => b.stock - a.stock;
    default:
      return (a, b) => (a.name ?? '').localeCompare(b.name ?? '', 'tr');
  }
}
