import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { errorMessage } from '../../core/utils/error-message';
import { Icon } from '../../shared/components/icon';
import { AuthAside } from './auth-aside';

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirm = group.get('confirmPassword')?.value;
  return password && confirm && password !== confirm ? { mismatch: true } : null;
}

@Component({
  selector: 'app-register',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, Icon, AuthAside],
  styleUrl: './auth-layout.scss',
  template: `
    <div class="auth">
      <app-auth-aside
        title="Uludağ'a katılın"
        text="Hesabınızı oluşturun. İlk girişinizde telefonunuza bir authenticator uygulaması bağlayacağız."
        [current]="1" />

      <section class="auth__main">
        <header>
          <span class="eyebrow">Kayıt</span>
          <h1>Yeni hesap oluşturun</h1>
          <p>Zaten hesabınız var mı? <a routerLink="/login">Giriş yapın</a></p>
        </header>

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
                     formControlName="password" autocomplete="new-password" placeholder="En az 6 karakter" />
              <button type="button" (click)="showPassword.set(!showPassword())">
                {{ showPassword() ? 'Gizle' : 'Göster' }}
              </button>
            </div>
            @if (form.controls.password.touched && form.controls.password.invalid) {
              <span class="error">Şifre en az 6 karakter olmalı.</span>
            }
          </div>

          <div class="field">
            <label for="confirmPassword">Şifre (tekrar)</label>
            <input id="confirmPassword" class="input" [type]="showPassword() ? 'text' : 'password'"
                   formControlName="confirmPassword" autocomplete="new-password" />
            @if (form.controls.confirmPassword.touched && form.hasError('mismatch')) {
              <span class="error">Şifreler eşleşmiyor.</span>
            }
          </div>

          <button class="btn btn--primary btn--lg btn--block" type="submit" [disabled]="loading()">
            @if (loading()) { <span class="spinner"></span> } Hesap oluştur
          </button>
        </form>
      </section>
    </div>
  `,
})
export class Register {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly form = inject(NonNullableFormBuilder).group(
    {
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required],
    },
    { validators: passwordsMatch },
  );

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly showPassword = signal(false);

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();
    this.loading.set(true);
    this.error.set(null);

    this.auth.register({ email, password }).subscribe({
      next: () => void this.router.navigate(['/login'], { queryParams: { reason: 'registered', email } }),
      error: (error) => {
        this.loading.set(false);
        this.error.set(errorMessage(error));
      },
    });
  }
}
