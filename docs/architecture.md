# Architecture

> Bootstrap 2026-11-19'da mevcut koddan çıkarıldı ve onaylandı. Agent'lar plan yapmadan önce bu
> dosyayı okur.

## System overview
Eğitim/referans amaçlı bir ECommerce API: ürün, sipariş, sipariş kalemi, kimlik doğrulama
(JWT + MFA/TOTP + refresh token rotasyonu) ve operation-claim tabanlı yetkilendirme. Tarz:
**Clean Architecture ile katmanlı monolit** — tek deploy birimi `ECommerce.WebAPI`; CQRS istekleri
kendi `Uludag.Core` mediator'ı üzerinden akar. Yanında Angular 22 istemci (`ECommerce/client`) ve
başka projelerde de kullanılacak paylaşılan çekirdek `Uludag.Core` bulunur. Gerekçe: katmanların ve
desenlerin (mediator, pipeline behavior, business rules, mapper) görünür ve öğretilebilir olması.

## Modules / components and ownership

| Module | Single responsibility | Owns |
|---|---|---|
| `ECommerce.Domain` | Entity'ler ve value object'ler (`Money`); saf C# | İş kavramlarının şekli ve VO değişmezleri |
| `ECommerce.Application` | Use case'ler: `Features/<X>/{Commands,Queries,Rules,Mappers,Constants,Locales}`, repository/servis arayüzleri | İş kuralları (BR-n), orkestrasyon, mesaj anahtarları |
| `ECommerce.Persistence` | EF Core `UludagDbContext`, konfigürasyonlar, repository implementasyonları, seed'ler, migration'lar | Veritabanı şeması ve kalıcılık |
| `ECommerce.Infrastructure` | Dış servisler için ayrılmış; **şu an boş** | — |
| `ECommerce.WebAPI` | HTTP giriş noktası: controller'lar, middleware (ProblemDetails, rate limiting), DI kompozisyonu | HTTP sözleşmesi |
| `Uludag.Core` | Yeniden kullanılabilir çekirdek: mediator + behavior'lar, JWT, MFA/OTP, hashing/şifreleme, cache, Leaky Bucket rate limiting | Çapraz kesen altyapı; hiçbir ECommerce kavramı |
| `ECommerce/client` | Angular 22 SPA (`core/`, `features/`, `shared/`) | Kullanıcı arayüzü; API'ye `proxy.conf.json` ile bağlanır |

Feature sahipliği (Application içinde): **Auth** → User, OperationClaim, UserOperationClaim,
OtpAuthenticator, RefreshToken · **Product** → Product · **Order** → Order (kalemleriyle aggregate) ·
**OrderItem** → OrderItem.

## Communication rules
- HTTP → Controller → `IMediator.Send(...)` → handler. Controller iş mantığı içermez.
- Pipeline behavior'ları (`AuthenticationBehavior`, `AuthorizationBehavior`, `LoggingBehavior`) her
  isteği handler'dan önce sarar.
- Handler kendi entity'sine **repository** ile, başka feature'ın entity'sine o feature'ın
  **servisi** ile erişir (`Services/<X>Service`, örn. `IProductService`).
- Kural ihlali exception olarak middleware'e kadar çıkar (`docs/conventions.md`, Error handling).
- Event/mesajlaşma yok; tüm çağrılar süreç içi ve senkron (async/await).

## Forbidden dependencies (make them testable)
- **F-1** `ECommerce.Domain` hiçbir projeye ve pakete referans vermez.
- **F-2** `ECommerce.Application` yalnızca `ECommerce.Domain` ve `Uludag.Core`'a bağlıdır; Persistence,
  Infrastructure, WebAPI ve EF Core'a (`Microsoft.EntityFrameworkCore*`) asla.
- **F-3** `Uludag.Core` hiçbir `ECommerce.*` projesine/tipine referans vermez.
- **F-4** `ECommerce.Persistence` `ECommerce.WebAPI`'ye referans vermez.
- **F-5** `ECommerce.WebAPI` controller'ları yalnızca `IMediator` kullanır; repository, `DbContext` veya
  servis enjekte etmez.
- **F-6** Bir handler başka feature'ın repository'sini kullanmaz; o feature'ın servisini kullanır
  (Auth feature'ı kendi kimlik entity'lerinin repository'lerini kullanabilir).
- **F-7** Command/Query handler'larında `if`/`else` ile durum kontrolü ve mapping yoktur: kontroller
  `Features/<X>/Rules` veya servislerde, dönüşümler `Features/<X>/Mappers/<Entity>Mapper` içindedir.

F-1…F-5 mimari testle (NetArchTest, `docs/testing.md`) doğrulanacak; F-6 ve F-7 review'da kontrol edilir.

## Deliberately out of scope
- Mikroservisler, event bus / mesaj kuyruğu, CQRS için ayrı okuma veritabanı.
- `ECommerce.Infrastructure`'ın doldurulması (ihtiyaç doğana kadar).
- Gerçek ödeme, kargo, fatura entegrasyonları.
- Postgres: aktif provider SQL Server; `_migrations_postgres_backup/` yalnızca yedek.
