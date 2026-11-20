# Spec 0001 — Test altyapısı

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0001-plan.md`
- Self-critique: done

## Intent
Bugün repoda tek bir test yok; `scripts/check` yalnızca derlemeyi doğruluyor, bu yüzden sonraki
hiçbir spec kabul kriterini test kanıtına bağlayamıyor. Geliştirici (ve agent'lar) için, `docs/testing.md`'de
bootstrap'ta onaylanan düzende üç test katmanı kurulur: hızlı unit testler (xUnit + NSubstitute),
mimari sınırları (F-1…F-5) koruyan mimari testler (NetArchTest) ve gerçek SQL Server + Redis'e karşı
koşan entegrasyon testleri (Testcontainers). Başarı: `./scripts/check --fast` unit + mimari testleri,
`./scripts/check` (full) bunlara ek olarak entegrasyon testlerini koşar ve bir test kırmızıysa check
kırmızıdır. Bilinçli olarak yapılmayan: iş kurallarının (BR-1…BR-11) tamamını test etmek, kimlik/yetki
yollarını test etmek ve Angular istemci testleri — her biri katmanların hazır olduğu bu altyapının
üzerine ayrı spec'lerle gelir.

## Requirements
- R-1 **Unit katmanı.** Paylaşılan çekirdek (`Uludag.Core`) ve uygulama katmanı (`ECommerce.Application`)
  için ayrı unit test projeleri vardır; bağımlılıklar test ikizleriyle (NSubstitute) değiştirilir, dış
  kaynak (veritabanı, ağ, gerçek saat) kullanılmaz. Her proje, katmanın çalıştığını gösteren en az bir
  gerçek davranış testi taşır (AC-7, AC-8).
- R-2 **Mimari katmanı.** `docs/architecture.md`'deki F-1…F-5 yasak bağımlılıklarının her biri ayrı bir
  mimari testle doğrulanır; bugünkü kod bu kurallara uyar, ihlal eden bir bağımlılık eklendiğinde ilgili
  test kırmızı olur. Hiçbir mimari test boş bir tip kümesi üzerinde "geçemez".
- R-3 **Entegrasyon katmanı.** Entegrasyon testleri uygulamayı süreç içinde, her koşu için yeni açılan
  temiz bir SQL Server ve Redis'e karşı ayağa kaldırır; koşu bitince (başarısızlıkta da) bu kaynaklar
  aynı adım içinde kapatılır ve hiçbir veri bir sonraki koşuya geçmez. Bu spec'te katman yalnızca
  uygulamanın gerçek altyapıyla ayağa kalktığını kanıtlar (AC-9); kimlik/yetki yollarına dokunmaz.
- R-4 **Tek sözleşme, iki hız.** Entegrasyon testleri `Category=Integration` özelliğiyle işaretlenir;
  hızlı profil bu özelliği taşımayan tüm testleri, full profil ek olarak işaretli testleri koşar
  (selector ile bölme, liste ile değil — `docs/testing.md`, Two speeds 4). Docker gerektiren adım bu
  ihtiyacı önkoşul olarak bildirir; Docker yoksa full profil kırmızıdır, adım asla atlanmaz.
- R-5 Test projeleri çözüme (`Uludag.Enerji.slnx`) eklenir ve `docs/testing.md`'deki adlandırma
  (`<Metot>_<Durum>_<Beklenen>`, sınıf `<TestEdilen>Tests`) ve determinizm (zaman `FakeTimeProvider`,
  kültür `InvariantCulture`) kurallarına uyar.

## User experience
none — no user interface

## Constraints & out of scope
- Katman yerleşimi ve araçlar `docs/testing.md` "Frameworks & layout" tablosunda bootstrap'ta
  onaylandığı gibidir; bu spec onları değiştirmez.
- Üretim kodu davranışı değişmez. Bir testin yazılabilmesi için üretim kodunda değişiklik gerekirse
  (ör. test ortamı için bir ayar kancası) bu plana açıkça yazılır ve davranışı değiştirmez.
- Fast profilin test adımı tek başına ~1 dakikanın altında kalır (`docs/testing.md`, Two speeds 3;
  3 koşunun medyanı); entegrasyon full-only'dir.
- `./scripts/residue` her iki profilde temiz olmalıdır (`check.conf` değiştiği için; BUILD'in Done'ı,
  kabul kriteri değil) — buna entegrasyon için açılan container'lar da dahildir.
- **OUT OF SCOPE:**
  - BR-1…BR-11'in tam test kapsamı ve mevcut kod için characterization testleri (sonraki spec'ler).
  - Kimlik ve yetki yolları (401/403, MFA kilidi, refresh token reuse) ve HTTP exception → durum kodu
    sözleşmesi testleri (ayrı spec; hassas alan).
  - F-6 ve F-7 için otomatik test (`docs/architecture.md`: review'da kontrol edilir).
  - Angular istemci testleri (`client-test` adımı — istemcide henüz test builder'ı yok; ayrı spec).
  - `format` adımı (B-4'e bağlı) ve B-1…B-5'teki diğer backlog maddeleri.
  - CI pipeline'ında değişiklik (check zaten CI'ın çalıştırdığı tek komuttur).
- Sensitive area: yok — bu spec kimlik/yetki koduna ne test ne değişiklik olarak dokunur.
- Split: değerlendirildi (unit+mimari / entegrasyon); tek spec olarak öneriliyor — tek yetenek
  ("check testleri koşar"), `check.conf` etkinleştirmesi tek değişiklik, 10 kriter sınırda.

## Acceptance criteria
- [ ] AC-1 — F-1: derlenmiş `ECommerce.Domain` derlemesi yalnızca .NET temel kütüphanelerine referans verir — hiçbir çözüm projesine ve üçüncü taraf derlemeye değil (build-time analizörler, ör. BannedApiAnalyzers, sayılmaz); mimari test bugünkü kodda yeşildir.
- [ ] AC-2 — F-2: `ECommerce.Application` tipleri `ECommerce.Persistence`, `ECommerce.Infrastructure`, `ECommerce.WebAPI` ve `Microsoft.EntityFrameworkCore*` isim alanlarına bağımlı değildir; mimari test yeşildir.
- [ ] AC-3 — F-3: `Uludag.Core` tipleri hiçbir `ECommerce.*` isim alanına bağımlı değildir; mimari test yeşildir.
- [ ] AC-4 — F-4: `ECommerce.Persistence` tipleri `ECommerce.WebAPI` isim alanına bağımlı değildir; mimari test yeşildir.
- [ ] AC-5 — F-5: WebAPI controller'ları `ECommerce.Application.Services` isim alanına (repository arayüzleri dahil), `ECommerce.Persistence` isim alanına ve `Microsoft.EntityFrameworkCore*` isim alanlarına bağımlı değildir (iş yalnızca `IMediator` üzerinden akar); mimari test yeşildir.
- [ ] AC-6 — AC-1…AC-5'teki her mimari test, kuralı değerlendirdiği tip kümesinin boş olmadığını da doğrular (yanlış isim alanı/derleme filtresiyle boşuna geçemez).
- [ ] AC-7 — `Uludag.Core` unit testi: in-memory Leaky Bucket rate limiter, kapasite içindeki istekleri kabul eder, kapasiteyi aşan isteği reddeder ve sahte saatle (FakeTimeProvider) yeterli süre ilerletildiğinde yeniden kabul eder — gerçek saat beklenmeden.
- [ ] AC-8 — `ECommerce.Application` unit testi (BR-6): repository aynı adda ürün döndürdüğünde `ProductNameCannotExist` `BusinessException` fırlatır; döndürmediğinde fırlatmaz — repository NSubstitute ikiziyle sağlanır.
- [ ] AC-9 — Entegrasyon testi (`Category=Integration`): uygulama yeni açılmış SQL Server + Redis container'larına karşı süreç içinde ayağa kalkar; veritabanında bekleyen migration yoktur ve uygulamanın cache servisine yazılan bir değer doğrudan Redis container'ından (uygulamanın cache servisi üzerinden değil) geri okunur. Test ortamı kendi ayarlarını (bağlantı dizeleri, token anahtarı vb.) test içinde verir; repodaki appsettings secret'larına dayanmaz (B-1). Koşu bitince container'lar durdurulmuştur; art arda iki koşunun ikincisi de boş veritabanıyla başlar.
- [ ] AC-10 — `scripts/check.conf`: `./scripts/check --fast` unit + mimari testleri koşar ve entegrasyon testlerini koşmaz; `./scripts/check` (full) ek olarak entegrasyon testlerini koşar; bölme `Category` selector'ıyla yapılır (yeni eklenen işaretsiz bir test fast'te, işaretli bir test full'de koşar); entegrasyon adımı `requires` ile Docker'ı ister ve Docker yoksa full profil kırmızıdır, adım atlanmaz. Kanıt: selector bölmesi için `check.conf`'taki birbirinin tersi iki filtre (inceleme; ayrı test eklenmez); Docker önkoşulu için `requires integration: docker info` satırı + Docker kapalıyken bir kez koşulan `./scripts/check` sonuç satırı.

## Definition of Done
- [ ] Every acceptance criterion mapped to proof (test or reproducible observation)
- [ ] `scripts/check` green
- [ ] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [ ] Docs / ADRs updated if behavior or architecture changed
- [ ] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | |
| Fix rounds | |
| Review findings: real / noise / deferred | |
| Regressions introduced | |
| Bugs escaped to production | |
