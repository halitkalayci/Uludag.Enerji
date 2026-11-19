import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SetupOtpAuthenticatorResponse } from '../../core/models/api.models';
import { AuthService } from '../../core/services/auth.service';
import { MfaSessionService } from '../../core/services/mfa-session.service';
import { ToastService } from '../../core/services/toast.service';
import { countdown, formatDuration } from '../../core/utils/countdown';
import { errorMessage } from '../../core/utils/error-message';
import { Icon } from '../../shared/components/icon';
import { OtpInput } from '../../shared/components/otp-input';
import { QrCode } from '../../shared/components/qr-code';
import { AuthAside } from './auth-aside';
import { MfaFlow } from './mfa-flow';

// İlk giriş: backend gizli anahtarı + otpauth:// adresini döner, burada QR'a çevrilir.
// Kullanıcı QR'ı authenticator uygulamasına okutup ilk kodu girince MFA aktif olur ve JWT alınır.
@Component({
  selector: 'app-mfa-setup',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, Icon, OtpInput, QrCode, AuthAside],
  styleUrls: ['./auth-layout.scss', './mfa-setup.scss'],
  template: `
    <div class="auth">
      <app-auth-aside
        title="Telefonunuzu bağlayın"
        text="Google Authenticator, Microsoft Authenticator veya benzeri bir uygulama ile QR kodu okutun."
        [current]="2" />

      <section class="auth__main">
        <header>
          <span class="eyebrow">Adım 2 / 3 · Kurulum</span>
          <h1>Authenticator kurulumu</h1>
          <p><strong>{{ mfa.pending()?.email }}</strong> hesabı için iki adımlı doğrulamayı etkinleştirin.</p>
        </header>

        @if (setupError()) {
          <div class="alert alert--error" role="alert">
            <app-icon name="info" [size]="18" />
            <div class="stack" style="gap:8px">
              <span>{{ setupError() }}</span>
              <div><button class="btn btn--sm" type="button" (click)="startSetup()">Tekrar dene</button></div>
            </div>
          </div>
        }

        <div class="setup">
          <div class="setup__qr">
            @if (setup(); as data) {
              <app-qr-code [value]="data.qrCodeUri" [size]="188" alt="Authenticator QR kodu" />
            } @else {
              <div class="skeleton" style="width:188px;height:188px"></div>
            }
          </div>

          <ol class="setup__steps">
            <li>Telefonunuzda authenticator uygulamasını açın.</li>
            <li><strong>QR kod tara</strong> seçeneğiyle soldaki kodu okutun.</li>
            <li>Uygulamanın ürettiği 6 haneli kodu aşağıya girin.</li>
          </ol>
        </div>

        @if (setup(); as data) {
          <details class="manual">
            <summary>QR okutamıyor musunuz? Anahtarı elle girin</summary>
            <div class="manual__body">
              <code class="secret">{{ groupedSecret() }}</code>
              <div class="row">
                <button type="button" class="btn btn--sm" (click)="copySecret(data.secretKey)">
                  <app-icon name="copy" [size]="15" /> Kopyala
                </button>
                <a class="btn btn--sm btn--ghost" [href]="data.qrCodeUri">
                  <app-icon name="smartphone" [size]="15" /> Bu cihazda aç
                </a>
              </div>
              <span class="small muted">Tür: zamana dayalı (TOTP) · 6 hane · 30 sn</span>
            </div>
          </details>
        }

        <div class="verify">
          <label class="verify__label">Doğrulama kodu</label>
          <app-otp-input
            [(value)]="code"
            [invalid]="!!verifyError()"
            [disabled]="!setup() || verifying()"
            (completed)="verify()" />

          @if (verifyError()) {
            <div class="alert alert--error" role="alert"><app-icon name="info" [size]="18" /> {{ verifyError() }}</div>
          }

          <button class="btn btn--primary btn--lg btn--block" type="button"
                  [disabled]="code().length !== 6 || verifying() || !setup()" (click)="verify()">
            @if (verifying()) { <span class="spinner"></span> } Doğrula ve etkinleştir
          </button>

          <div class="row" style="justify-content:space-between">
            <span class="countdown" [class.is-low]="remaining() < 60">
              Oturum süresi: <strong>{{ remainingText() }}</strong>
            </span>
            <a routerLink="/login" class="small">Farklı hesapla gir</a>
          </div>
        </div>
      </section>
    </div>
  `,
})
export class MfaSetup {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly flow = inject(MfaFlow);
  protected readonly mfa = inject(MfaSessionService);

  readonly returnUrl = input<string>();

  protected readonly setup = signal<SetupOtpAuthenticatorResponse | null>(null);
  protected readonly setupError = signal<string | null>(null);
  protected readonly code = signal('');
  protected readonly verifying = signal(false);
  protected readonly verifyError = signal<string | null>(null);

  private readonly otp = viewChild(OtpInput);

  protected readonly remaining = countdown(() => this.mfa.pending()?.expiresAt);
  protected readonly remainingText = computed(() => formatDuration(this.remaining()));
  protected readonly groupedSecret = computed(
    () => this.setup()?.secretKey.match(/.{1,4}/g)?.join(' ') ?? '',
  );

  constructor() {
    this.startSetup();

    // Kullanıcı kodu düzeltmeye başlayınca hata mesajını kaldır.
    effect(() => {
      const length = this.code().length;
      if (length > 0 && length < 6) this.verifyError.set(null);
    });

    effect(() => {
      if (this.remaining() === 0 && this.mfa.pending()) this.flow.expired();
    });
  }

  protected startSetup(): void {
    this.setupError.set(null);
    this.auth.setupAuthenticator().subscribe({
      next: (response) => this.setup.set(response),
      error: (error) => {
        if (!this.flow.handleFatal(error)) this.setupError.set(errorMessage(error));
      },
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
        this.verifyError.set(errorMessage(error));
        this.otp()?.reset();
      },
    });
  }

  protected copySecret(secret: string): void {
    navigator.clipboard?.writeText(secret).then(
      () => this.toast.success('Anahtar panoya kopyalandı.'),
      () => this.toast.error('Kopyalanamadı; anahtarı elle seçin.'),
    );
  }
}
