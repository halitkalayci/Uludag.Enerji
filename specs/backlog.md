# Backlog — deferred findings

Findings a human triaged as **defer**: real, but fixed later by decision (`workflows/feature-development.md`,
step 7). Nothing lands here without that decision; noise is rejected in the triage record, not parked here.

- A criterion moved out at the ship gate (R-08, `→ moved to B-<n>`) lands here too, by the
  human's ship decision; the entry names the spec and criterion.
- One entry per finding, newest last. Delete the entry in the commit of the work that closes it.
- An entry that grows into new behavior becomes a spec (`specs/active/`), not a fix.

<!-- Format (labels stay English):
- **B-<n>** — <finding, one line> — Source: spec <NNNN>, review round <n> (<file:line>) — Severity: <…>
  — Recommendation: <the reviewer's action> — Deferred by: <name>, <YYYY-MM-DD>, <why>
  A decision deferred at bootstrap (its REPORT) uses Source: bootstrap <YYYY-MM-DD>.
-->

- **B-1** — Git'te takip edilen `ECommerce/src/ECommerce.WebAPI/appsettings.Development.json` (TokenOptions:SecurityKey, RedisOptions:ConnectionString, AdminSeed:Password) ve `appsettings.json` (SQL `sa` şifresi) secret içeriyor — Source: bootstrap 2026-11-19 — Severity: High (öncelikli)
  — Recommendation: secret'ları `dotnet user-secrets` / ortam değişkenlerine taşı, `SecurityKey`'i yenile; hassas alan (Kimlik ve yetki) fix'i — Deferred by: Halit Enes Kalaycı, 2026-11-19, bootstrap yalnızca doküman ve konfigürasyon yazar
- **B-2** — `global.json` yok; build makinedeki en yeni SDK ile (bugün 10.0.401) yapılıyor — Source: bootstrap 2026-11-19 — Severity: Low
  — Recommendation: SDK'yı `global.json` ile sabitle (rollForward: latestFeature) — Deferred by: Halit Enes Kalaycı, 2026-11-19, mevcut build ayarları bootstrap'ta değiştirilmez
- **B-3** — Hedef `net9.0` (STS, destek sonu Kasım 2026) — Source: bootstrap 2026-11-19 — Severity: Medium
  — Recommendation: .NET 10 LTS'e yükseltme (ayrı spec; EF Core 10, paket sürümleri) — Deferred by: Halit Enes Kalaycı, 2026-11-19, ayrı iş
- **B-4** — `prettier --check` 53 dosyada fark buluyor (muhtemelen CRLF/LF; yeni `.gitattributes`) — Source: bootstrap 2026-11-19 — Severity: Low
  — Recommendation: tek seferlik `prettier --write` + satır sonu normalizasyonu (trivial değil: çok dosya), ardından `check.conf`'a `format [fast]` adımı — Deferred by: Halit Enes Kalaycı, 2026-11-19, check bugün yeşil kalsın
- **B-5** — `CreateOrderCommand`, `GetByIdOrderQuery`, `GetAllProductQuery` ne `ISecuredRequest` ne `IAllowAnonymousRequest` bildiriyor (yalnızca kimlik doğrulama); `GetByIdOrderQuery`'de sahiplik (IDOR) kontrolü doğrulanmadı — Source: bootstrap 2026-11-19 — Severity: Medium
  — Recommendation: incele (investigate); gerekirse hassas alan (Kimlik ve yetki) fix'i ile yetki/sahiplik kontrolü ekle — Deferred by: Halit Enes Kalaycı, 2026-11-20, bootstrap yalnızca doküman ve konfigürasyon yazar
- **B-6** — `StartupTests.Startup_FreshContainers_HasNoPendingMigrations` model ↔ migration kaymasını (unutulmuş migration) yakalamıyor; `Assert.Empty(pendingMigrations)` fixture `MigrateAsync` çağırdığı için kendiliğinden doğru — Source: spec 0001, VERIFY round 1 (`tests/ECommerce.IntegrationTests/StartupTests.cs`) — Severity: Low
  — Recommendation: `Assert.False(dbContext.Database.HasPendingModelChanges())` ekle — Deferred by: Halit Enes Kalaycı, 2026-11-20, spec 0001'in AC-9 metni harfiyen sağlanıyor; ek assert kapsamı genişletir
- **B-7** — `MultiFactorLockoutTests::Verify_Lockout_LastsFifteenMinutes` zamanın geçişini yalnızca Redis TTL'ini kısaltarak taklit eder; A-8 (BR-9 kilidi ilk hatalı denemeden başlıyor) TTL dışı bir pencereyle (değer içinde zaman damgası) düzeltilirse test yanlış yeşil/kırmızı verebilir — Source: spec 0002, review round 2 (`tests/ECommerce.IntegrationTests/Auth/MultiFactorLockoutTests.cs:75-99`) — Severity: Info
  — Recommendation: A-8 `/fix-bug` işine kısıt: kilit süresi Redis TTL ile ifade edilmeye devam etsin ya da uygulamaya `TimeProvider` eklenip test zamanı oradan yönetilsin — Deferred by: Halit Enes Kalaycı, 2026-11-20, bugünkü kodda risk yok; A-8 düzeltmesinin tasarımına ait
- **B-8** — `RegisterTests::Register_NewEmail_CreatesUserWithoutPlainPassword` şifrenin yalnızca bayt bayt düz saklanmasını yakalar; geri çevrilebilir kodlanmış (base64/hex) saklama testten geçer — Source: spec 0002, VERIFY round 1 (`tests/ECommerce.IntegrationTests/Auth/RegisterTests.cs`) — Severity: Info
  — Recommendation: şifrenin yaygın geri çevrilebilir kodlamalarının (base64, hex) da hash/salt'ta bulunmadığını assert et; şifre hash algoritmasının gücü (HMACSHA512 tek tur → PBKDF2/Argon2, analiz A-6) ile aynı işte ele al — Deferred by: Halit Enes Kalaycı, 2026-11-20, spec metni harfiyen karşılanıyor
