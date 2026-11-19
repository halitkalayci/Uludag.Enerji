import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Product } from '../../core/models/api.models';
import { ProductService } from '../../core/services/product.service';
import { ToastService } from '../../core/services/toast.service';
import { errorMessage } from '../../core/utils/error-message';
import { Icon } from '../../shared/components/icon';
import { ProductArt } from '../../shared/components/product-art';
import { MoneyPipe } from '../../shared/pipes/money.pipe';

const CURRENCIES = ['TRY', 'USD', 'EUR', 'GBP'];

// Ürün ekle / güncelle / sil. Yetki backend'de kontrol edilir (Product.Admin / Write / Create / Update / Delete);
// yetkisiz istek 403 döner ve kullanıcıya gösterilir.
@Component({
  selector: 'app-admin-products',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, Icon, ProductArt, MoneyPipe],
  templateUrl: './admin-products.html',
  styleUrl: './admin-products.scss',
})
export class AdminProducts {
  private readonly productService = inject(ProductService);
  private readonly toast = inject(ToastService);

  protected readonly currencies = CURRENCIES;
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly query = signal('');

  protected readonly dialogOpen = signal(false);
  protected readonly editing = signal<Product | null>(null);
  protected readonly saving = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly deleting = signal<string | null>(null);

  protected readonly products = computed(() => {
    const query = this.query().trim().toLocaleLowerCase('tr-TR');
    return (this.productService.products() ?? []).filter(
      (product) => !query || (product.name ?? '').toLocaleLowerCase('tr-TR').includes(query),
    );
  });

  protected readonly stats = computed(() => {
    const products = this.productService.products() ?? [];
    return {
      total: products.length,
      outOfStock: products.filter((product) => product.stock === 0).length,
      lowStock: products.filter((product) => product.stock > 0 && product.stock <= 5).length,
    };
  });

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    currency: ['TRY', Validators.required],
    stock: [0, [Validators.required, Validators.min(0)]],
  });

  constructor() {
    this.load();
  }

  protected load(force = false): void {
    this.loading.set(true);
    this.error.set(null);
    this.productService.loadAll(force).subscribe({
      next: () => this.loading.set(false),
      error: (error) => {
        this.loading.set(false);
        this.error.set(errorMessage(error));
      },
    });
  }

  protected onSearch(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
  }

  protected openCreate(): void {
    this.editing.set(null);
    this.form.reset({ name: '', amount: 0, currency: 'TRY', stock: 0 });
    this.formError.set(null);
    this.dialogOpen.set(true);
  }

  protected openEdit(product: Product): void {
    this.editing.set(product);
    this.form.reset({
      name: product.name ?? '',
      amount: product.amount,
      currency: product.currency,
      stock: product.stock,
    });
    this.formError.set(null);
    this.dialogOpen.set(true);
  }

  protected close(): void {
    if (!this.saving()) this.dialogOpen.set(false);
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const request = { ...value, name: value.name.trim(), currency: value.currency.toUpperCase() };
    const editing = this.editing();
    const call = editing
      ? this.productService.update(editing.id, request)
      : this.productService.create(request);

    this.saving.set(true);
    this.formError.set(null);
    call.subscribe({
      next: () => {
        this.saving.set(false);
        this.dialogOpen.set(false);
        this.toast.success(editing ? 'Ürün güncellendi.' : 'Ürün eklendi.');
        this.load(true);
      },
      error: (error) => {
        this.saving.set(false);
        this.formError.set(errorMessage(error));
      },
    });
  }

  protected remove(product: Product): void {
    if (!confirm(`"${product.name}" ürünü silinsin mi?`)) return;
    this.deleting.set(product.id);
    this.productService.delete(product.id).subscribe({
      next: () => {
        this.deleting.set(null);
        this.toast.success('Ürün silindi.');
        this.load(true);
      },
      error: (error) => {
        this.deleting.set(null);
        this.toast.error(errorMessage(error));
      },
    });
  }
}
