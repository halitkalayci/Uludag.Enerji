# Leaky Bucket Rate Limiting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Her HTTP isteğini IP başına bir leaky bucket'tan (kapasite 10, 12 sn'de 1 damla) geçirmek; kova doluysa 429.

**Architecture:** Algoritma `Uludag.Core/RateLimiting` altında `ILeakyBucketRateLimiter` olarak; Redis (Lua, atomik) ve
in-memory implementasyonları, `AddCacheService` ile aynı seçim mantığıyla DI'a kaydedilir. WebAPI'de pipeline'ın en
başındaki bir middleware IP'yi anahtar yapıp limiter'a sorar.

**Tech Stack:** .NET 9, ASP.NET Core, StackExchange.Redis 2.13 (Lua `EVAL`), Redis 8.

**Spec:** `docs/superpowers/specs/2026-11-19-leaky-bucket-rate-limiting-design.md`

## Global Constraints
- Kapasite `10`, sızma aralığı `12` saniye — appsettings `"RateLimitOptions": { "Capacity": 10, "LeakIntervalSeconds": 12 }`.
- Kova dolu → `429`, `Retry-After` (saniye, yukarı yuvarlanmış, en az 1), ProblemDetails `application/problem+json`.
- Anahtar: `HttpContext.Connection.RemoteIpAddress`; null → `"unknown"`. `X-Forwarded-For` okunmaz.
- Muafiyet yok; middleware `BusinessExceptionMiddleware`'den önce.
- Redis hatası → istek geçer (fail-open) + `LogWarning`.
- Redis'te zaman `TIME` komutundan; anahtar `RedisOptions.InstanceName + "rate-limit:" + ip`.
- Repo kuralı: yorumlar Türkçe, mevcut dosyalardaki açıklama tarzında.

## Review Focus
1. Aynı anda gelen çok sayıda istek (eşzamanlılık) → kova asla 10'u aşmamalı (Redis: tek Lua script; bellek: kova kilidi).
2. Kova boşken gelen ilk damla → sızma saati `now`'dan başlamalı (eski `lastLeak` yüzünden erken sızma olmamalı).
3. Uzun süre sessiz kalan IP → kova tamamen boş sayılmalı, `level` negatife düşmemeli.
4. Redis kapalıyken → 500 değil, istek geçmeli.
5. Bellek modunda binlerce farklı IP → boş kovalar temizlenmeli; temizlenen kovaya o an yazan istek kaybolmamalı.

Test projesi yok (spec: hedef dışı) → her task derleme + Task 3'teki elle doğrulama ile kapanır.

---

### Task 1: Core — sözleşme, seçenekler, in-memory limiter, DI kaydı

**Files:**
- Create: `Uludag.Core/RateLimiting/LeakyBucketOptions.cs`, `RateLimitResult.cs`, `ILeakyBucketRateLimiter.cs`,
  `InMemory/InMemoryLeakyBucketRateLimiter.cs`, `RateLimitingServiceCollectionExtensions.cs`

**Interfaces — Produces:**
- `class LeakyBucketOptions { int Capacity = 10; int LeakIntervalSeconds = 12; }`
- `sealed record RateLimitResult(bool IsAllowed, TimeSpan RetryAfter)` + `static RateLimitResult Allowed`
- `interface ILeakyBucketRateLimiter { Task<RateLimitResult> TryAcquireAsync(string key); }`
- `static IServiceCollection AddLeakyBucketRateLimiter(this IServiceCollection, LeakyBucketOptions, RedisOptions?)`
  (namespace `Microsoft.Extensions.DependencyInjection`; seçenekler ≤ 0 → `ArgumentException`; Redis modunda
  `AddCacheService` önce çağrılmış olmalı — `IConnectionMultiplexer` oradan gelir.)

