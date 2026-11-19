import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, finalize, shareReplay, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AssignOperationClaimRequest,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  SetupOtpAuthenticatorResponse,
  TokenResponse,
} from '../models/api.models';
import { readJson, writeJson } from '../utils/storage';
import { MfaSessionService } from './mfa-session.service';

export interface AuthSession extends TokenResponse {
  email: string;
}

const KEY = 'uludag.auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly mfa = inject(MfaSessionService);
  private readonly base = `${environment.apiUrl}/auth`;

  private readonly state = signal<AuthSession | null>(readJson<AuthSession>(localStorage, KEY));
  private refreshInFlight$: Observable<TokenResponse> | null = null;

  readonly session = this.state.asReadonly();
  readonly isAuthenticated = computed(() => this.state() !== null);
  readonly email = computed(() => this.state()?.email ?? null);
  readonly accessToken = computed(() => this.state()?.accessToken ?? null);

  register(request: RegisterRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/register`, request);
  }

  // Login JWT DÖNMEZ: MFA token'ı + sıradaki adım (SetupRequired / CodeRequired).
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.base}/login`, request)
      .pipe(tap((response) => this.mfa.start(request.email, response)));
  }

  setupAuthenticator(): Observable<SetupOtpAuthenticatorResponse> {
    const pending = this.mfa.pending();
    if (!pending) return throwError(() => new Error('MFA oturumu yok.'));
    return this.http.post<SetupOtpAuthenticatorResponse>(`${this.base}/mfa/setup`, {
      multiFactorToken: pending.token,
    });
  }

  // Kod doğruysa JWT + refresh token gelir; oturum burada açılır.
  verifyAuthenticator(code: string): Observable<TokenResponse> {
    const pending = this.mfa.pending();
    if (!pending) return throwError(() => new Error('MFA oturumu yok.'));
    return this.http
      .post<TokenResponse>(`${this.base}/mfa/verify`, {
        multiFactorToken: pending.token,
        authenticatorCode: code,
      })
      .pipe(
        tap((tokens) => {
          this.store({ ...tokens, email: pending.email });
          this.mfa.clear();
        }),
      );
  }

  // Refresh token rotasyonu: her çağrıda yeni JWT + YENİ refresh token gelir, eskisi geçersiz olur.
  // Aynı anda 401 alan birden fazla istek tek bir yenileme çağrısını paylaşır
  // (aynı refresh token iki kez gönderilirse backend bunu "reuse" sayıp aileyi iptal eder).
  refresh(): Observable<TokenResponse> {
    const current = this.state();
    if (!current) return throwError(() => new Error('Oturum yok.'));

    this.refreshInFlight$ ??= this.http
      .post<TokenResponse>(`${this.base}/refresh-token`, { refreshToken: current.refreshToken })
      .pipe(
        tap((tokens) => this.store({ ...tokens, email: current.email })),
        finalize(() => (this.refreshInFlight$ = null)),
        shareReplay(1),
      );

    return this.refreshInFlight$;
  }

  // Tüm cihazlardan çıkış: kullanıcının bütün refresh token'ları ve JWT oturumları kapatılır.
  logoutEverywhere(): Observable<void> {
    return this.http.post<void>(`${this.base}/logout`, {}).pipe(finalize(() => this.clearSession()));
  }

  // Yalnızca bu cihaz: bu cihazın refresh token'ı iptal edilir.
  logoutThisDevice(): Observable<void> {
    const current = this.state();
    if (!current) {
      this.clearSession();
      return throwError(() => new Error('Oturum yok.'));
    }
    return this.http
      .post<void>(`${this.base}/revoke-token`, { refreshToken: current.refreshToken })
      .pipe(finalize(() => this.clearSession()));
  }

  assignOperationClaim(request: AssignOperationClaimRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/assign-operation-claim`, request);
  }

  clearSession(): void {
    this.store(null);
  }

  private store(session: AuthSession | null): void {
    this.state.set(session);
    writeJson(localStorage, KEY, session);
  }
}
