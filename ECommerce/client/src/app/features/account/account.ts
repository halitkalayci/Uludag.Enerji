import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { countdown, formatDuration } from '../../core/utils/countdown';
import { errorMessage } from '../../core/utils/error-message';
import { Icon } from '../../shared/components/icon';

// Oturum bilgileri ve token işlemleri: refresh (rotasyon), bu cihazdan çıkış (revoke), tüm cihazlardan çıkış (logout).
@Component({
  selector: 'app-account',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, Icon],
  template: `
    <div class="container page">
      <header class="page-header">
        <div>
          <span class="eyebrow">Hesap</span>
          <h1>Hesabım ve oturum</h1>
          <p>Oturum token'larınızı görüntüleyin ve yönetin.</p>
        </div>
      </header>

      @if (auth.session(); as session) {
        <div class="profile card">
          <span class="avatar">{{ session.email.charAt(0).toLocaleUpperCase('tr-TR') }}</span>
          <div>
            <h2>{{ session.email }}</h2>
            <div class="row" style="margin-top: 6px">
              <span class="badge badge--success"><app-icon name="shield" [size]="13" /> İki adımlı doğrulama etkin</span>
            </div>
          </div>
        </div>

        <div class="grid">
          <section class="card card-body token">
            <div class="token__head">
              <span class="token__icon"><app-icon name="key" /></span>
              <div>
                <h3>Erişim token'ı (JWT)</h3>
                <p class="small muted">Her API isteğinde Bearer olarak gönderilir.</p>
              </div>
            </div>
            <div class="token__value mono">{{ mask(session.accessToken) }}</div>
            <dl>
              <div><dt>Bitiş</dt><dd>{{ session.expiration | date: 'd MMM y, HH:mm:ss' }}</dd></div>
              <div><dt>Kalan süre</dt><dd [class.low]="accessLeft() < 120">{{ accessLeftText() }}</dd></div>
            </dl>
            <div class="meter"><span [style.width.%]="accessPercent()"></span></div>
          </section>

          <section class="card card-body token">
            <div class="token__head">
              <span class="token__icon token__icon--accent"><app-icon name="refresh" /></span>
              <div>
                <h3>Yenileme token'ı</h3>
                <p class="small muted">Her kullanımda yenisiyle değiştirilir (rotasyon).</p>
              </div>
            </div>
            <div class="token__value mono">{{ mask(session.refreshToken) }}</div>
            <dl>
              <div><dt>Bitiş</dt><dd>{{ session.refreshTokenExpiration | date: 'd MMM y, HH:mm' }}</dd></div>
              <div><dt>Kalan süre</dt><dd>{{ refreshLeftText() }}</dd></div>
            </dl>
            <button type="button" class="btn btn--sm" (click)="refresh()" [disabled]="busy()">
              @if (busy() === 'refresh') { <span class="spinner"></span> } <app-icon name="refresh" [size]="15" /> Token'ı şimdi yenile
            </button>
          </section>
        </div>

        <section class="card card-body stack">
          <h3>Oturumu kapat</h3>
          <div class="logout">
            <div class="logout__option">
              <div>
                <strong>Bu cihazdan çık</strong>
                <p class="small muted">Yalnızca bu tarayıcının yenileme token'ı iptal edilir. Diğer cihazlarınız açık kalır.</p>
              </div>
              <button type="button" class="btn" (click)="logoutThisDevice()" [disabled]="busy()">
                @if (busy() === 'device') { <span class="spinner"></span> } <app-icon name="logout" [size]="16" /> Bu cihazdan çık
              </button>
            </div>
            <div class="logout__option">
              <div>
                <strong>Tüm cihazlardan çık</strong>
                <p class="small muted">Hesabınızın tüm yenileme token'ları ve açık oturumları kapatılır.</p>
              </div>
              <button type="button" class="btn btn--danger" (click)="logoutEverywhere()" [disabled]="busy()">
                @if (busy() === 'all') { <span class="spinner"></span> } <app-icon name="shield" [size]="16" /> Her yerden çık
              </button>
            </div>
          </div>
        </section>
      }
    </div>
  `,
  styles: `
    .profile { display: flex; align-items: center; gap: 18px; padding: 22px 24px; margin-bottom: 20px; }
    .profile h2 { font-size: 22px; word-break: break-all; }
    .avatar { display: grid; place-items: center; width: 60px; height: 60px; border-radius: 50%; background: var(--accent-soft); color: var(--accent); font: 700 26px var(--font-display); flex-shrink: 0; }
    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-bottom: 20px; }
    @media (max-width: 800px) { .grid { grid-template-columns: 1fr; } }
    .token { display: flex; flex-direction: column; gap: 14px; }
    .token__head { display: flex; gap: 12px; align-items: center; }
    .token__icon { display: grid; place-items: center; width: 42px; height: 42px; border-radius: 12px; background: var(--primary-soft); color: var(--primary); flex-shrink: 0; }
    .token__icon--accent { background: var(--accent-soft); color: var(--accent); }
    .token__value { padding: 10px 12px; font-size: 12px; border-radius: var(--radius-sm); background: var(--surface-muted); border: 1px solid var(--border); color: var(--text-soft); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    dl { margin: 0; display: flex; flex-direction: column; gap: 6px; font-size: 14px; }
    dl div { display: flex; justify-content: space-between; gap: 12px; }
    dt { color: var(--text-muted); }
    dd { margin: 0; font-weight: 600; font-variant-numeric: tabular-nums; }
    dd.low { color: var(--danger); }
    .meter { height: 6px; border-radius: 999px; background: var(--bg-soft); overflow: hidden; }
    .meter span { display: block; height: 100%; background: linear-gradient(90deg, var(--primary), #3c9a76); transition: width 1s linear; }
    .token > button { align-self: flex-start; }
    .logout { display: flex; flex-direction: column; }
    .logout__option { display: flex; justify-content: space-between; align-items: center; gap: 16px; padding: 14px 0; border-top: 1px solid var(--border); flex-wrap: wrap; }
    .logout__option p { margin-top: 2px; max-width: 52ch; }
  `,
})
export class Account {
  protected readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly busy = signal<'refresh' | 'device' | 'all' | null>(null);

