import { Injectable, computed, signal } from '@angular/core';
import { LoginResponse, MultiFactorStatus } from '../models/api.models';
import { readJson, writeJson } from '../utils/storage';

// Login ile JWT arasındaki ara adım: kısa ömürlü MFA token'ı.
// Sekme yenilenirse kaybolmasın diye sessionStorage'da tutulur (sekme kapanınca silinir).
export interface PendingMfa {
  email: string;
  status: MultiFactorStatus;
  token: string;
  expiresAt: string; // UTC ISO
}

const KEY = 'uludag.mfa';

@Injectable({ providedIn: 'root' })
export class MfaSessionService {
  private readonly state = signal<PendingMfa | null>(readJson<PendingMfa>(sessionStorage, KEY));

  readonly pending = this.state.asReadonly();
  readonly status = computed(() => this.state()?.status ?? null);

  start(email: string, response: LoginResponse): void {
    this.set({
      email,
      status: response.multiFactorStatus,
      token: response.multiFactorToken,
      expiresAt: response.multiFactorTokenExpiration,
    });
  }

  isValid(): boolean {
    const current = this.state();
    return !!current && new Date(current.expiresAt).getTime() > Date.now();
  }

  clear(): void {
    this.set(null);
  }

  private set(value: PendingMfa | null): void {
    this.state.set(value);
    writeJson(sessionStorage, KEY, value);
  }
}
