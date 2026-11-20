# Security

> Bootstrap 2026-11-19'da onaylandı. Agent'lar her plan ve review'da bu kurallara uyar.

## Secrets
- Secrets never enter the repo, specs, prompts, or chat. `.env` is gitignored; provide `.env.example`.
- Agents never print secret values, even when debugging.
- Development secret'ları `dotnet user-secrets` veya ortam değişkenlerinde durur
  (`TokenOptions__SecurityKey`, `ConnectionStrings__ECommerce`, `RedisOptions__ConnectionString`,
  `AdminSeed__Password`).
- **Bilinen ihlal (B-1, öncelikli):** git'te takip edilen `appsettings.Development.json` ve
  `appsettings.json` secret içeriyor. Taşıma + `SecurityKey` rotasyonu ayrı bir fix işiyle
  (hassas alan) yapılacak; o zamana kadar yeni secret eklenmez.

## Input & output
- Doğrulama sınırda, `Features/<X>/Rules` içinde; ihlal → `BusinessException` (400).
- Kimliğe bağlı alanlar (UserId vb.) asla istemci girdisinden alınmaz (BR-5); route `id`'si gövdeyi ezer.
- Hata yanıtı yalnızca ProblemDetails; iç detay sızmaz (`docs/conventions.md`, Error handling).
- Giriş hataları kullanıcı varlığını ele vermez (BR-8).
- Ham SQL yasak (RS0030). Şifreler ve refresh token'lar yalnızca hash/özet olarak saklanır.
- Rate limiting: Leaky Bucket, istemci IP'si başına, pipeline'ın en başında (429); muaf yollar yalnızca
  `RateLimitOptions:Whitelist`.

## AuthN / AuthZ
- **Varsayılan: reddet.** `AuthenticationBehavior`, `IAllowAnonymousRequest` olmayan her isteği geçerli
  token + Redis'te TokenSession ile doğrulamaya zorlar.
- Yetki `ISecuredRequest` + operation claim'ler ile `AuthorizationBehavior`'da; global `Admin` her
  zaman geçer (BR-11).
- Her yeni command/query ya `IAllowAnonymousRequest` ya `ISecuredRequest` olduğunu **açıkça** bildirir
  (yalnızca kimlik doğrulama isteniyorsa bu da spec'te yazılır).
- Kullanıcıya ait veride (sipariş vb.) sahiplik kontrolü zorunlu (IDOR).
- Bu alanın tamamı hassas alandır: **Kimlik ve yetki** (`docs/domain.md`).

## Dependencies
- Yeni NuGet/npm paketi planın dosya listesinde yer alır ve PLAN gate'inde insan onayı ister.
- Kontrol: lisans (ücretsiz/OSS uyumlu — örn. FluentAssertions v8 gibi ticari lisanslılar yok), aktif
  bakım, bilinen CVE (`dotnet list package --vulnerable`, `npm audit`).
- Sürümler sabit (NuGet tam sürüm, npm `package-lock.json` commit'li).

## Review lens
Security is a mandatory dimension of every independent review (see `prompts/review.md`), not a
separate afterthought phase.
