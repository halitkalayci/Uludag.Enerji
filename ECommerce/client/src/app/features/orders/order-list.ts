import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Order } from '../../core/models/api.models';
import { OrderService } from '../../core/services/order.service';
import { ToastService } from '../../core/services/toast.service';
import { errorMessage } from '../../core/utils/error-message';
import { Icon } from '../../shared/components/icon';
import { MoneyPipe } from '../../shared/pipes/money.pipe';

@Component({
  selector: 'app-order-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, Icon, MoneyPipe],
  template: `
    <div class="container page">
      <header class="page-header">
        <div>
          <span class="eyebrow">Hesap</span>
          <h1>Siparişler</h1>
          <p>Oluşturulan siparişleri görüntüleyin, düzenleyin veya silin.</p>
        </div>
        <div class="row">
          <button type="button" class="btn btn--ghost btn--sm" (click)="load()" [disabled]="loading()">
            <app-icon name="refresh" [size]="16" /> Yenile
          </button>
          <a routerLink="/" class="btn btn--primary btn--sm"><app-icon name="plus" [size]="16" /> Yeni sipariş</a>
        </div>
      </header>

      @if (orders().length > 0) {
        <div class="summary">
          <div class="card summary__item"><span>Sipariş</span><strong>{{ orders().length }}</strong></div>
          @for (total of totals(); track total.currency) {
            <div class="card summary__item"><span>Toplam ({{ total.currency }})</span><strong>{{ total.sum | money: total.currency }}</strong></div>
          }
        </div>
      }

      @if (loading() && orders().length === 0) {
        <div class="card" style="padding: 8px">
          @for (i of [1, 2, 3, 4]; track i) {
            <div class="skeleton" style="height: 48px; margin: 8px"></div>
          }
        </div>
      } @else if (error()) {
        <div class="card empty">
          <div class="empty__icon"><app-icon [name]="forbidden() ? 'lock' : 'info'" /></div>
          <h3>{{ forbidden() ? 'Siparişleri görme yetkiniz yok' : 'Siparişler yüklenemedi' }}</h3>
          <p>{{ forbidden() ? 'Bu sayfa için Order.Read veya Order.Admin yetkisi gerekir. Bir yöneticiden yetki isteyin.' : error() }}</p>
          @if (!forbidden()) {
            <button type="button" class="btn btn--primary" (click)="load()">Tekrar dene</button>
          }
        </div>
      } @else if (orders().length === 0) {
        <div class="card empty">
          <div class="empty__icon"><app-icon name="receipt" /></div>
          <h3>Henüz sipariş yok</h3>
          <p>Sepetinize ürün ekleyip ilk siparişinizi verin.</p>
          <a routerLink="/" class="btn btn--primary">Ürünlere göz at</a>
        </div>
      } @else {
        <div class="card table-wrap">
          <table class="table">
            <thead>
              <tr>
                <th>Sipariş</th>
                <th>Tarih</th>
                <th class="num">Tutar</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (order of sorted(); track order.id) {
                <tr>
                  <td>
                    <a [routerLink]="['/orders', order.id]" class="order-id">#{{ order.id.slice(0, 8).toUpperCase() }}</a>
                  </td>
                  <td>{{ order.orderDate | date: 'd MMMM y, HH:mm' }}</td>
                  <td class="num"><strong>{{ order.totalPrice | money: order.currency }}</strong></td>
                  <td class="num">
                    <div class="row" style="justify-content: flex-end; gap: 6px">
                      <a [routerLink]="['/orders', order.id]" class="btn btn--sm">Detay</a>
                      <button type="button" class="btn btn--sm btn--danger btn--icon" (click)="remove(order)"
                              [disabled]="deleting() === order.id" aria-label="Siparişi sil">
                        <app-icon name="trash" [size]="15" />
                      </button>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
  styles: `
    .summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 14px; margin-bottom: 20px; }
    .summary__item { display: flex; flex-direction: column; gap: 2px; padding: 16px 20px; }
    .summary__item span { font-size: 13px; color: var(--text-muted); }
    .summary__item strong { font-family: var(--font-display); font-size: 26px; }
    .order-id { font-family: var(--font-mono); font-weight: 600; }
  `,
})
export class OrderList {
  private readonly orderService = inject(OrderService);
  private readonly toast = inject(ToastService);

  protected readonly orders = signal<Order[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly forbidden = signal(false);
  protected readonly deleting = signal<string | null>(null);

  protected readonly sorted = computed(() =>
    [...this.orders()].sort((a, b) => b.orderDate.localeCompare(a.orderDate)),
  );

  protected readonly totals = computed(() => {
    const map = new Map<string, number>();
    for (const order of this.orders()) map.set(order.currency, (map.get(order.currency) ?? 0) + order.totalPrice);
    return [...map].map(([currency, sum]) => ({ currency, sum }));
  });

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.orderService.getAll().subscribe({
      next: (orders) => {
        this.orders.set(orders);
        this.loading.set(false);
      },
      error: (error) => {
        this.loading.set(false);
        this.forbidden.set(error instanceof HttpErrorResponse && error.status === 403);
        this.error.set(errorMessage(error));
      },
    });
  }

  protected remove(order: Order): void {
    if (!confirm(`#${order.id.slice(0, 8).toUpperCase()} numaralı sipariş silinsin mi?`)) return;
    this.deleting.set(order.id);
    this.orderService.delete(order.id).subscribe({
      next: () => {
        this.deleting.set(null);
        this.orders.update((orders) => orders.filter((item) => item.id !== order.id));
        this.toast.success('Sipariş silindi.');
      },
      error: (error) => {
        this.deleting.set(null);
        this.toast.error(errorMessage(error));
      },
    });
  }
}
