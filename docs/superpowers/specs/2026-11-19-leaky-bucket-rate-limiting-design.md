# Leaky Bucket Rate Limiting — Tasarım

## Amaç
Tüm sisteme (her HTTP isteğine) Leaky Bucket algoritmasıyla istek sınırı getirmek.
Bir istemci kısa sürede çok istek atarsa reddedilsin; sistem kötüye kullanıma / aşırı yüke karşı korunsun.

## Kararlaştırılanlar (kullanıcı)
- **Kapasite:** 10. **Sızma:** her 12 saniyede 1 damla (kalıcı hız ≈ dakikada 5 istek).
- **Kova anahtarı:** IP başına — her istemci IP'sinin kendi kovası var.
- **Kova doluyken:** istek beklemeden reddedilir → `429 Too Many Requests` (kuyruk / bekletme yok, "meter" yorumu).
- **Depolama:** mevcut cache seçimine uyar — `RedisOptions:ConnectionString` doluysa Redis, değilse in-memory.
- **Muafiyet:** yok. Swagger ve `/openapi` dahil her istek kovaya sayılır.
- **Yaklaşım A** (aşağıda).

## Yaklaşımlar
- **A) Kendi leaky bucket bileşenimiz + HTTP middleware (seçildi):** Algoritma Uludag.Core'da, Redis ve in-memory
  implementasyonlarıyla; WebAPI'de pipeline'ın en başında bir middleware. Gerçek leaky bucket, Redis'te dağıtık çalışır,
  tüm HTTP trafiğini kapsar. Bedeli: biraz daha fazla kod.
- B) ASP.NET Core `AddRateLimiter`: hazır leaky bucket yok (en yakını Token Bucket) ve yalnızca in-memory;
  Redis için yine kendi `PartitionedRateLimiter`'ımızı yazmak gerekirdi → A'nın işi + ek soyutlama.
- C) Mediator `IPipelineBehavior`: yalnızca `mediator.Send` çağrılarını kapsar, Swagger/`/openapi` dışarıda kalır →
  "muafiyet yok" kararıyla çelişir.

## Tasarım

### Algoritma (meter)
Her kova iki değer tutar: `level` (kovadaki damla sayısı, tam sayı) ve `lastLeak` (son sızma zamanı, ms).
Her istekte, atomik olarak:
1. `drips = floor((now - lastLeak) / interval)`; `level = max(0, level - drips)`; `lastLeak += drips * interval`
   (kova boşaldıysa `lastLeak = now`).
2. `level + 1 > capacity` → **reddet**; `retryAfter = lastLeak + interval - now` (bir sonraki damlaya kalan süre).
3. Değilse `level += 1` → **kabul**.
4. Anahtarın TTL'i `capacity * interval` (kova o sürede kendiliğinden boşalır; boş kova = anahtar yok).

### Uludag.Core — `RateLimiting/`
- `LeakyBucketOptions { int Capacity; int LeakIntervalSeconds }` (appsettings `"RateLimitOptions"`).
- `RateLimitResult { bool IsAllowed; TimeSpan RetryAfter }`.
- `ILeakyBucketRateLimiter { Task<RateLimitResult> TryAcquireAsync(string key) }`.
- `Redis/RedisLeakyBucketRateLimiter`: algoritma tek Lua script'inde (HASH: `level`, `lastLeak`).
  Zaman Redis'in `TIME` komutundan alınır → birden fazla uygulama instance'ında saat farkı sorun olmaz.
  Anahtarlar `RedisOptions.InstanceName` öneki ile (`uludag:rate-limit:<ip>`).
- `InMemory/InMemoryLeakyBucketRateLimiter`: anahtar başına kova + kilit (`ConcurrentDictionary`), aynı algoritma.
  Boş kovalar (TTL dolmuş) temizlenir, sözlük sınırsız büyümez.
- `AddLeakyBucketRateLimiter(options, redisOptions)` uzantısı: `AddCacheService` ile aynı seçim mantığı;
  Redis'te mevcut `IConnectionMultiplexer` singleton'ı paylaşılır.

### ECommerce.WebAPI
- `Middlewares/LeakyBucketRateLimitingMiddleware`: anahtar = `HttpContext.Connection.RemoteIpAddress`.
  Reddedilirse `429` + `Retry-After` (saniye, yukarı yuvarlanmış) + ProblemDetails (`application/problem+json`,
  `BusinessExceptionMiddleware` ile aynı biçim). Kabul edilirse `next()`.
- `Program.cs`: kayıt + middleware **pipeline'ın en başında** (`BusinessExceptionMiddleware`'den önce) → reddedilen
  istek hiçbir iş yaptırmaz.
- `appsettings.json`: `"RateLimitOptions": { "Capacity": 10, "LeakIntervalSeconds": 12 }`.

## Benim kararlarım (düzeltilebilir)
- **IP kaynağı:** `RemoteIpAddress`. Önünde proxy yok; `X-Forwarded-For`'a güvenilmez (istemci sahteleyip limiti atlatabilir).
  İleride reverse proxy eklenirse `UseForwardedHeaders` + bilinen proxy listesi gerekir.
  IP alınamazsa (ör. test sunucusu) tek bir `"unknown"` kovası kullanılır.
- **Başlıklar:** yalnızca 429'da `Retry-After`. Başarılı cevaplara `X-RateLimit-*` eklenmez.
- **Redis erişilemezse:** istek reddedilmez, geçirilir (fail-open) ve uyarı loglanır — cache altyapısının çökmesi
  tüm API'yi kilitlemesin diye.
- **Doğrulama:** repoda test projesi yok; derleme + uygulamayı çalıştırıp aynı IP'den 11. istekte 429 geldiğini,
  12 sn sonra tekrar 1 isteğin geçtiğini elle (curl) doğrulama. Redis ve in-memory modlarının ikisi de denenir.

## Güncelleme: Whitelist (kullanıcı kararı)
"Muafiyet yok" kararı değişti: `RateLimitOptions:Whitelist` içindeki yollar rate limiting dışında tutulur
(kovaya sayılmaz, hiç 429 almaz).
- Başlangıç listesi: `["/swagger", "/openapi"]`.
- Eşleşme: önek (prefix) + segment sınırı, harf duyarsız (`PathString.StartsWithSegments`, `OrdinalIgnoreCase`).
  `"/swagger"` → `/swagger/index.html` muaf, `/swaggerX` değil. Sondaki `/` yok sayılır.
- `"/"` ile başlamayan değer → açılışta `ArgumentException`.
- `Request.Path` Kestrel tarafından `..` segmentlerinden arındırıldığı için `/swagger/../api/...` whitelist'e takılmaz.

## Hedef dışı
- Endpoint / kullanıcı bazlı farklı limitler, kuyruklama (bekletme), muafiyet listesi.
- Reverse proxy / forwarded headers desteği.
- Test projesi kurmak.
