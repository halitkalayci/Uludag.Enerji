import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Product } from '../../core/models/api.models';
import { CartService } from '../../core/services/cart.service';
import { ToastService } from '../../core/services/toast.service';
import { Icon } from '../../shared/components/icon';
import { ProductArt } from '../../shared/components/product-art';
import { MoneyPipe } from '../../shared/pipes/money.pipe';

@Component({
  selector: 'app-product-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Icon, ProductArt, MoneyPipe],
  template: `
    <a class="art" [routerLink]="['/products', product().id]" [attr.aria-label]="product().name + ' detayı'">
      <app-product-art [name]="product().name" />
      @if (product().stock === 0) {
        <span class="badge badge--danger art__badge">Tükendi</span>
      } @else if (product().stock <= 5) {
        <span class="badge badge--warning art__badge">Son {{ product().stock }} ürün</span>
      }
    </a>

    <div class="body">
      <a class="name" [routerLink]="['/products', product().id]">{{ product().name || 'İsimsiz ürün' }}</a>
      <span class="stock">{{ product().stock > 0 ? product().stock + ' adet stokta' : 'Stokta yok' }}</span>

      <div class="footer">
        <span class="price">{{ product().amount | money: product().currency }}</span>

        @if (inCart() > 0) {
          <div class="qty">
            <button type="button" (click)="cart.setQuantity(product().id, inCart() - 1)" aria-label="Azalt">
              <app-icon name="minus" [size]="16" />
            </button>
            <span>{{ inCart() }}</span>
            <button type="button" (click)="add()" [disabled]="inCart() >= product().stock" aria-label="Arttır">
              <app-icon name="plus" [size]="16" />
            </button>
          </div>
        } @else {
          <button type="button" class="btn btn--primary btn--sm" (click)="add()" [disabled]="product().stock === 0">
            <app-icon name="cart" [size]="16" /> Sepete ekle
          </button>
        }
      </div>
    </div>
  `,
  styles: `
    :host {
      display: flex; flex-direction: column; overflow: hidden;
      background: var(--surface); border: 1px solid var(--border);
      border-radius: var(--radius-lg); box-shadow: var(--shadow-sm);
      transition: transform .2s, box-shadow .2s;
    }
    :host:hover { transform: translateY(-3px); box-shadow: var(--shadow-md); }
    .art { position: relative; display: block; aspect-ratio: 16 / 10; }
    .art app-product-art { height: 100%; }
    .art__badge { position: absolute; top: 12px; left: 12px; box-shadow: var(--shadow-sm); }
    .body { display: flex; flex-direction: column; gap: 4px; padding: 16px 18px 18px; flex: 1; }
    .name {
      font-family: var(--font-display); font-size: 18px; font-weight: 600; color: var(--text);
      overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
    }
    .stock { font-size: 13px; color: var(--text-muted); }
    .footer { display: flex; align-items: center; justify-content: space-between; gap: 10px; margin-top: auto; padding-top: 14px; }
    .price { font-size: 19px; font-weight: 700; color: var(--primary); font-variant-numeric: tabular-nums; }
    .qty {
      display: flex; align-items: center; gap: 4px; padding: 3px;
      border: 1px solid var(--border-strong); border-radius: 999px;
    }
    .qty span { min-width: 22px; text-align: center; font-weight: 700; font-size: 14px; }
    .qty button {
      display: grid; place-items: center; width: 28px; height: 28px; border: 0; border-radius: 50%;
      background: var(--bg-soft); color: var(--text); cursor: pointer;
    }
    .qty button:disabled { opacity: .4; cursor: not-allowed; }
  `,
})
export class ProductCard {
  protected readonly cart = inject(CartService);
  private readonly toast = inject(ToastService);

  readonly product = input.required<Product>();
  protected readonly inCart = computed(() => {
    this.cart.lines(); // sepet değişince yeniden hesapla
    return this.cart.quantityOf(this.product().id);
  });

  protected add(): void {
    const first = this.inCart() === 0;
    this.cart.add(this.product());
    if (first) this.toast.success(`${this.product().name ?? 'Ürün'} sepete eklendi.`);
  }
}