  protected readonly accessLeft = countdown(() => this.auth.session()?.expiration);
  protected readonly refreshLeft = countdown(() => this.auth.session()?.refreshTokenExpiration);
  protected readonly accessLeftText = computed(() =>
    this.accessLeft() > 0 ? formatDuration(this.accessLeft()) : 'Süresi doldu (ilk istekte yenilenir)',
  );
  protected readonly refreshLeftText = computed(() => formatDuration(this.refreshLeft()));
  // AccessTokenExpiration: 60 dk (appsettings).
  protected readonly accessPercent = computed(() => Math.min(100, (this.accessLeft() / 3600) * 100));

  protected mask(token: string): string {
    return token.length > 24 ? `${token.slice(0, 14)}…${token.slice(-10)}` : token;
  }

  protected refresh(): void {
    this.busy.set('refresh');
    this.auth.refresh().subscribe({
      next: () => {
        this.busy.set(null);
        this.toast.success('Yeni token çifti alındı; eski yenileme token\'ı geçersiz kılındı.');
      },
      error: (error) => {
        this.busy.set(null);
        this.toast.error(errorMessage(error));
      },
    });
  }

  protected logoutThisDevice(): void {
    this.busy.set('device');
    this.auth.logoutThisDevice().subscribe({
      next: () => this.toast.success('Bu cihazdan çıkış yapıldı.'),
      error: () => this.toast.info('Oturum bu cihazda kapatıldı.'),
    });
    void this.router.navigate(['/']);
  }

  protected logoutEverywhere(): void {
    if (!confirm('Tüm cihazlardaki oturumlarınız kapatılsın mı?')) return;
    this.busy.set('all');
    this.auth.logoutEverywhere().subscribe({
      next: () => void this.router.navigate(['/login'], { queryParams: { reason: 'logged-out' } }),
      error: (error) => {
        this.toast.error(errorMessage(error));
        void this.router.navigate(['/login']);
      },
    });
  }
}
