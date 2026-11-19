import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { CartService, CurrencyTotal } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';
import { ProductService } from '../../core/services/product.service';
import { ToastService } from '../../core/services/toast.service';
import { errorMessage } from '../../core/utils/error-message';
import { Icon } from '../../shared/components/icon';
import { ProductArt } from '../../shared/components/product-art';
import { MoneyPipe } from '../../shared/pipes/money.pipe';

@Component({
  selector: 'app-cart',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Icon, ProductArt, MoneyPipe],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',
})
export class Cart {
  protected readonly cart = inject(CartService);
  protected readonly auth = inject(AuthService);
  private readonly orders = inject(OrderService);
  private readonly products = inject(ProductService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly placing = signal<string | null>(null); // sipariş verilen para birimi
  protected readonly error = signal<string | null>(null);

  // Bir sipariş tek para birimiyle açılır: her para birimi grubu ayrı bir siparişe dönüşür.
  protected placeOrder(group: CurrencyTotal): void {
    this.placing.set(group.currency);
    this.error.set(null);

    this.orders
      .create({
        currency: group.currency,
        items: group.lines.map((line) => ({ productId: line.product.id, amount: line.quantity })),
      })
      .subscribe({
        next: () => {
          this.placing.set(null);
          this.cart.removeCurrency(group.currency);
          this.products.loadAll(true).subscribe({ error: () => undefined }); // stoklar değişmiş olabilir
          this.toast.success('Siparişiniz oluşturuldu.');
          if (this.cart.isEmpty()) void this.router.navigate(['/orders']);
        },
        error: (error) => {
          this.placing.set(null);
          this.error.set(errorMessage(error, 'Sipariş oluşturulamadı.'));
        },
      });
  }
}
