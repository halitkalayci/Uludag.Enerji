import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MfaSessionService } from '../../core/services/mfa-session.service';
import { ToastService } from '../../core/services/toast.service';
import { ProblemDetails } from '../../core/models/api.models';

// MFA ekranlarının ortak davranışı: başarı sonrası yönlendirme ve MFA oturumu bitince login'e dönüş.
@Injectable({ providedIn: 'root' })
export class MfaFlow {
  private readonly router = inject(Router);
  private readonly mfa = inject(MfaSessionService);
  private readonly toast = inject(ToastService);

  completed(returnUrl: string | undefined): void {
    this.toast.success('Giriş başarılı. Hoş geldiniz!');
    void this.router.navigateByUrl(returnUrl && returnUrl.startsWith('/') ? returnUrl : '/');
  }

  // MFA token'ı geçersizse bu ekranda yapılacak bir şey kalmaz: baştan giriş.
  handleFatal(error: unknown): boolean {
    const detail = error instanceof HttpErrorResponse ? (error.error as ProblemDetails | null)?.detail : null;
    if (detail === 'InvalidMultiFactorToken') {
      this.mfa.clear();
      void this.router.navigate(['/login'], { queryParams: { reason: 'mfa-expired' } });
      return true;
    }
    return false;
  }

  expired(): void {
    this.mfa.clear();
    void this.router.navigate(['/login'], { queryParams: { reason: 'mfa-expired' } });
  }
}
