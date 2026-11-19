import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon } from '../../shared/components/icon';

// Kimlik ekranlarının sol paneli: başlık + giriş akışının adımları (1 şifre, 2 kurulum, 3 kod).
@Component({
  selector: 'app-auth-aside',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon],
  template: `
    <div>
      <span class="badge-light"><app-icon name="shield" [size]="14" /> İki adımlı doğrulama</span>
      <h2>{{ title() }}</h2>
      <p>{{ text() }}</p>
    </div>
    <ol class="steps">
      @for (step of steps; track step.no) {
        <li [class.is-active]="step.no === current()" [class.is-done]="step.no < current()">
          <span class="step__dot">
            @if (step.no < current()) { <app-icon name="check" [size]="14" /> } @else { {{ step.no }} }
          </span>
          {{ step.label }}
        </li>
      }
    </ol>
  `,
  styles: `
    :host {
      position: relative;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      gap: 32px;
      padding: 40px;
      color: #eef5f1;
      background:
        radial-gradient(circle at calc(100% - 56px) 52%, rgba(255, 255, 255, 0.9) 0 20px, transparent 21px),
        linear-gradient(165deg, #2a7a5f 0%, #1f5f4a 45%, #143f31 100%);
      overflow: hidden;
      min-height: 520px;
    }
    :host::after {
      content: '';
      position: absolute;
      left: 0; right: 0; bottom: 0;
      height: 46%;
      background: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 400 160' preserveAspectRatio='none'%3E%3Cpath d='M0 160 L0 110 L70 60 L120 95 L190 20 L250 80 L300 50 L400 115 L400 160Z' fill='%23ffffff' fill-opacity='.10'/%3E%3Cpath d='M170 44 L190 20 L208 38 L196 34 L186 42Z' fill='%23ffffff' fill-opacity='.6'/%3E%3Cpath d='M0 160 L0 135 L90 95 L160 125 L240 70 L330 120 L400 100 L400 160Z' fill='%23ffffff' fill-opacity='.16'/%3E%3C/svg%3E") bottom / 100% 100% no-repeat;
      pointer-events: none;
    }
    h2 { color: #fff; font-size: 30px; margin-top: 18px; }
    p { color: rgba(238, 245, 241, 0.82); margin-top: 10px; max-width: 34ch; }
    .badge-light {
      display: inline-flex; align-items: center; gap: 6px;
      padding: 5px 11px; border-radius: 999px;
      background: rgba(255, 255, 255, 0.14); font-size: 12px; font-weight: 600;
    }
    .steps {
      position: relative; z-index: 1;
      display: flex; flex-direction: column; gap: 14px;
      margin: 0 0 40px; padding: 0; list-style: none;
    }
    li { display: flex; align-items: center; gap: 12px; font-size: 14px; color: rgba(238, 245, 241, 0.65); }
    li.is-active { color: #fff; font-weight: 600; }
    li.is-done { color: rgba(238, 245, 241, 0.9); }
    .step__dot {
      display: grid; place-items: center; width: 28px; height: 28px;
      border-radius: 50%; border: 1.5px solid rgba(255, 255, 255, 0.4);
      font-size: 12px; font-weight: 700;
    }
    .is-active .step__dot { background: #fff; color: var(--primary); border-color: #fff; }
    .is-done .step__dot { background: rgba(255, 255, 255, 0.18); border-color: transparent; }
  `,
})
export class AuthAside {
  readonly title = input.required<string>();
  readonly text = input('');
  readonly current = input(1);

  protected readonly steps = [
    { no: 1, label: 'E-posta ve şifre' },
    { no: 2, label: 'Authenticator kurulumu (ilk giriş)' },
    { no: 3, label: '6 haneli kod ile doğrulama' },
  ];
}
