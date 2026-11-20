# Domain

> Bootstrap 2026-11-19'da mevcut koddan (`*BusinessRules`, entity'ler) çıkarıldı ve onaylandı.
> Bir terim burada yoksa, agent kendi anlamını uydurur — önce buraya ekleyin.

## Ubiquitous language

| Term | Meaning | Notes / not to be confused with |
|---|---|---|
| Product (Ürün) | Satılan şey: ad, fiyat (`Money`), stok, en fazla bir kategori | Ad benzersiz (BR-6) |
| Category (Kategori) | Ürünlerin gruplandığı, yönetilebilir ad listesi (spec 0009) | Hiyerarşi yok; ürünün kategorisi isteğe bağlı (BR-13) |
| Stock (Stok) | Bir ürünün satılabilir adedi (`int`) | Sipariş oluşturulunca düşer (BR-4) |
| Order (Sipariş) | Bir kullanıcının kalemleriyle birlikte tek seferde kaydedilen siparişi; aggregate kökü | Kalemler ayrı kaydedilmez, Order ile birlikte |
| OrderItem (Sipariş kalemi) | Siparişteki bir ürün + adet (`Amount`) + kalem toplamı | `Amount` = adet, para değil |
| Money | Tutar (`decimal`) + para birimi (`Currency`, varsayılan `TRY`) value object'i | Para asla çıplak `decimal`/`double` değil |
| User (Kullanıcı) | E-posta + şifre hash/salt ile kimliği olan kişi | — |
| OperationClaim (Yetki) | Metinsel izin adı, örn. `Order.Write` | Rol değil; birebir, büyük/küçük harf duyarlı karşılaştırılır |
| Admin (global) | `GeneralOperationClaims.Admin`; her `ISecuredRequest`'ten geçer | `<Entity>.Admin` yalnızca o entity için |
| TokenSession | JWT'nin işaret ettiği, Redis/in-memory cache'te tutulan oturum (kullanıcı id, e-posta, yetkiler) | JWT yalnızca oturum anahtarı taşır |
| MFA / OTP Authenticator | TOTP tabanlı iki adımlı doğrulama kaydı | Login önce kısa ömürlü MFA token'ı döner |
| RefreshToken | Yeni JWT almak için kullanılan, yalnızca SHA-256 özeti saklanan token | — |
| Family (Token ailesi) | Tek bir girişten türeyen tüm refresh token'lar (`FamilyId`) | Reuse tespitinde aile topluca iptal edilir |

## Business rules
- **BR-1** Bir sipariş en az bir kalem içerir.
- **BR-2** Kalem adedi (`Amount`) 0'dan büyüktür.
- **BR-3** Kalemdeki ürünün para birimi siparişin para birimiyle aynıdır.
- **BR-4** Sipariş oluşturulunca her kalemin adedi kadar ürün stoğu düşülür; stok yetmezse sipariş hiç oluşmaz.
- **BR-5** Siparişin sahibi (`UserId`), tarihi ve fiyatları istemciden alınmaz; sunucu belirler (sahip token oturumundan).
- **BR-6** Ürün adı benzersizdir.
- **BR-7** `Money` tutarı negatif olamaz.
- **BR-8** E-posta benzersizdir; giriş hatasında kullanıcı yok / şifre yanlış ayrımı yapılmaz — tek mesaj: "E-posta veya şifre hatalı."
- **BR-9** Giriş, MFA (TOTP) doğrulanınca JWT + refresh token verir; hatalı kod denemesi `MaxVerificationAttempts` (5) ile sınırlıdır, aşılınca `LockoutDuration` (15 dk) kilit.
- **BR-10** Refresh token her kullanımda döndürülür (rotation); iptal edilmiş bir token yeniden kullanılırsa çalınmış sayılır ve aynı ailenin aktif token'ı da iptal edilir. Token'ın kendisi değil, yalnızca SHA-256 özeti saklanır.
- **BR-11** Global `Admin` her `ISecuredRequest`'ten geçer; diğer yetkiler `<Entity>.Admin`, `<Entity>.Write` / `<Entity>.Read`, `<Entity>.<İşlem>` desenindedir.
- **BR-12** Kategori adı zorunludur, kırpılarak saklanır, en fazla 100 karakterdir ve büyük/küçük harf farkı gözetilmeden benzersizdir.
- **BR-13** Bir ürünün sıfır ya da bir kategorisi vardır; verilen kategori var olmalıdır.
- **BR-14** Kendisine bağlı ürünü olan kategori silinemez.

## Key domain invariants
- Stok asla negatife düşmez (BR-4).
- Bir siparişin sahibi asla istemci girdisinden atanmaz (BR-5).
- Şifre ve refresh token düz metin olarak saklanmaz (BR-10, `docs/security.md`).

## Sensitive areas
- **Kimlik ve yetki** — şifre hash'leme, JWT/TokenSession, MFA/OTP, refresh token rotasyonu, operation
  claim yetkilendirmesi. Kod: `ECommerce.Application/Features/Auth/`, `Uludag.Core/Security/`,
  `Uludag.Core/Mediation/Behaviors/{Authentication,Authorization}Behavior.cs`, WebAPI'deki JwtBearer
  kurulumu ve `ISecuredRequest`/`IAllowAnonymousRequest` işaretleri. Bu alana dokunan spec
  Constraints'te `Sensitive area: Kimlik ve yetki` yazar.
- Sipariş/stok/para hassas alan **değil** (gerçek ödeme yok; normal süreç yeterli).
