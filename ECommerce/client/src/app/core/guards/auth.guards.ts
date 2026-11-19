import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { MultiFactorStatus } from '../models/api.models';
import { AuthService } from '../services/auth.service';
import { MfaSessionService } from '../services/mfa-session.service';

// Giriş yapılmış olmalı; değilse login'e (dönüş adresiyle).
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isAuthenticated()
    ? true
    : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

// Giriş yapmış kullanıcı login / register sayfalarını görmesin.
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isAuthenticated() ? router.createUrlTree(['/']) : true;
};

// MFA ekranları yalnızca geçerli bir MFA oturumu varken açılır; adım da doğru olmalı.
export function mfaGuard(step: MultiFactorStatus): CanActivateFn {
  return () => {
    const mfa = inject(MfaSessionService);
    const router = inject(Router);

    if (!mfa.isValid()) {
      mfa.clear();
      return router.createUrlTree(['/login'], { queryParams: { reason: 'mfa-expired' } });
    }

    if (mfa.status() !== step) {
      return router.createUrlTree([mfa.status() === 'SetupRequired' ? '/mfa/setup' : '/mfa/verify']);
    }

    return true;
  };
}
