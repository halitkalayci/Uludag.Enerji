# uludag-gen — CRUD Generator

`ECommerce.Domain/Entities` altındaki bir entity için CQRS temel CRUD dilimini, projenin desenine
(Product/Order feature'ları, F-1…F-7) uyan dosyalar olarak tek komutta üretir. AI ajanı ve geliştirici
dosyaları elle yazmaz: önce aracı çalıştırır, sonra yalnızca entity'ye özgü kısımları düzenler.
Spec: `specs/active/0005-crud-generator-cli.md` (ship sonrası `specs/done/`).

## Kurulum

```sh
sh tools/Uludag.Generator/install.sh      # kurar; kuruluysa güncel koddan yeniden kurar (güncelleme)
uludag-gen --version                      # herhangi bir dizinden
dotnet tool uninstall -g Uludag.Generator # kaldırma
```

Global .NET tool olarak `~/.dotnet/tools` altına kurulur. Paket yalnızca yerel olarak paketlenen
klasörden (`artifacts/nupkg`, gitignore'da) ve csproj'daki sürümle kurulur; nuget.org'a bakılmaz.
Şablon değiştiyse `install.sh` yeniden çalıştırılır.

## Kullanım

```sh
uludag-gen list                      # entity'leri listeler (enum ve value object hariç)
uludag-gen crud Category --dry-run   # önizleme: hiçbir dosyaya dokunmaz
uludag-gen crud Category             # üretir
uludag-gen crud Category --force     # var olan hedef dosyaların üzerine yazar
uludag-gen crud Person --plural People  # düzensiz çoğul
```

Repo içindeki herhangi bir dizinden çalışır (kökü kendisi bulur); dışarıdan `--root <yol>` ile.
Etkileşimli istem yoktur. Çıkış kodları: `0` başarı · `1` hatalı girdi · `2` çakışma.

## Ne üretir

Entity `X`, çoğul `Xs` için 16 yeni dosya:

| Katman | Dosyalar |
|---|---|
| Application `Features/X/` | `Commands/{Create,Update,Delete}/…Command.cs`, `Queries/{GetAll,GetById}/…Query.cs`, `Queries/XResponse.cs`, `Rules/XBusinessRules.cs`, `Mappers/XMapper.cs`, `Constants/XOperationClaims.cs`, `Constants/XConstants.cs`, `Locales/XLocale.{tr,en}.yaml` |
| Application `Services/Repositories/` | `IXRepository.cs` |
| Persistence | `Repositories/XRepository.cs`, `Configurations/XConfiguration.cs` |
| WebAPI | `Controllers/XsController.cs` |

ve 3 kayıt (tekrar çalıştırmada çoğaltılmaz): `UludagDbContext`'e `DbSet<X>`, Persistence DI'a repository,
`OperationClaimSeeds`'e 6 claim (`X.Admin/Write/Read/Create/Update/Delete`, sabit yeni Id'ler).

Kurallar:
- Tüm istekler `ISecuredRequest`: Create/Update/Delete → `Admin, Write, <İşlem>`; GetAll/GetById → `Admin, Read`.
- Handler'larda `if`/`else` ve mapping yok (F-7): kontrol `XBusinessRules`'ta, dönüşüm `XMapper`'da.
- Özellikler: `Guid Id` route'tan gelir (Create'te yok). Skaler (string, sayısal, bool, tarih, Guid, enum,
  nullable) aynen taşınır. Tek `Money` → `Amount`/`Currency`; birden fazla `Money` ya da aynı adlı başka
  özellik varsa → `<Özellik>Amount`/`<Özellik>Currency`. Navigation, koleksiyon ve tanınmayan tipler
  atlanır ve çıktıda listelenir. `UserId` komutlara girmez (sunucuda atanır), yanıtta kalır.
- Update tam değiştirmedir (PUT). Kayıt yoksa 404.
- Hedef dosyalardan biri varsa hiçbir şey yazılmaz (`--force` hariç). `Guid Id`'siz entity reddedilir.

## Üretimden sonra (çıktı da yazar)

1. **ZORUNLU — migration:** `dotnet ef migrations add add_x -p ECommerce/src/ECommerce.Persistence -s ECommerce/src/ECommerce.WebAPI`
   (yeni tablo + seed; migration olmadan tablo oluşmaz, EF Core 9 bekleyen model değişikliğinde durur).
2. Entity'ye özgü iş kuralları → `XBusinessRules`, handler'dan çağrılır.
3. Kimliğe bağlı alanlar (kullanıcı, tarih, fiyat, toplam) istemciden alınmaz (`docs/conventions.md`, Data rules).
4. Uzunluk, indeks, ilişki → `XConfiguration`. Mesaj metinleri → `Locales/XLocale.{tr,en}.yaml`.

## Geliştirme

- Kod: `Cli/` (argümanlar), `Workspace/` (repo kökü), `Analysis/` (Roslyn ile Domain okuma),
  `Generation/` (plan → çakışma kontrolü → yazma, kayıt eklemeleri, rapor), `Templates/` (C# raw string).
- Testler: `tests/Uludag.Generator.Tests` — fast: okuma, eşleme, çakışma, F-7, yetki matrisi;
  full (`Category=Integration`): üretilen kodu repo dışındaki geçici bir kopyada derler.

### Çalışırlık gözlemi (spec AC-3/AC-4, VERIFY)

Domain kirlenmesin diye atılan bir dalda yapılır (spec G-1, plan 0005 R-9):

1. `git switch -c tmp/0005-smoke`; `ECommerce.Domain/Entities/PriceAlert.cs` olarak
   `tests/Uludag.Generator.Tests/Infrastructure/Fixtures.cs` içindeki `PriceAlert`'i ekle. (Kimliğe bağlı FK
   içermez: `Category`'nin `UserId`'si komutlara girmediği için Create, FK ihlaliyle 500 dönerdi.)
2. `uludag-gen crud PriceAlert` → `dotnet ef migrations add add_price_alert -p … -s …`. Üretilen koda dokunma.
3. `tests/ECommerce.IntegrationTests` altında `ApiFixture` ile geçici bir test: kullanıcı, `PriceAlert.*`
   claim'leri ve token uygulamanın kendi servisleriyle hazırlanır; oluştur (`ProductId` = seed ürün
   `2dcb27b9-06cf-4053-a423-c2e94ed048a6`) → listele → getir → güncelle → sil; olmayan id → 404
   ProblemDetails; token'sız → 401; claim'siz kullanıcı → 403.
4. `dotnet test tests/ECommerce.IntegrationTests --filter "FullyQualifiedName~PriceAlert"` ve `./scripts/check`
   (mimari testler) → sonucu kanıt tablosuna yaz; `git switch -` ve `git branch -D tmp/0005-smoke`.
