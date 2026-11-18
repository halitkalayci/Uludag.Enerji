# OperationClaim (Yetkilendirme) — Tasarım

## Amaç
Kimlik doğrulama (AuthenticationBehavior) var, yetkilendirme yok: geçerli token'ı olan herkes her işlemi yapabiliyor.
Her Command/Query, kabul ettiği operation claim'leri açıkça belirtecek; kullanıcının claim'lerinden en az biri
listede **birebir metin eşleşmesi** ile bulunursa istek geçer, yoksa 403.

## Kararlaştırılanlar (kullanıcı)
- `OperationClaim` entity'si, `User` ile çoka-çok ilişki.
- Karşılaştırma metinsel (claim adı string).
- Varsayılan yapı:
  - `Create` → `Entity.Admin`, `Entity.Write`, `Entity.Create`
  - `Update` → `Entity.Admin`, `Entity.Write`, `Entity.Update`
  - `Delete` → `Entity.Admin`, `Entity.Write`, `Entity.Delete`
  - `GetAll` / `GetById` → `Entity.Admin`, `Entity.Read`
- **Yaklaşım A:** Her istek claim'lerini açıkça yazar (`ISecuredRequest.Roles`); isimden otomatik türetme yok.

## Yaklaşımlar
- **A) Açık bildirim (seçildi):** `ISecuredRequest { string[] Roles }`, değerler feature'ın `Constants/<Entity>OperationClaims` sınıfından.
  Okunabilir, aranabilir, mevcut Constants/Rules düzenine uyar; bedeli komut başına bir satır.
- B) İsimden türetme: `CreateProductCommand` → `Product` + `Create`. Tekrar yok ama örtük ve isimlendirmeye kırılgan (`GetByIdProductQuery`, `OrderItem`).

## Tasarım

### Uludag.Core (mekanizma — ECommerce entity'lerini bilmez)
- `Mediation/ISecuredRequest`: `string[] Roles { get; }`.
- `Mediation/Behaviors/AuthorizationBehavior`: `ISecuredRequest` değilse geçer. Değilse token'daki claim'leri okur;
  kullanıcıda `Admin` (global) claim'i **veya** `Roles` ile kesişen bir claim yoksa `AuthorizationException` fırlatır.
  Karşılaştırma `StringComparer.Ordinal`. AuthenticationBehavior'dan **sonra** kaydedilir.
- `Security/AuthorizationException` (Core'da; Application'ın exception'larına bağımlı olmamak için).
- `ITokenHelper.CreateToken(Guid userId, string email, IEnumerable<string> operationClaims)`: claim'ler token'a
  `ClaimTypes.Role` tipiyle eklenir.
- `ITokenHelper`'a token'dan claim'leri okuyan bir metot (behavior HttpContext.User / JwtBearer eşlemesine değil, Core'un kendi doğrulamasına dayanır — AuthenticationBehavior ile aynı yaklaşım).
- `Security/Constants/GeneralOperationClaims.Admin = "Admin"`.

### ECommerce.Domain
- `OperationClaim { Guid Id; string Name; ICollection<UserOperationClaim> }`
- `UserOperationClaim { Guid Id; Guid UserId; Guid OperationClaimId; User; OperationClaim }` — açık ara tablo
  (ileride atama/kaldırma işlemleri bu entity üzerinden yapılabilsin diye).
- `User.UserOperationClaims` navigation.

### ECommerce.Persistence
- `OperationClaimConfiguration` (Name max 100, unique index), `UserOperationClaimConfiguration` ((UserId, OperationClaimId) unique).
- Seed (HasData, sabit Guid'ler): `Admin` + Product/Order/OrderItem için `Admin, Write, Read, Create, Update, Delete`.
- `IUserOperationClaimRepository.GetOperationClaimNamesByUserIdAsync(userId)` (+ implementasyon, DI kaydı).
- Migration: `add_operation_claims`.

### ECommerce.Application
- `Features/<Entity>/Constants/<Entity>OperationClaims.cs` (Product, Order, OrderItem): claim adı sabitleri.
- Product/Order/OrderItem'ın tüm Command/Query'leri `ISecuredRequest` uygular, `Roles` yukarıdaki varsayılan yapıya göre.
- `LoginCommand`: kullanıcının claim adlarını repository'den alıp `CreateToken`'a verir (handler'da if yok).
- `ApplicationServiceRegisteration`: AuthorizationBehavior kaydı (Authentication'dan sonra, Logging'den önce).

### ECommerce.WebAPI
- `BusinessExceptionMiddleware`: `AuthorizationException` → 403.
- **Başlangıç admin'i (benim kararım):** Claim'ler olmadan herkes 403 alır. Migration'a sabit şifreli kullanıcı yazmak yerine,
  açılışta çalışan bir seeder `AdminSeed:Email` / `AdminSeed:Password` ayarlarını (Development'ta appsettings.Development.json,
  diğer ortamlarda env) okur; kullanıcı yoksa oluşturur ve `Admin` claim'ini atar. Ayar yoksa hiçbir şey yapmaz.

## Hedef dışı (bu iş için)
- OperationClaim CRUD ve kullanıcıya claim atama/kaldırma endpoint'leri (sonraki adım olarak eklenebilir).
- Register'da varsayılan claim verilmesi.
- Claim değişikliğinin anında etkili olması: claim'ler token'da taşınır, değişiklik **bir sonraki login'de** yansır.

## Builder'a kalanlar
- Claim sabitlerinin dosya içi düzeni, seeder'ın sınıf adı/yeri, migration'ın tam içeriği.
- Mevcut Türkçe açıklama yorum stiline uyulması; handler'larda if yok, mapping Mappers'ta (proje kuralları).
