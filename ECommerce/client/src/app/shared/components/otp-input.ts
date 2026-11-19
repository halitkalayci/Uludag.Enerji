import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  computed,
  input,
  model,
  output,
  viewChildren,
} from '@angular/core';

// 6 haneli authenticator kodu girişi: kutular arası otomatik geçiş, geri silme, yapıştırma desteği.
@Component({
  selector: 'app-otp-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="otp" [class.otp--error]="invalid()" role="group" aria-label="Doğrulama kodu">
      @for (index of slots(); track index) {
        <input
          #box
          class="otp__box"
          type="text"
          inputmode="numeric"
          autocomplete="one-time-code"
          maxlength="1"
          [attr.aria-label]="index + 1 + '. hane'"
          [value]="digitAt(index)"
          [disabled]="disabled()"
          (input)="onInput(index, $event)"
          (keydown)="onKeydown(index, $event)"
          (paste)="onPaste($event)"
          (focus)="select($event)"
        />
        @if (index === 2) {
          <span class="otp__sep" aria-hidden="true"></span>
        }
      }
    </div>
  `,
  styles: `
    .otp { display: flex; gap: 8px; justify-content: center; align-items: center; }
    .otp__box {
      width: 48px; height: 58px; text-align: center;
      font: 600 24px var(--font-mono); color: var(--text);
      border: 1.5px solid var(--border-strong); border-radius: var(--radius-md);
      background: var(--surface); transition: border-color .15s, box-shadow .15s;
      caret-color: var(--primary);
    }
    .otp__box:focus { outline: none; border-color: var(--primary); box-shadow: 0 0 0 4px var(--primary-ring); }
    .otp__box:disabled { opacity: .6; }
    .otp__sep { width: 10px; height: 2px; background: var(--border-strong); border-radius: 2px; }
    .otp--error .otp__box { border-color: var(--danger); animation: shake .3s; }
    @keyframes shake { 25% { transform: translateX(-3px); } 75% { transform: translateX(3px); } }
    @media (max-width: 420px) { .otp { gap: 5px; } .otp__box { width: 40px; height: 50px; font-size: 20px; } }
  `,
})
export class OtpInput {
  readonly length = input(6);
  readonly disabled = input(false);
  readonly invalid = input(false);
  readonly value = model('');
  readonly completed = output<string>();

  protected readonly slots = computed(() => Array.from({ length: this.length() }, (_, i) => i));
  private readonly boxes = viewChildren<ElementRef<HTMLInputElement>>('box');

  constructor() {
    afterNextRender(() => this.focus(0));
  }

  reset(): void {
    this.value.set('');
    this.focus(0);
  }

  protected onInput(index: number, event: Event): void {
    const target = event.target as HTMLInputElement;
    const digit = target.value.replace(/\D/g, '').slice(-1);
    target.value = digit;
    this.setDigit(index, digit);
    if (digit) this.focus(index + 1);
  }

  protected onKeydown(index: number, event: KeyboardEvent): void {
    if (event.key === 'Backspace' && !this.digitAt(index)) {
      event.preventDefault();
      this.setDigit(index - 1, '');
      this.focus(index - 1);
    } else if (event.key === 'ArrowLeft') {
      this.focus(index - 1);
    } else if (event.key === 'ArrowRight') {
      this.focus(index + 1);
    }
  }

  protected onPaste(event: ClipboardEvent): void {
    event.preventDefault();
    const digits = (event.clipboardData?.getData('text') ?? '').replace(/\D/g, '').slice(0, this.length());
    if (!digits) return;
    this.value.set(digits);
    this.focus(Math.min(digits.length, this.length() - 1));
    this.emitIfComplete();
  }

  protected digitAt(index: number): string {
    return (this.value()[index] ?? '').trim();
  }

  protected select(event: FocusEvent): void {
    (event.target as HTMLInputElement).select();
  }

  private setDigit(index: number, digit: string): void {
    if (index < 0 || index >= this.length()) return;
    const chars = this.value().padEnd(this.length(), ' ').split('');
    chars[index] = digit || ' ';
    this.value.set(chars.join('').trimEnd());
    this.emitIfComplete();
  }

  private emitIfComplete(): void {
    const current = this.value();
    if (current.length === this.length() && /^\d+$/.test(current)) {
      this.completed.emit(current);
    }
  }

  private focus(index: number): void {
    const boxes = this.boxes();
    if (index >= 0 && index < boxes.length) boxes[index].nativeElement.focus();
  }
}
