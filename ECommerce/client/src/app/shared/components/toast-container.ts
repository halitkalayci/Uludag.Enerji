import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../../core/services/toast.service';
import { Icon } from './icon';

@Component({
  selector: 'app-toast-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon],
  template: `
    <div class="toasts" aria-live="polite">
      @for (toast of toasts.toasts(); track toast.id) {
        <div class="toast" [class]="'toast toast--' + toast.kind" role="status">
          <app-icon [name]="toast.kind === 'success' ? 'check' : toast.kind === 'error' ? 'x' : 'info'" [size]="18" />
          <span>{{ toast.message }}</span>
          <button type="button" class="toast__close" (click)="toasts.dismiss(toast.id)" aria-label="Kapat">
            <app-icon name="x" [size]="14" />
          </button>
        </div>
      }
    </div>
  `,
  styles: `
    .toasts {
      position: fixed; right: 20px; bottom: 20px; z-index: 100;
      display: flex; flex-direction: column; gap: 10px; max-width: min(380px, calc(100vw - 32px));
    }
    .toast {
      display: flex; align-items: flex-start; gap: 10px;
      padding: 12px 14px; border-radius: var(--radius-md);
      background: var(--surface); border: 1px solid var(--border);
      box-shadow: var(--shadow-lg); font-size: 14px; line-height: 1.4;
      animation: toast-in .25s ease-out;
    }
    .toast span { flex: 1; padding-top: 1px; }
    .toast--success app-icon { color: var(--success); }
    .toast--error app-icon { color: var(--danger); }
    .toast--info app-icon { color: var(--primary); }
    .toast__close { border: 0; background: none; color: var(--text-muted); cursor: pointer; padding: 2px; }
    @keyframes toast-in { from { opacity: 0; transform: translateY(8px); } }
  `,
})
export class ToastContainer {
  protected readonly toasts = inject(ToastService);
}
