import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { Order, OrderItem } from '../../core/models/api.models';
import { OrderService } from '../../core/services/order.service';
import { ProductService } from '../../core/services/product.service';
import { ToastService } from '../../core/services/toast.service';
import { errorMessage } from '../../core/utils/error-message';
import { Icon } from '../../shared/components/icon';
import { ProductArt } from '../../shared/components/product-art';
import { MoneyPipe } from '../../shared/pipes/money.pipe';

// <input type="datetime-local"> yerel saat ister; API UTC ISO bekler.
function toLocalInput(iso: string): string {
  const date = new Date(iso);
  const offset = date.getTimezoneOffset() * 60000;
  return new Date(date.getTime() - offset).toISOString().slice(0, 16);
}

@Component({
  selector: 'app-order-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, ReactiveFormsModule, Icon, ProductArt, MoneyPipe],
  templateUrl: './order-detail.html',
  styleUrl: './order-detail.scss',
})
export class OrderDetail {
  private readonly orderService = inject(OrderService);
  private readonly productService = inject(ProductService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);

  readonly id = input.required<string>();

  protected readonly order = signal<Order | null>(null);
  protected readonly items = signal<OrderItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly busy = signal(false);
  protected readonly editingItem = signal<string | null>(null);
  protected readonly editAmount = signal(1);
  protected readonly editingDate = signal(false);

  protected readonly products = computed(() => this.productService.products() ?? []);
  // Sipariş kendi para biriminde; yalnızca aynı para birimindeki ürünler eklenebilir.
  protected readonly addableProducts = computed(() =>
    this.products().filter((product) => product.currency === this.order()?.currency && product.stock > 0),
  );

  protected readonly addForm = this.fb.group({
    productId: ['', Validators.required],
    amount: [1, [Validators.required, Validators.min(1)]],
  });

  protected readonly dateForm = this.fb.group({
    orderDate: ['', Validators.required],
  });

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => this.load(id));
    });
  }

  protected productName(productId: string): string {
    return this.productService.findCached(productId)?.name ?? 'Bilinmeyen ürün';
  }

  protected unitPrice(item: OrderItem): number {
    return item.amount > 0 ? item.totalPrice / item.amount : 0;
  }

  protected load(id = this.id()): void {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      order: this.orderService.getById(id),
      items: this.orderService.getAllItems(),
      products: this.productService.loadAll(),
    }).subscribe({
      next: ({ order, items }) => {
        this.order.set(order);
        this.items.set(items.filter((item) => item.orderId === id));
        this.dateForm.setValue({ orderDate: toLocalInput(order.orderDate) });
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.error.set(errorMessage(error, 'Sipariş yüklenemedi.'));
      },
    });
  }

  // Kalem eklendi / silindi / güncellendi -> sipariş toplamı backend'de yeniden hesaplanır.
  private reloadQuietly(message: string): void {
    this.busy.set(false);
    this.toast.success(message);
    this.load();
  }

  private fail(error: unknown): void {
    this.busy.set(false);
    this.toast.error(errorMessage(error));
  }

  protected saveDate(): void {
    const order = this.order();
    if (!order || this.dateForm.invalid) return;
    this.busy.set(true);
    const orderDate = new Date(this.dateForm.getRawValue().orderDate).toISOString();
    this.orderService.update(order.id, { orderDate }).subscribe({
      next: () => {
        this.editingDate.set(false);
        this.reloadQuietly('Sipariş tarihi güncellendi.');
      },
      error: (error) => this.fail(error),
    });
  }

  protected addItem(): void {
    const order = this.order();
    if (!order || this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }
    this.busy.set(true);
    const { productId, amount } = this.addForm.getRawValue();
    this.orderService.addItem({ orderId: order.id, productId, amount }).subscribe({
      next: () => {
        this.addForm.reset({ productId: '', amount: 1 });
        this.reloadQuietly('Ürün siparişe eklendi.');
      },
      error: (error) => this.fail(error),
    });
  }

  protected startEdit(item: OrderItem): void {
    this.editingItem.set(item.id);
    this.editAmount.set(item.amount);
  }

  protected onEditAmount(event: Event): void {
    this.editAmount.set(Number((event.target as HTMLInputElement).value));
  }

  protected saveItem(item: OrderItem): void {
    const amount = this.editAmount();
    if (!Number.isInteger(amount) || amount < 1) {
      this.toast.error('Adet en az 1 olmalı.');
      return;
    }
    this.busy.set(true);
    this.orderService.updateItem(item.id, { amount }).subscribe({
      next: () => {
        this.editingItem.set(null);
        this.reloadQuietly('Adet güncellendi.');
      },
      error: (error) => this.fail(error),
    });
  }

  protected removeItem(item: OrderItem): void {
    if (!confirm(`"${this.productName(item.productId)}" siparişten çıkarılsın mı?`)) return;
    this.busy.set(true);
    this.orderService.deleteItem(item.id).subscribe({
      next: () => this.reloadQuietly('Ürün siparişten çıkarıldı.'),
      error: (error) => this.fail(error),
    });
  }

  protected removeOrder(): void {
    const order = this.order();
    if (!order || !confirm('Bu sipariş tamamen silinsin mi?')) return;
    this.busy.set(true);
    this.orderService.delete(order.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.toast.success('Sipariş silindi.');
        void this.router.navigate(['/orders']);
      },
      error: (error) => this.fail(error),
    });
  }
}
