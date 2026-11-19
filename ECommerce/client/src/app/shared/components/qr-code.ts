import { ChangeDetectionStrategy, Component, effect, input, signal } from '@angular/core';
import QRCode from 'qrcode';

// Verilen metni (ör. backend'in döndürdüğü otpauth://totp/... adresi) tarayıcıda QR koda çevirir.
// Gizli anahtar hiçbir üçüncü taraf servise gönderilmez; QR tamamen istemcide üretilir.
@Component({
  selector: 'app-qr-code',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (dataUrl(); as src) {
      <img [src]="src" [width]="size()" [height]="size()" [alt]="alt()" />
    } @else if (failed()) {
      <div class="qr-fallback">QR kod oluşturulamadı</div>
    } @else {
      <div class="skeleton" [style.width.px]="size()" [style.height.px]="size()"></div>
    }
  `,
  styles: `
    :host { display: inline-block; line-height: 0; }
    img { display: block; image-rendering: pixelated; }
    .qr-fallback { padding: 24px; font-size: 13px; line-height: 1.4; color: var(--danger); }
  `,
})
export class QrCode {
  readonly value = input.required<string>();
  readonly size = input(220);
  readonly alt = input('QR kod');

  protected readonly dataUrl = signal<string | null>(null);
  protected readonly failed = signal(false);

  constructor() {
    effect(() => {
      const value = this.value();
      const size = this.size();
      this.dataUrl.set(null);
      this.failed.set(false);

      QRCode.toDataURL(value, {
        width: size * 2, // retina için 2x üret, CSS ile küçült
        margin: 1,
        errorCorrectionLevel: 'M',
        color: { dark: '#18221e', light: '#ffffff' },
      })
        .then((url) => this.dataUrl.set(url))
        .catch(() => this.failed.set(true));
    });
  }
}