- [ ] **Step 1:** Seçenek, sonuç, arayüz dosyaları.
- [ ] **Step 2:** `InMemoryLeakyBucketRateLimiter(LeakyBucketOptions, TimeProvider)` — `ConcurrentDictionary<string, Bucket>`,
  kova başına `lock`; spec'teki algoritma (tam sayı damla, boş kovada `lastLeak = now`). Fırsatçı temizlik: en fazla
  `capacity*interval`'de bir, `lastLeak + level*interval <= now` olan kovalar silinir; silinen kova `Removed` işaretlenir,
  onu yakalamış istek yeni kovayla yeniden dener.
- [ ] **Step 3:** DI uzantısı (Redis dalı Task 2'de doldurulur; şimdilik yalnızca bellek dalı).
- [ ] **Step 4:** `dotnet build Uludag.Enerji.slnx` → 0 hata.

### Task 2: Core — Redis limiter

**Files:** Create `Uludag.Core/RateLimiting/Redis/RedisLeakyBucketRateLimiter.cs`; Modify `RateLimitingServiceCollectionExtensions.cs`.

**Interfaces — Consumes:** Task 1'in tipleri; `IConnectionMultiplexer`, `RedisOptions`, `ILogger<RedisLeakyBucketRateLimiter>`.

- [ ] **Step 1:** Lua script (HASH `level`/`lastLeak`, `TIME`, `HMGET`/`HSET`/`PEXPIRE capacity*interval`), dönüş `{allowed, retryAfterMs}`:

```lua
local t = redis.call('TIME')
local now = tonumber(t[1]) * 1000 + math.floor(tonumber(t[2]) / 1000)
local capacity = tonumber(ARGV[1]); local interval = tonumber(ARGV[2])
local data = redis.call('HMGET', KEYS[1], 'level', 'lastLeak')
local level = tonumber(data[1]) or 0; local lastLeak = tonumber(data[2]) or now
local drips = math.floor((now - lastLeak) / interval)
if drips > 0 then level = math.max(0, level - drips); lastLeak = lastLeak + drips * interval end
if level == 0 then lastLeak = now end
if level + 1 > capacity then return {0, lastLeak + interval - now} end
redis.call('HSET', KEYS[1], 'level', level + 1, 'lastLeak', lastLeak)
redis.call('PEXPIRE', KEYS[1], capacity * interval)
return {1, 0}
```
- [ ] **Step 2:** `RedisException` yakalanır → `LogWarning` + `RateLimitResult.Allowed`.
- [ ] **Step 3:** DI uzantısında `RedisOptions.ConnectionString` doluysa Redis implementasyonu.
- [ ] **Step 4:** `dotnet build` → 0 hata.

### Task 3: WebAPI — middleware, kayıt, ayar, elle doğrulama

**Files:** Create `ECommerce/src/ECommerce.WebAPI/Middlewares/LeakyBucketRateLimitingMiddleware.cs`;
Modify `Program.cs`, `appsettings.json`.

- [ ] **Step 1:** Middleware: anahtar IP; reddedilirse 429 + `Retry-After` + ProblemDetails (Title `"Too Many Requests"`).
- [ ] **Step 2:** `Program.cs`: `RateLimitOptions` bölümü okunur (yoksa açılışta hata), `AddLeakyBucketRateLimiter` `AddCacheService`'ten sonra;
  `app.UseMiddleware<LeakyBucketRateLimitingMiddleware>()` `BusinessExceptionMiddleware`'den önce.
- [ ] **Step 3:** `dotnet build` → 0 hata.
- [ ] **Step 4 (Redis modu):** uygulamayı çalıştır, aynı IP'den 11 istek → 10×(200/401), 11. `429` + `Retry-After`;
  ~12 sn bekle → 1 istek geçer, sonraki 429.
- [ ] **Step 5 (bellek modu):** `RedisOptions__ConnectionString=""` ile aynı senaryo.
- [ ] **Step 6 (fail-open):** Redis ayarlı ama erişilemez bir adresle → istekler geçer, log'da uyarı.
