import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { errorMessage } from '../../core/utils/error-message';
import { Icon } from '../../shared/components/icon';
import { AuthAside } from './auth-aside';

const REASONS: Record<string, { kind: 'info' | 'warning'; text: string }> = {
  expired: { kind: 'warning', text: 'Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.' },
  'mfa-expired': { kind: 'warning', text: 'Doğrulama süresi doldu. Lütfen tekrar giriş yapın.' },
  registered: { kind: 'info', text: 'Hesabınız oluşturuldu. Şimdi giriş yapabilirsiniz.' },
  'logged-out': { kind: 'info', text: 'Tüm cihazlardaki oturumlarınız kapatıldı.' },
};

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, Icon, AuthAside],
  styleUrl: './auth-layout.scss',
  template: `
    <div class="auth">
      <app-auth-aside
        title="Tekrar hoş geldiniz"
        text="Şifrenizden sonra authenticator uygulamanızdaki 6 haneli kod ile girişinizi tamamlayın."
        [current]="1" />

      <section class="auth__main">
        <header>
          <span class="eyebrow">Giriş</span>
          <h1>Hesabınıza girin</h1>
          <p>Hesabınız yok mu? <a routerLink="/register">Hemen kayıt olun</a></p>
        </header>

        @if (notice(); as notice) {
          <div class="alert" [class]="'alert alert--' + notice.kind"><app-icon name="info" [size]="18" /> {{ notice.text }}</div>
        }
        @if (error()) {
          <div class="alert alert--error" role="alert"><app-icon name="info" [size]="18" /> {{ error() }}</div>
        }

        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <div class="field">
            <label for="email">E-posta</label>
            <input id="email" class="input" type="email" formControlName="email" autocomplete="email"
                   placeholder="ornek@eposta.com" />
            @if (form.controls.email.touched && form.controls.email.invalid) {
              <span class="error">Geçerli bir e-posta adresi girin.</span>
            }
          </div>

          <div class="field">
            <label for="password">Şifre</label>
            <div class="password">
              <input id="password" class="input" [type]="showPassword() ? 'text' : 'password'"
                     formControlName="password" autocomplete="current-password" placeholder="••••••••" />
              <button type="button" (click)="showPassword.set(!showPassword())">
                {{ showPassword() ? 'Gizle' : 'Göster' }}
              </button>
            </div>
            @if (form.controls.password.touched && form.controls.password.invalid) {
              <span class="error">Şifrenizi girin.</span>
            }
          </div>

          <button class="btn btn--primary btn--lg btn--block" type="submit" [disabled]="loading()">
            @if (loading()) { <span class="spinner"></span> } Devam et
            <app-icon name="arrowRight" [size]="18" />
          </button>
        </form>

        <p class="auth__footer">
          <app-icon name="lock" [size]="14" /> Bağlantınız iki adımlı doğrulama ile korunur.
        </p>
      </section>
    </div>
  `,
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  // withComponentInputBinding -> query parametreleri input olarak gelir.
  readonly returnUrl = input<string>();
  readonly reason = input<string>();
  readonly email = input<string>();

  protected readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly showPassword = signal(false);
  protected readonly notice = computed(() => REASONS[this.reason() ?? ''] ?? null);

  ngOnInit(): void {
    const email = this.email();
    if (email) this.form.controls.email.setValue(email);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: (response) => {
        const next = response.multiFactorStatus === 'SetupRequired' ? '/mfa/setup' : '/mfa/verify';
        void this.router.navigate([next], { queryParams: { returnUrl: this.returnUrl() } });
      },
      error: (error) => {
        this.loading.set(false);
        this.error.set(errorMessage(error));
      },
    });
  }
}
