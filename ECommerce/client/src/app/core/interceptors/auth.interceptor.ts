import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

// Bearer token eklenmeyecek (anonim) auth uçları.
const ANONYMOUS_AUTH = /\/auth\/(login|register|mfa\/setup|mfa\/verify|refresh-token|revoke-token)$/;

function withBearer(request: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

// 1) Oturum varsa her isteğe "Authorization: Bearer <jwt>" ekler.
// 2) 401 gelirse refresh token ile yeni JWT alır ve isteği bir kez tekrarlar.
// 3) Yenileme de başarısızsa oturumu kapatıp giriş sayfasına yönlendirir.
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const isAnonymousAuth = ANONYMOUS_AUTH.test(request.url);
  const isLogout = request.url.endsWith('/auth/logout');
  const token = auth.accessToken();
  const outgoing = token && !isAnonymousAuth ? withBearer(request, token) : request;

  return next(outgoing).pipe(
    catchError((error: unknown) => {
      const canRefresh =
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !isAnonymousAuth &&
        !isLogout &&
        auth.isAuthenticated();

      if (!canRefresh) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        catchError(() => {
          auth.clearSession();
          void router.navigate(['/login'], {
            queryParams: { returnUrl: router.url, reason: 'expired' },
          });
          return throwError(() => error);
        }),
        switchMap((tokens) => next(withBearer(request, tokens.accessToken))),
      );
    }),
  );
};
