import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from './core/services/auth.service';
import { CartService } from './core/services/cart.service';
import { ToastService } from './core/services/toast.service';
import { errorMessage } from './core/utils/error-message';
import { Icon } from './shared/components/icon';
import { ToastContainer } from './shared/components/toast-container';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Icon, ToastContainer],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly auth = inject(AuthService);
  protected readonly cart = inject(CartService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  protected readonly menuOpen = signal(false);
  protected readonly accountOpen = signal(false);
  protected readonly year = new Date().getFullYear();

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => {
        this.menuOpen.set(false);
        this.accountOpen.set(false);
      });
  }

  protected initial(email: string | null): string {
    return (email ?? '?').charAt(0).toLocaleUpperCase('tr-TR');
  }

  protected logout(): void {
    this.accountOpen.set(false);
    // Bu cihazın refresh token'ı iptal edilir; istek başarısız olsa da oturum yerelde her durumda kapanır.
    this.auth.logoutThisDevice().subscribe({
      next: () => this.toast.success('Çıkış yapıldı.'),
      error: (error) => this.toast.info(`Oturum bu cihazda kapatıldı. ${errorMessage(error, '')}`.trim()),
    });
    void this.router.navigate(['/']);
  }
}
