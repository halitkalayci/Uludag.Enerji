import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { Product } from '../../core/models/api.models';
import { CartService } from '../../core/services/cart.service';
import { ProductService } from '../../core/services/product.service';
import { ToastService } from '../../core/services/toast.service';
import { errorMessage } from '../../core/utils/error-message';
import { Icon } from '../../shared/components/icon';
import { ProductArt } from '../../shared/components/product-art';
import { MoneyPipe } from '../../shared/pipes/money.pipe';

@Component({
  selector: 'app-product-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Icon, ProductArt, MoneyPipe],
  template: `
    <div class="container page">
      <a routerLink="/" class="back"><app-icon name="arrowLeft" [size]="16" /> Tüm ürünler</a>

      @if (loading()) {
        <div class="detail">
          <div class="skeleton" style="aspect-ratio: 4/3"></div>
          <div class="stack">
            <div class="skeleton" style="height: 40px; width: 70%"></div>
            <div class="skeleton" style="height: 24px; width: 40%"></div>
            <div class="skeleton" style="height: 120px"></div>
          </div>
        </div>
      } @else if (error(); as error) {
        <div class="card empty">
          <div class="empty__icon"><app-icon [name]="forbidden() ? 'lock' : 'info'" /></div>
          <h3>{{ forbidden() ? 'Bu ürünü görüntüleme yetkiniz yok' : 'Ürün yüklenemedi' }}</h3>
          <p>{{ forbidden() ? 'Ürün detayı için Product.Read yetkisi gerekir. Ürünü listeden sepete ekleyebilirsiniz.' : error }}</p>
          <a routerLink="/" class="btn btn--primary">Ürünlere dön</a>
        </div>
      } @else if (product(); as product) {
        <div class="detail">
          <div class="detail__art">
            <app-product-art [name]="product.name" />
          </div>

          <div class="detail__info">
            <span class="eyebrow">Ürün</span>
            <h1>{{ product.name || 'İsimsiz ürün' }}</h1>

            <div class="price">{{ product.amount | money: product.currency }}</div>

            <div class="row">
              @if (product.stock === 0) {
                <span class="badge badge--danger">Tükendi</span>
              } @else if (product.stock <= 5) {
                <span class="badge badge--warning">Son {{ product.stock }} ürün</span>
              } @else {
                <span class="badge badge--success"><app-icon name="check" [size]="13" /> Stokta · {{ product.stock }} adet</span>
              }
              <span class="badge">{{ product.currency }}</span>
            </div>

            <div class="buy card">
              <div class="qty">
                <button type="button" (click)="quantity.set(quantity() - 1)" [disabled]="quantity() <= 1" aria-label="Azalt">
                  <app-icon name="minus" [size]="16" />
                </button>
                <span>{{ quantity() }}</span>
                <button type="button" (click)="quantity.set(quantity() + 1)" [disabled]="quantity() >= available()" aria-label="Arttır">
                  <app-icon name="plus" [size]="16" />
                </button>
              </div>
              <button type="button" class="btn btn--primary btn--lg" style="flex:1"
                      [disabled]="available() === 0" (click)="addToCart(product)">
                <app-icon name="cart" [size]="18" /> Sepete ekle · {{ product.amount * quantity() | money: product.currency }}
              </button>
            </div>

            @if (inCart() > 0) {
              <p class="small muted">Sepetinizde bu üründen {{ inCart() }} adet var. <a routerLink="/cart">Sepete git</a></p>
            }

            <dl class="meta">
              <div><dt>Ürün kodu</dt><dd class="mono">{{ product.id }}</dd></div>
              <div><dt>Birim fiyat</dt><dd>{{ product.amount | money: product.currency }}</dd></div>
              <div><dt>Stok</dt><dd>{{ product.stock }} adet</dd></div>
            </dl>
          </div>
        </div>
      }
    </div>
  `,
  styles: `
    .back { display: inline-flex; align-items: center; gap: 6px; font-weight: 600; font-size: 14px; margin-bottom: 22px; }
    .detail { display: grid; grid-template-columns: 1.1fr 1fr; gap: 44px; align-items: start; }
    @media (max-width: 860px) { .detail { grid-template-columns: 1fr; gap: 28px; } }
    .detail__art { overflow: hidden; aspect-ratio: 4 / 3; border-radius: var(--radius-xl); border: 1px solid var(--border); box-shadow: var(--shadow-md); }
    .detail__art app-product-art { height: 100%; }
    .detail__info { display: flex; flex-direction: column; gap: 16px; }
    .price { font-size: 32px; font-weight: 700; color: var(--primary); font-variant-numeric: tabular-nums; }
    .buy { display: flex; gap: 12px; align-items: center; padding: 14px; }
    .qty { display: flex; align-items: center; gap: 6px; padding: 4px; border: 1px solid var(--border-strong); border-radius: 999px; }
    .qty span { min-width: 28px; text-align: center; font-weight: 700; }
    .qty button { display: grid; place-items: center; width: 38px; height: 38px; border: 0; border-radius: 50%; background: var(--bg-soft); cursor: pointer; color: var(--text); }
    .qty button:disabled { opacity: .4; cursor: not-allowed; }
    .meta { margin: 8px 0 0; border-top: 1px solid var(--border); }
    .meta div { display: flex; justify-content: space-between; gap: 16px; padding: 12px 0; border-bottom: 1px solid var(--border); font-size: 14px; }
    .meta dt { color: var(--text-muted); }
    .meta dd { margin: 0; font-weight: 600; text-align: right; word-break: break-all; }
  `,
})
export class ProductDetail {
  private readonly products = inject(ProductService);
  private readonly cart = inject(CartService);
  private readonly toast = inject(ToastService);

  readonly id = input.required<string>();

  protected readonly product = signal<Product | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly forbidden = signal(false);
  protected readonly quantity = signal(1);

  protected readonly inCart = computed(() => {
    this.cart.lines();
    const product = this.product();
    return product ? this.cart.quantityOf(product.id) : 0;
  });
  protected readonly available = computed(() => Math.max(0, (this.product()?.stock ?? 0) - this.inCart()));

  constructor() {
    effect(() => this.load(this.id()));
  }

  private load(id: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.forbidden.set(false);
    this.products.getById(id).subscribe({
      next: (product) => {
        this.product.set(product);
        this.quantity.set(1);
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.forbidden.set(error instanceof HttpErrorResponse && error.status === 403);
        this.error.set(errorMessage(error));
      },
    });
  }

  protected addToCart(product: Product): void {
    const quantity = Math.min(this.quantity(), this.available());
    if (quantity <= 0) return;
    this.cart.add(product, quantity);
    this.quantity.set(1);
    this.toast.success(`${quantity} × ${product.name ?? 'ürün'} sepete eklendi.`);
  }
}
