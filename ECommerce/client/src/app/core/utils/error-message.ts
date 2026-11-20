import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../models/api.models';

// Backend'in ProblemDetails.detail alanında döndürdüğü kural kodları -> kullanıcıya gösterilecek metin.
const MESSAGES: Record<string, string> = {
  UserEmailAlreadyExists: 'Bu e-posta adresiyle zaten bir hesap var.',
  InvalidEmail: 'Geçerli bir e-posta adresi girin.',
  PasswordRequired: 'Şifre boş olamaz.',
  InvalidEmailOrPassword: 'E-posta veya şifre hatalı.',
  UserNotFound: 'Kullanıcı bulunamadı.',
  OperationClaimNotFound: 'Böyle bir yetki tanımlı değil.',
  UserAlreadyHasOperationClaim: 'Kullanıcı bu yetkiye zaten sahip.',
  InvalidMultiFactorToken: 'Doğrulama oturumunuz geçersiz ya da süresi doldu. Lütfen tekrar giriş yapın.',
  MultiFactorAlreadyEnabled: 'İki adımlı doğrulama bu hesapta zaten etkin.',
  OtpAuthenticatorNotSetUp: 'Authenticator kurulumu bulunamadı. Lütfen kurulumu yeniden başlatın.',
  InvalidAuthenticatorCode: 'Doğrulama kodu hatalı. Uygulamadaki güncel kodu girin.',
  AuthenticatorCodeAlreadyUsed: 'Bu doğrulama kodu zaten kullanıldı. Uygulamada yeni kodu bekleyin.',
  TooManyAuthenticatorAttempts: 'Çok fazla hatalı deneme yapıldı. Bir süre sonra tekrar deneyin.',
  InvalidRefreshToken: 'Oturumunuz geçersiz. Lütfen tekrar giriş yapın.',
  RefreshTokenExpired: 'Oturumunuzun süresi doldu. Lütfen tekrar giriş yapın.',
  RefreshTokenReuseDetected: 'Güvenlik nedeniyle oturumunuz kapatıldı. Lütfen tekrar giriş yapın.',
  InvalidTokenSession: 'Oturumunuz zaten kapatılmış.',
  OrderNotFound: 'Sipariş bulunamadı.',
  OrderMustContainAtLeastOneItem: 'Sipariş en az bir ürün içermelidir.',
  PaymentDeclined: 'Ödeme reddedildi. Lütfen tekrar deneyin.',
};

export function errorMessage(error: unknown, fallback = 'Beklenmeyen bir hata oluştu.'): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0) {
    return 'Sunucuya ulaşılamadı. API çalışıyor mu?';
  }

  if (error.status === 429) {
    const retryAfter = error.headers.get('Retry-After');
    return retryAfter
      ? `Çok fazla istek gönderildi. ${retryAfter} saniye sonra tekrar deneyin.`
      : 'Çok fazla istek gönderildi. Biraz sonra tekrar deneyin.';
  }

  const problem = (typeof error.error === 'object' ? error.error : null) as ProblemDetails | null;
  const detail = problem?.detail;

  if (detail && MESSAGES[detail]) {
    return MESSAGES[detail];
  }

  if (problem?.errors) {
    const first = Object.values(problem.errors).flat()[0];
    if (first) return first;
  }

  switch (error.status) {
    case 401:
      return 'Bu işlem için giriş yapmalısınız.';
    case 403:
      return 'Bu işlem için yetkiniz yok.';
    case 404:
      return detail ?? 'Kayıt bulunamadı.';
    case 500:
      return 'Sunucuda bir hata oluştu.';
  }

  return detail ?? fallback;
}
