# Uludağ Market — Angular istemcisi

`ECommerce.WebAPI` için Angular 22 (standalone, signals, zoneless) istemcisi.

## Çalıştırma

```bash
# 1) API (https profili: https://localhost:7155)
dotnet run --project ../src/ECommerce.WebAPI --launch-profile https

# 2) İstemci
npm install
npm start            # http://localhost:4200
```

İstekler `proxy.conf.json` ile `/api` → `https://localhost:7155` adresine yönlenir; backend'de CORS gerekmez.
API'yi `http` profiliyle (5057) çalıştırıyorsanız proxy hedefini `http://localhost:5057` yapın.

## Özellikler

| Ekran | API |
|---|---|
| Kayıt / Giriş | `POST /api/auth/register`, `POST /api/auth/login` |
| Authenticator kurulumu (QR) | `POST /api/auth/mfa/setup` → `otpauth://` adresi tarayıcıda QR'a çevrilir (`qrcode` paketi) |
| OTP doğrulama (6 kutu, yapıştırma) | `POST /api/auth/mfa/verify` |
| Oturum yenileme | 401 → `POST /api/auth/refresh-token` (tek istek paylaşılır, rotasyon) → istek tekrarlanır |
| Çıkış | Bu cihaz: `revoke-token`, tüm cihazlar: `logout` |
| Ana sayfa / ürün detayı | `GET /api/products`, `GET /api/products/{id}` |
| Sepet → sipariş | `POST /api/orders` (para birimi başına bir sipariş) |
| Siparişler | `GET/PUT/DELETE /api/orders`, `GET/POST/PUT/DELETE /api/orderitems` |
| Ürün yönetimi | `POST/PUT/DELETE /api/products` |
| Yetki atama | `POST /api/auth/assign-operation-claim` (yalnızca Admin) |

Rol bilgisi JWT'de taşınmadığı (oturum Redis'te) için menüler herkese gösterilir; yetkisiz işlemler
backend'in 403 cevabıyla kullanıcıya açıklanır.

## Yapı

```
src/app/
  core/       modeller, servisler (auth, mfa, ürün, sipariş, sepet, toast), interceptor, guard, yardımcılar
  shared/     ikon, OTP girişi, QR kod, ürün görseli, toast, para pipe'ı
  features/   home, product-detail, cart, auth (login/register/mfa), orders, admin, account
```
