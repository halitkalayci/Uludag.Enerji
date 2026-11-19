import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { MfaSessionService } from '../../core/services/mfa-session.service';
import { countdown, formatDuration } from '../../core/utils/countdown';
import { errorMessage } from '../../core/utils/error-message';
import { Icon } from '../../shared/components/icon';
import { OtpInput } from '../../shared/components/otp-input';
import { AuthAside } from './auth-aside';
import { MfaFlow } from './mfa-flow';

// MFA zaten kurulu: authenticator uygulamasındaki güncel kod istenir.
@Component({
  selector: 'app-mfa-verify',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Icon, OtpInput, AuthAside],
  styleUrl: './auth-layout.scss',
  template: `
    <div class="auth">
      <app-auth-aside
        title="Son bir adım"
        text="Authenticator uygulamanız her 30 saniyede bir yeni kod üretir. Güncel kodu girin."
        [current]="3" />

      <section class="auth__main">
        <header>
          <span class="eyebrow">Adım 3 / 3 · Doğrulama</span>
          <h1>Doğrulama kodunu girin</h1>
          <p><strong>{{ mfa.pending()?.email }}</strong> için authenticator uygulamanızdaki 6 haneli kod.</p>
        </header>

        <div class="otp-icon"><app-icon name="smartphone" [size]="28" /></div>

        <app-otp-input
          [(value)]="code"
          [invalid]="!!error()"
          [disabled]="verifying()"
          (completed)="verify()" />

        @if (error()) {
          <div class="alert alert--error" role="alert"><app-icon name="info" [size]="18" /> {{ error() }}</div>
        }

        <button class="btn btn--primary btn--lg btn--block" type="button"
                [disabled]="code().length !== 6 || verifying()" (click)="verify()">
          @if (verifying()) { <span class="spinner"></span> } Giriş yap
        </button>

        <div class="row" style="justify-content:space-between">
          <span class="countdown" [class.is-low]="remaining() < 60">
            Oturum süresi: <strong>{{ remainingText() }}</strong>
          </span>
          <a routerLink="/login" class="small">Farklı hesapla gir</a>
        </div>
      </section>
    </div>
  `,
  styles: `
    .otp-icon {
      display: grid; place-items: center; width: 60px; height: 60px; margin: 0 auto;
      border-radius: 18px; background: var(--primary-soft); color: var(--primary);
    }
  `,
})
export class MfaVerify {
  private readonly auth = inject(AuthService);
  private readonly flow = inject(MfaFlow);
  protected readonly mfa = inject(MfaSessionService);

  readonly returnUrl = input<string>();

  protected readonly code = signal('');
  protected readonly verifying = signal(false);
  protected readonly error = signal<string | null>(null);
  private readonly otp = viewChild(OtpInput);

  protected readonly remaining = countdown(() => this.mfa.pending()?.expiresAt);
  protected readonly remainingText = computed(() => formatDuration(this.remaining()));

  constructor() {
    effect(() => {
      const length = this.code().length;
      if (length > 0 && length < 6) this.error.set(null);
    });

    effect(() => {
      if (this.remaining() === 0 && this.mfa.pending()) this.flow.expired();
    });
  }

  protected verify(): void {
    const code = this.code();
    if (code.length !== 6 || this.verifying()) return;

    this.verifying.set(true);
    this.auth.verifyAuthenticator(code).subscribe({
      next: () => this.flow.completed(this.returnUrl()),
      error: (error) => {
        this.verifying.set(false);
        if (this.flow.handleFatal(error)) return;
        this.error.set(errorMessage(error));
        this.otp()?.reset();
      },
    });
  }
}
