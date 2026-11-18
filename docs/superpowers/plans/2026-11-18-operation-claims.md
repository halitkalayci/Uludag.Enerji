# OperationClaim (Yetkilendirme) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Her Command/Query kabul ettiği operation claim'leri açıkça bildirir; kullanıcının token'daki claim'leriyle birebir metin eşleşmesi yoksa istek 403 alır.

**Architecture:** Mekanizma Uludag.Core'da (`ISecuredRequest`, `AuthorizationBehavior`, `AuthorizationException`, token'a claim yazma/okuma). Veri ECommerce'de (`OperationClaim`, `UserOperationClaim`, seed, repository). Claim'ler login'de JWT'ye `ClaimTypes.Role` olarak yazılır, behavior aynı token'dan okur.

**Tech Stack:** .NET 9, EF Core 9 (SQL Server, docker `uludag-mssql`), Microsoft.IdentityModel.JsonWebTokens, kendi Mediator'ımız.

**Spec:** `docs/superpowers/specs/2026-11-18-operation-claims-design.md`

## Global Constraints

- Handler'larda if-else yok; kontroller `Rules`/servislerde. Handler'da mapping yok; `Features/<X>/Mappers` kullanılır.
- Uludag.Core, ECommerce tiplerine referans vermez.
- Claim karşılaştırması `StringComparer.Ordinal` (büyük/küçük harf duyarlı).
- Claim adları: `<Entity>.Admin|Write|Read|Create|Update|Delete` (Entity: `Product`, `Order`, `OrderItem`), global `Admin`.
- Roller: Create/Update/Delete → `[Admin, Write, <İşlem>]`; GetAll/GetById → `[Admin, Read]` (hepsi entity önekli).
- Pipeline sırası (ilk eklenen en dışta): Authentication → Authorization → Logging.
- Yorumlar mevcut stil gibi Türkçe ve açıklayıcı.
- Repoda test projesi yok, bu iş eklemiyor. Doğrulama: `dotnet build` + migration + Task 4'teki HTTP smoke script'i.
- `AccessTokenExpiration` 1 dakika: smoke script login'den hemen sonra istek atmalı.

## Review Focus

1. Hiç claim'i olmayan yeni kullanıcı korumalı endpoint'e gelir → 403 (500 değil). Task 4 smoke: `user-no-claims`.
2. Tek claim'li kullanıcı (JWT tek değeri dizi değil string olarak yazar) → claim yine okunmalı. Task 4 smoke: `Product.Read` atanmış kullanıcı GET 200, POST 403.
3. Token'sız istek korumalı endpoint'e → 401 (403 değil; Authentication önce çalışır). Task 4 smoke: `no-token`.
4. Uygulama ikinci kez açıldığında admin seeder kullanıcıyı/claim'i tekrar eklemeye çalışıp patlamamalı. Task 4: iki kez başlatma adımı.
5. `AdminSeed` ayarı yokken (prod) seeder sessizce hiçbir şey yapmamalı. Task 4: `AdminSeedOptions` null/boş → return (kod incelemesi + Step 1 açıklaması).

---

### Task 1: Veri modeli — entity'ler, claim sabitleri, konfigürasyon, seed, repository, migration

**Files:**
- Create: `Uludag.Core/Security/Constants/GeneralOperationClaims.cs`
- Create: `ECommerce/src/ECommerce.Application/Features/Product/Constants/ProductOperationClaims.cs`
- Create: `ECommerce/src/ECommerce.Application/Features/Order/Constants/OrderOperationClaims.cs`
- Create: `ECommerce/src/ECommerce.Application/Features/OrderItem/Constants/OrderItemOperationClaims.cs`
- Create: `ECommerce/src/ECommerce.Domain/Entities/OperationClaim.cs`
- Create: `ECommerce/src/ECommerce.Domain/Entities/UserOperationClaim.cs`
- Modify: `ECommerce/src/ECommerce.Domain/Entities/User.cs`
- Create: `ECommerce/src/ECommerce.Persistence/Seeds/OperationClaimSeeds.cs`
- Create: `ECommerce/src/ECommerce.Persistence/Configurations/OperationClaimConfiguration.cs`
- Create: `ECommerce/src/ECommerce.Persistence/Configurations/UserOperationClaimConfiguration.cs`
- Modify: `ECommerce/src/ECommerce.Persistence/Contexts/UludagDbContext.cs`
- Create: `ECommerce/src/ECommerce.Application/Services/Repositories/IUserOperationClaimRepository.cs`
- Create: `ECommerce/src/ECommerce.Persistence/Repositories/UserOperationClaimRepository.cs`
- Modify: `ECommerce/src/ECommerce.Persistence/PersistenceServiceRegisteration.cs`
- Create: `ECommerce/src/ECommerce.Persistence/Migrations/<ts>_add_operation_claims.cs` (EF üretir)

