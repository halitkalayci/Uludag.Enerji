import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { errorMessage } from '../../core/utils/error-message';
import { Icon } from '../../shared/components/icon';

// Backend'deki OperationClaim seed'leri (GeneralOperationClaims + *OperationClaims sabitleri).
const CLAIM_GROUPS = [
  { label: 'Genel', claims: ['Admin'] },
  { label: 'Ürün', claims: ['Product.Admin', 'Product.Read', 'Product.Write', 'Product.Create', 'Product.Update', 'Product.Delete'] },
  { label: 'Sipariş', claims: ['Order.Admin', 'Order.Read', 'Order.Write', 'Order.Create', 'Order.Update', 'Order.Delete'] },
  { label: 'Sipariş kalemi', claims: ['OrderItem.Admin', 'OrderItem.Read', 'OrderItem.Write', 'OrderItem.Create', 'OrderItem.Update', 'OrderItem.Delete'] },
];

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

interface AssignmentLog {
  userId: string;
  claim: string;
  at: Date;
}

@Component({
  selector: 'app-admin-claims',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Icon],
  template: `
    <div class="container page">
      <header class="page-header">
        <div>
          <span class="eyebrow">Yönetim</span>
          <h1>Yetki atama</h1>
          <p>Bir kullanıcıya operation claim (rol) atayın. Yalnızca <strong>Admin</strong> yetkili hesaplar kullanabilir.</p>
        </div>
      </header>

      <div class="layout">
        <form class="card card-body stack" [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="alert alert--info">
            <app-icon name="info" [size]="18" />
            <span>Yeni yetki anında geçerli olur; kullanıcının yeniden giriş yapmasına gerek yoktur.</span>
          </div>

          @if (error()) {
            <div class="alert alert--error" role="alert"><app-icon name="info" [size]="18" /> {{ error() }}</div>
          }

          <div class="field">
            <label for="userId">Kullanıcı Id (GUID)</label>
            <input id="userId" class="input mono" formControlName="userId"
                   placeholder="00000000-0000-0000-0000-000000000000" autocomplete="off" />
            @if (form.controls.userId.touched && form.controls.userId.invalid) {
              <span class="error">Geçerli bir GUID girin.</span>
            } @else {
              <span class="hint">Kullanıcı Id'si veritabanındaki Users tablosundan alınır.</span>
            }
          </div>

          <div class="field">
            <label>Yetki</label>
            <div class="claims">
              @for (group of groups; track group.label) {
                <div class="claims__group">
                  <span class="claims__label">{{ group.label }}</span>
                  <div class="claims__chips">
                    @for (claim of group.claims; track claim) {
                      <button type="button" class="chip" [class.is-selected]="form.controls.operationClaimName.value === claim"
                              (click)="form.controls.operationClaimName.setValue(claim)">
                        {{ claim }}
                      </button>
                    }
                  </div>
                </div>
              }
            </div>
            <input class="input mono" formControlName="operationClaimName" placeholder="veya elle yazın, ör. Product.Read" />
            @if (form.controls.operationClaimName.touched && form.controls.operationClaimName.invalid) {
              <span class="error">Bir yetki seçin.</span>
            }
          </div>

          <button type="submit" class="btn btn--primary btn--lg" [disabled]="saving()">
            @if (saving()) { <span class="spinner"></span> } <app-icon name="key" [size]="18" /> Yetkiyi ata
          </button>
        </form>

        <aside class="card card-body stack">
          <h3>Bu oturumdaki atamalar</h3>
          @if (log().length === 0) {
            <p class="small muted">Henüz atama yapılmadı.</p>
          } @else {
            <ul class="log">
              @for (entry of log(); track $index) {
                <li>
                  <span class="badge badge--primary">{{ entry.claim }}</span>
                  <span class="mono small">{{ entry.userId }}</span>
                </li>
              }
            </ul>
          }
        </aside>
      </div>
    </div>
  `,
  styles: `
    .layout { display: grid; grid-template-columns: 1fr 340px; gap: 24px; align-items: start; }
    @media (max-width: 900px) { .layout { grid-template-columns: 1fr; } }
    .claims { display: flex; flex-direction: column; gap: 12px; padding: 14px; margin-bottom: 4px; border: 1px solid var(--border); border-radius: var(--radius-md); background: var(--surface-muted); }
    .claims__group { display: flex; flex-direction: column; gap: 6px; }
    .claims__label { font-size: 11px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--text-muted); }
    .claims__chips { display: flex; flex-wrap: wrap; gap: 6px; }
    .chip {
      height: 30px; padding: 0 12px; border-radius: 999px; cursor: pointer;
      border: 1px solid var(--border-strong); background: var(--surface);
      font: 600 12px var(--font-mono); color: var(--text-soft);
    }
    .chip:hover { border-color: var(--primary); color: var(--primary); }
    .chip.is-selected { background: var(--primary); border-color: var(--primary); color: #fff; }
    .log { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 10px; }
    .log li { display: flex; flex-direction: column; align-items: flex-start; gap: 4px; padding-bottom: 10px; border-bottom: 1px solid var(--border); word-break: break-all; }
  `,
})
export class AdminClaims {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  protected readonly groups = CLAIM_GROUPS;
  protected readonly saving = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly log = signal<AssignmentLog[]>([]);

  protected readonly form = inject(NonNullableFormBuilder).group({
    userId: ['', [Validators.required, Validators.pattern(GUID)]],
    operationClaimName: ['', Validators.required],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request = this.form.getRawValue();
    request.userId = request.userId.trim();
    request.operationClaimName = request.operationClaimName.trim();

    this.saving.set(true);
    this.error.set(null);
    this.auth.assignOperationClaim(request).subscribe({
      next: () => {
        this.saving.set(false);
        this.log.update((log) => [{ userId: request.userId, claim: request.operationClaimName, at: new Date() }, ...log]);
        this.form.controls.operationClaimName.reset('');
        this.toast.success(`${request.operationClaimName} yetkisi atandı.`);
      },
      error: (error) => {
        this.saving.set(false);
        this.error.set(errorMessage(error));
      },
    });
  }
}
