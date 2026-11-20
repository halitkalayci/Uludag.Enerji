# Conventions

> Bootstrap 2026-11-19'da mevcut koddan çıkarıldı ve onaylandı. Buradaki her kural ya tooling ile
> zorlanır ya da review'da kontrol edilir.

## Language
- Chat language (interviews, sessions): Türkçe (`tr`)
- Document language (docs/, specs, plans, ADRs, review and verify reports): Türkçe (`tr`)
- Always English (protocol, not prose): ANEW core files, template headings and field labels,
  `Status:` values (Draft / Approved / In progress / Shipped), `Approved by / on:`, `Source:`.
- Code identifiers, branch names, commit messages: İngilizce. Kod yorumları Türkçe; kullanıcıya dönen
  mesajlar `Locales/<Feature>Locale.{tr,en}.yaml` üzerinden.
- Agent messages (handoff summaries, refusals, `Next:` lines): chat language; command names, file
  paths and protocol fields verbatim.

## Language & framework versions
- .NET: `net9.0` (ASP.NET Core 9), C# nullable + implicit usings açık. `global.json` yok (B-2).
- EF Core 9.0.x — aktif provider SQL Server 2022; Npgsql/Postgres 17 yedek; InMemory referanslı.
- Redis 8 (cache, TokenSession, MFA oturumları, rate limit kovaları); yoksa in-memory.
- Angular 22.2, TypeScript 6.0, Node 24, npm 11.19 (`packageManager`), prettier 3.
- Yerel altyapı: `docker-compose.yml` (postgres, mssql, redis, pgadmin, redisinsight; yalnızca 127.0.0.1).

## Naming
- Command: `Features/<Feature>/Commands/<Action>/<Action><Feature>Command.cs`; handler aynı dosyada
  iç sınıf `<Action><Feature>CommandHandler`. Query: `Queries/{GetAll,GetById}/<...>Query.cs`.
  Yanıt: `<Entity>Response`.
- Kurallar: `Features/<Feature>/Rules/<Feature>BusinessRules : BaseBusinessRules` (otomatik Scoped DI);
  metot adları cümle gibi: `OrderShouldHaveAtLeastOneItem`, `ProductNameCannotExist`.
- Mapper: `Features/<Feature>/Mappers/<Entity>Mapper` (static). Sabitler: `<Feature>Constants`
  (mesaj anahtarları), `<Feature>OperationClaims`.
- Repository: arayüz `Application/Services/Repositories/I<Entity>Repository`, implementasyon
  `Persistence/Repositories/<Entity>Repository`. Servis: `I<X>Service` / `<X>Manager`.
- Field'lar camelCase, alt çizgisiz; atama `this.x = x`.
- Angular: `src/app/{core,features,shared}`; prettier `printWidth: 100`, `singleQuote`.

## Error handling
- Rules sınıfları ihlalde exception fırlatır, aksi halde sessizce döner; handler hata için `null`/`false` dönmez.
  `NotFoundException` → 404 · `BusinessException` → 400 · `AuthenticationException` → 401 ·
  `AuthorizationException` → 403 (`BusinessExceptionMiddleware`, `application/problem+json`).
- Beklenmeyen exception → 500, genel mesaj ("Beklenmedik bir hata oluştu."). Stack trace, SQL, iç tip
  adı veya secret yanıta asla sızmaz.
- `NotFoundException`, `BusinessException`'dan türer: catch sırası önce NotFound.

## Data rules
- Para her zaman `Money` (`decimal`, DB'de `decimal(18,2)`, `Currency` varsayılan `TRY`); `double`/`float` yasak.
- Zaman damgaları UTC (`DateTime.UtcNow`); **yeni kodda** test edilebilirlik için `TimeProvider` tercih edilir.
- ID'ler `Guid`.
- Kimliğe bağlı alanlar (UserId, tarih, fiyat, toplam) sunucuda atanır; istemci gövdesinden alınmaz.
  Route'taki `id` gövdedeki `Id`'yi ezer.
- Ham SQL yasak: LINQ veya `FromSql($"...")` / `ExecuteSql($"...")` (interpolated, parametreli).

## Known traps
<!-- Starts empty. "<trap> — <what to do instead> — from <spec/finding>". Added at triage (Amendment A-n). -->
- Benzersizlik kuralı yalnızca Rules'ta kontrol-sonra-ekle ile yazılırsa eşzamanlı istekte delinir; `nvarchar(max)` kolona index konamaz — kolona uzunluk + DB unique index ekle, repository'de 2601/2627 ihlalini aynı kuralın `BusinessException`'ına çevir — from spec 0002 / A-2 (review Info-4)

## Enforced by tooling
- RS0030 (BannedApiAnalyzers, `Directory.Build.props` + `BannedSymbols.txt`): `FromSqlRaw`,
  `ExecuteSqlRaw(Async)`, `SqlQueryRaw` build **hatası**.
- `Nullable` enable (uyarı düzeyinde; mevcut 10 uyarı var, hataya çevrilmedi).
- `scripts/check`: .NET build + Angular build + testler (fast: unit + mimari; full: + entegrasyon, Docker gerekir).
  Prettier kontrolü B-4 sonrası eklenecek.