**Interfaces:**
- Produces:
  - `Uludag.Core.Security.Constants.GeneralOperationClaims.Admin = "Admin"` (`public const string`)
  - `ProductOperationClaims` / `OrderOperationClaims` / `OrderItemOperationClaims`: `public const string Admin, Write, Read, Create, Update, Delete` → ör. `"Product.Admin"`, `"OrderItem.Delete"`. Namespace: `ECommerce.Application.Features.<X>.Constants`.
  - `OperationClaim { Guid Id; string Name; ICollection<UserOperationClaim> UserOperationClaims }`
  - `UserOperationClaim { Guid Id; Guid UserId; Guid OperationClaimId; User User; OperationClaim OperationClaim }`
  - `User.UserOperationClaims : ICollection<UserOperationClaim>` (`= new List<...>()`)
  - `OperationClaimSeeds.AdminId : static readonly Guid`, `OperationClaimSeeds.All : OperationClaim[]` (19 kayıt: `Admin` + 3 entity × 6)
  - `IUserOperationClaimRepository.GetOperationClaimNamesByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) : Task<List<string>>`
  - `DbSet<OperationClaim> OperationClaims`, `DbSet<UserOperationClaim> UserOperationClaims`

- [ ] **Step 1: Sabitleri ve entity'leri ekle** (yukarıdaki imzalar; `OperationClaimConfiguration`/seed Application sabitlerini kullanır, metin tekrar yazılmaz).

- [ ] **Step 2: Konfigürasyonları yaz**
  - `OperationClaim`: `Name` zorunlu, `HasMaxLength(100)`, `HasIndex(Name).IsUnique()`, `HasData(OperationClaimSeeds.All)`.
  - `UserOperationClaim`: `HasOne(User).WithMany(UserOperationClaims).HasForeignKey(UserId)`, `HasOne(OperationClaim).WithMany(UserOperationClaims).HasForeignKey(OperationClaimId)`, `HasIndex(UserId, OperationClaimId).IsUnique()`.
  - Seed Guid'leri bir kez üretilip koda sabit yazılır (migration her seferinde değişmesin).

- [ ] **Step 3: Repository** — `UserOperationClaims.Where(UserId == userId).Select(x => x.OperationClaim.Name).ToListAsync()`; DI'a Scoped ekle.

- [ ] **Step 4: Build**

Run: `dotnet build Uludag.Enerji.slnx` (repo kökünden)
Expected: `Build succeeded`, 0 error.

- [ ] **Step 5: Migration üret ve uygula**

Run (repo kökünden):
```
dotnet ef migrations add add_operation_claims -p ECommerce/src/ECommerce.Persistence -s ECommerce/src/ECommerce.WebAPI
dotnet ef database update -p ECommerce/src/ECommerce.Persistence -s ECommerce/src/ECommerce.WebAPI
```
Expected: migration dosyasında `OperationClaims` + `UserOperationClaims` tabloları ve 19 `InsertData` satırı; `database update` "Done." ile biter. Kontrol:
`docker exec uludag-mssql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "Admin123!" -d uludag -Q "SELECT COUNT(*) FROM OperationClaims"` → `19`.

- [ ] **Step 6: Commit**

```
git add Uludag.Core ECommerce docs
git commit -m "feat: add OperationClaim and UserOperationClaim entities with seed"
```

---

### Task 2: Yetkilendirme mekanizması — Core behavior, token claim'leri, login, 403

**Files:**
- Create: `Uludag.Core/Mediation/ISecuredRequest.cs`
- Create: `Uludag.Core/Security/Authorization/AuthorizationException.cs`
- Create: `Uludag.Core/Security/JWT/HttpContextBearerTokenExtensions.cs`
- Create: `Uludag.Core/Mediation/Behaviors/AuthorizationBehavior.cs`
- Modify: `Uludag.Core/Mediation/Behaviors/AuthenticationBehavior.cs` (bearer okuma yeni extension'a taşınır)
- Modify: `Uludag.Core/Security/JWT/ITokenHelper.cs`, `Uludag.Core/Security/JWT/JwtHelper.cs`
- Modify: `ECommerce/src/ECommerce.Application/Features/Auth/Commands/Login/LoginCommand.cs`
- Modify: `ECommerce/src/ECommerce.Application/ApplicationServiceRegisteration.cs`
- Modify: `ECommerce/src/ECommerce.WebAPI/Middlewares/BusinessExceptionMiddleware.cs`

**Interfaces:**
- Consumes: `GeneralOperationClaims.Admin`, `IUserOperationClaimRepository.GetOperationClaimNamesByUserIdAsync` (Task 1)
- Produces:
  - `public interface ISecuredRequest { string[] Roles { get; } }` (namespace `Uludag.Core.Mediation`)
  - `public class AuthorizationException : Exception` (`Uludag.Core.Security.Authorization`), ctor `(string message)`
  - `internal static string? GetBearerToken(this HttpContext? httpContext)` — mevcut `AuthenticationBehavior.GetBearerToken` mantığı aynen
  - `ITokenHelper.CreateToken(Guid userId, string email, IEnumerable<string> operationClaims) : AccessToken`
  - `ITokenHelper.GetOperationClaimsAsync(string token) : Task<IReadOnlyCollection<string>>` — token'ı doğrular; geçersizse boş koleksiyon; geçerliyse `ClaimTypes.Role` tipli claim değerleri
  - `AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>` — ctor `(IHttpContextAccessor, ITokenHelper)`

- [ ] **Step 1: Core tiplerini ekle** (`ISecuredRequest`, `AuthorizationException`, bearer extension; `AuthenticationBehavior` yeni extension'ı kullanır, davranışı değişmez).

- [ ] **Step 2: `JwtHelper`** — `CreateToken` her claim adı için `new Claim(ClaimTypes.Role, name)` ekler. `GetOperationClaimsAsync`: `tokenHandler.ValidateTokenAsync(token, validationParameters)`; `IsValid` değilse `[]`, değilse `result.ClaimsIdentity.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()`.

- [ ] **Step 3: `AuthorizationBehavior.Handle`**

```csharp
if (request is not ISecuredRequest securedRequest)
    return await next();

string? token = httpContextAccessor.HttpContext.GetBearerToken();
IReadOnlyCollection<string> userClaims = token is null ? [] : await tokenHelper.GetOperationClaimsAsync(token);

bool isAuthorized = userClaims.Contains(GeneralOperationClaims.Admin, StringComparer.Ordinal)
    || userClaims.Intersect(securedRequest.Roles, StringComparer.Ordinal).Any();
if (!isAuthorized)
    throw new AuthorizationException("Bu işlem için yetkiniz bulunmamaktadır.");

return await next();
```
Sınıf başına mevcut behavior'lardaki gibi Türkçe açıklama yorumu.

- [ ] **Step 4: Kayıt ve login**
  - `ApplicationServiceRegisteration`: `AuthorizationBehavior` satırı Authentication'dan sonra, Logging'den önce.
  - `LoginCommandHandler`: `IUserOperationClaimRepository` enjekte; `List<string> operationClaims = await userOperationClaimRepository.GetOperationClaimNamesByUserIdAsync(user.Id, cancellationToken);` → `tokenHelper.CreateToken(user.Id, user.Email, operationClaims)`. If yok.

- [ ] **Step 5: Middleware** — `catch (AuthorizationException)` → `403`, title `"Forbidden"`. Üstteki açıklama yorumuna `AuthorizationException -> 403` satırı.

- [ ] **Step 6: Build**

Run: `dotnet build Uludag.Enerji.slnx`
Expected: `Build succeeded`, 0 error.

- [ ] **Step 7: Commit**

```
git add Uludag.Core ECommerce
git commit -m "feat: add AuthorizationBehavior and operation claims in JWT"
```

---

### Task 3: Product / Order / OrderItem isteklerini koru

**Files (15 istek, hepsi Modify):**
- `ECommerce/src/ECommerce.Application/Features/{Product,Order,OrderItem}/Commands/{Create,Update,Delete}/<İşlem><Entity>Command.cs`
- `ECommerce/src/ECommerce.Application/Features/{Product,Order,OrderItem}/Queries/{GetAll,GetById}/<İşlem><Entity>Query.cs`

**Interfaces:**
- Consumes: `ISecuredRequest` (Task 2), `<Entity>OperationClaims` (Task 1)

- [ ] **Step 1: Her isteğe `ISecuredRequest` ekle — açık arayüz implementasyonu ile** (public property olmadığı için JSON body'ye/Swagger şemasına girmez):

```csharp
public class CreateProductCommand : IRequest, ISecuredRequest
{
    // Bu işlemi yapabilecek claim'ler: biri yeterli. Global "Admin" behavior'da ayrıca geçer.
    string[] ISecuredRequest.Roles => [ProductOperationClaims.Admin, ProductOperationClaims.Write, ProductOperationClaims.Create];
```
Update/Delete için son eleman `Update`/`Delete`; GetAll/GetById için `[<Entity>.Admin, <Entity>.Read]`.

- [ ] **Step 2: Kontrol** — `grep -L "ISecuredRequest" ECommerce/src/ECommerce.Application/Features/{Product,Order,OrderItem}/{Commands,Queries}/*/*.cs` → çıktı boş (`*Response.cs` dosyaları klasör seviyesinde olduğu için eşleşmez).

- [ ] **Step 3: Build** — `dotnet build Uludag.Enerji.slnx` → `Build succeeded`.

- [ ] **Step 4: Commit**

```
git add ECommerce
git commit -m "feat: require operation claims on product, order and order item requests"
```

---

### Task 4: Başlangıç admin'i ve uçtan uca doğrulama

**Files:**
- Create: `ECommerce/src/ECommerce.Persistence/Seeds/AdminSeedOptions.cs`
- Create: `ECommerce/src/ECommerce.Persistence/Seeds/AdminUserSeeder.cs`
- Modify: `ECommerce/src/ECommerce.WebAPI/Program.cs`
- Modify: `ECommerce/src/ECommerce.WebAPI/appsettings.Development.json`
- Smoke script (repoya girmez): `<scratchpad>/smoke-operation-claims.ps1`

**Interfaces:**
- Consumes: `OperationClaimSeeds.AdminId` (Task 1), `HashingHelper.CreatePasswordHash` (Core)
- Produces:
  - `public class AdminSeedOptions { string Email; string Password; }`
  - `public static Task SeedAdminUserAsync(this IServiceProvider serviceProvider, AdminSeedOptions? options)` (`AdminUserSeeder`, static class)

- [ ] **Step 1: Seeder** — options null veya Email/Password boşsa hiçbir şey yapmadan döner. Değilse scope açıp `UludagDbContext` alır; e-postayla kullanıcı yoksa hash+salt ile oluşturur; kullanıcının `AdminId` ataması yoksa `UserOperationClaim` ekler; tek `SaveChangesAsync`. Böylece tekrar çalıştırmak güvenli.

- [ ] **Step 2: Program.cs** — `builder.Build()` sonrası, middleware'lerden önce:
```csharp
AdminSeedOptions? adminSeedOptions = builder.Configuration.GetSection("AdminSeed").Get<AdminSeedOptions>();
await app.Services.SeedAdminUserAsync(adminSeedOptions);
```
`appsettings.Development.json`'a: `"AdminSeed": { "Email": "admin@uludag.local", "Password": "Admin123!" }` (yalnızca geliştirme; appsettings.json'a eklenmez).

- [ ] **Step 3: Build** — `dotnet build Uludag.Enerji.slnx` → `Build succeeded`.

- [ ] **Step 4: Smoke script'i yaz** — `Invoke-WebRequest -SkipHttpErrorCheck` ile `http://localhost:5057`, her senaryo beklenen status ile karşılaştırılır, sonunda `PASS`/`FAIL` sayısı:

| Senaryo | İstek | Beklenen |
|---|---|---|
| no-token | `GET /api/products` | 401 |
| user-no-claims | register+login rastgele kullanıcı → `GET /api/products` | 403 |
| user-no-claims | aynı token → `POST /api/products` | 403 |
| admin | `admin@uludag.local` login → `POST /api/products` (benzersiz isim, `Amount 10, Currency "TRY", Stock 5`) | 200 |
| admin | `GET /api/products` | 200 |
| single-claim | kullanıcıya sqlcmd ile `Product.Read` ata (`INSERT INTO UserOperationClaims (Id, UserId, OperationClaimId) SELECT NEWID(), u.Id, c.Id FROM Users u, OperationClaims c WHERE u.Email='<email>' AND c.Name='Product.Read'`), tekrar login → `GET /api/products` | 200 |
| single-claim | aynı token → `POST /api/products` | 403 |
| single-claim | aynı token → `GET /api/orders` | 403 |

- [ ] **Step 5: Uygulamayı başlat ve smoke'u çalıştır**

Run: `dotnet run --project ECommerce/src/ECommerce.WebAPI --launch-profile http` (arka planda), ardından `pwsh <scratchpad>/smoke-operation-claims.ps1`
Expected: tüm satırlar PASS, `FAIL: 0`.

- [ ] **Step 6: Tekrar başlatma (seeder idempotent)** — uygulamayı durdur, yeniden başlat; açılışta hata yok; `SELECT COUNT(*) FROM UserOperationClaims uoc JOIN Users u ON u.Id=uoc.UserId WHERE u.Email='admin@uludag.local'` → `1`. Smoke'u tekrar çalıştır → `FAIL: 0`.

- [ ] **Step 7: Commit**

```
git add ECommerce
git commit -m "feat: seed development admin user with Admin claim"
```
