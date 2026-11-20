# Spec 0005 — CRUD Generator CLI

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0005-plan.md`
- Self-critique: done

## Intent
Geliştirici ve AI ajanı, `ECommerce.Domain` altına yeni bir entity eklediğinde o entity'nin temel CRUD
dilimini (komutlar, sorgular, yanıt modeli, iş kuralları, mapper, yetki sabitleri, repository, controller)
her seferinde elle — ve her seferinde biraz farklı — yazmak zorunda kalıyor. İstenen: entity adı verilerek
çalıştırılan bir **Generator CLI**'ın bu dilimi projenin mevcut desenine (Product feature'ı referans) ve
mimari kurallarına (F-1…F-7) birebir uyan dosyalar olarak tek komutta üretmesi; AI ajanı dosyaları
yazmakla vakit harcamadan CLI'ı çalıştırıp yalnızca entity'ye özgü iş kurallarını sonradan düzenler.
Başarı: yeni bir entity için tek komutla derlenen, mimari testlerden geçen, Swagger'da çalışan CRUD
uçları elde edilir ve araç bu bilgisayarda global komut olarak kuruludur. Bilinçli olarak yapılmayan:
entity'ye özgü iş kuralı üretimi, migration oluşturma, Angular istemci kodu, test üretimi.

## Requirements
- R-1 **Entity seçimi.** Araç, `ECommerce.Domain/Entities` altındaki entity'leri kaynak koddan okur
  (çözümün derlenmesini gerektirmez); kullanıcı entity'yi adıyla seçer; mevcut entity'ler listelenebilir.
- R-2 **Üretilen dilim** (entity `X`, çoğul `Xs`): Create / Update / Delete komutları, GetAll / GetById
  sorguları, `XResponse`, `XBusinessRules` (yalnızca "seçilen kayıt var olmalı" kuralı), `XMapper`,
  `XOperationClaims`, `XConstants` (mesaj anahtarları) + `Locales/XLocale.{tr,en}.yaml`
  (`docs/conventions.md`, Order kalıbı), `IXRepository`, EF Core `XRepository`, `XConfiguration`
  (yalnızca iskelet), `XsController`. Dosya yolları ve adları mevcut Product feature'ıyla aynı düzendedir. Çoğul ad basit
  İngilizce kurallarıyla türetilir (y→ies; s/x/ch/sh→es; diğerleri +s) ve seçenekle elle verilebilir.
  `XConfiguration` her `Money` özelliği için owned-type eşlemesini (18,2 hassasiyet, 3 karakter para birimi)
  içerir. Update tam değiştirmedir (PUT; Product gibi), kısmi güncelleme yoktur.
- R-3 **Kayıtlar.** Üretilen parçalar sisteme bağlanır: DbContext'e `DbSet`, Persistence DI'a repository
  kaydı, operation claim seed'lerine 6 yeni claim (sabit, yeni üretilmiş Id'lerle). Bu ekler tekrar
  çalıştırmada çoğaltılmaz (idempotent).
- R-4 **Özellik eşleme.** `Id` (Guid) Create gövdesine girmez; Update/Delete/GetById'de route'tan gelir.
  Skaler özellikler (string, sayısal tipler, bool, DateTime, Guid, enum ve bunların nullable'ları) komut
  ve yanıta aynen taşınır. `Money` tek ise `Amount` + `Currency` olarak düzleştirilir (Product şekli);
  birden fazla `Money` varsa `<Özellik>Amount` + `<Özellik>Currency` olur. Navigation özellikleri
  (entity tipli ve koleksiyon) atlanır; FK Guid'leri skaler olarak kalır — `UserId` hariç: kimliğe bağlı
  alan olduğundan (`docs/conventions.md`, Data rules) Create/Update komutlarına girmez, yanıtta kalır ve
  çıktıda "sunucuda oturumdan atanmalı" uyarısı verilir. Tanınmayan tipler
  atlanır ve uyarıyla raporlanır.
- R-5 **Mimari uyum.** Üretilen handler'larda `if`/`else` ve mapping yoktur (F-7); controller yalnızca
  `IMediator` kullanır (F-5); Application EF Core'a referans vermez (F-2). Tüm istekler varsayılan olarak
  korumalıdır: Create/Update/Delete → `Admin, Write, <İşlem>`; GetAll/GetById → `Admin, Read`
  (`ISecuredRequest`, Product'ın belgelenmiş claim yapısı). Anonim istek üretilmez.
- R-6 **Güvenli yazma.** Hedef dosyalardan biri bile varsa hiçbir dosya yazılmaz, çakışan dosyalar listelenir
  ve araç hata koduyla çıkar; üzerine yazma yalnızca açık bir seçenekle olur. Kuru çalıştırma (dry-run)
  hiçbir dosyaya dokunmadan yazılacak/değişecek dosyaları listeler.
- R-7 **AI dostu çıktı.** Araç etkileşim istemeden (argümanla) çalışır; çıktısı oluşturulan/değişen
  dosyaları, atlanan özellikleri ve sonraki adımları (migration komutu, entity'ye özgü kuralların yeri) düz
  metin olarak yazar; başarı 0, her hata sıfırdan farklı çıkış kodudur. Seed eklendiği için uygulama
  migration eklenmeden açılmaz (EF Core 9 bekleyen model değişikliğinde durur): migration adımı çıktıda
  **zorunlu** adım olarak yazılır.
- R-8 **Kurulum.** Araç bu bilgisayara global .NET tool olarak kurulur ve herhangi bir dizinden tek komutla
  çağrılır; repo içinden çalıştırıldığında repo kökünü kendisi bulur. Kurulum, güncelleme ve kaldırma
  tek belgelenmiş komutla yapılır.
- R-9 **Ajan kullanımı belgelenir.** Proje kuralları (AI'ın okuduğu belgeler), "yeni entity için CRUD
  gerekiyorsa önce generator'ı çalıştır, sonra düzenle" yönergesini ve komut kullanımını içerir.

## User experience
none — no user interface (komut satırı aracı; akış R-1, R-6, R-7'de tanımlı).

## Constraints & out of scope
- **Kararlar** (lite: netleştirme taslağa katlandı; D-1…D-6 ve self-critique G-1…G-6 kabul — Halit Enes
  Kalaycı, 2026-11-20):
  - D-1 Konum ve ad: `tools/Uludag.Generator` projesi + `tests/Uludag.Generator.Tests`, çözüme (`.slnx`)
    eklenir; böylece `scripts/check` aracı da derler ve test eder. Komut adı: `uludag-gen`.
  - D-2 Seçim argümanla yapılır (`uludag-gen crud <Entity>`, `uludag-gen list`); etkileşimli menü yok —
    AI ajanı etkileşimli istemi kullanamaz.
  - D-3 Kapsam yalnızca bu repo düzeni (ECommerce katmanları, Product deseni); başka projelere
    genelleştirme ve şablon özelleştirme V1 dışı.
  - D-4 Kayıt dosyalarına (DbContext, Persistence DI, OperationClaimSeeds) ekleme yapılır; migration
    üretilmez, yalnızca komutu yazdırılır (şema değişikliği insan onayıyla).
  - D-5 Hassas alan: araç Auth kodunu değiştirmez; yalnızca mevcut `ISecuredRequest` işaretini Product ile
    aynı biçimde **yeni** dosyalara yazar ve varsayılanı "korumalı"dır. Bu nedenle `Sensitive area: none`
    önerilir; üretilen bir entity'nin gerçek kullanımı kendi spec'inde değerlendirilir.
  - D-6 Spec numarası 0005: kurala göre sıradaki numara 0003'tür, ancak spec 0002 0003 ve 0004'ü Auth
    serisine (token yaşam döngüsü, yetkilendirme) ayırmıştır; çakışmamak için 0005 kullanıldı.
- **Kanıt ortamı (G-1):** fixture entity repoda kalıcı olmaz. Build kanıtı repo dışındaki geçici bir
  kopyada çalışan entegrasyon testiyle; çalışırlık (AC-3/AC-4) atılan bir branch'te tekrarlanabilir
  gözlemle alınır.
- **Bağımlılık:** kaynak kod çözümleme için bir NuGet paketi (Roslyn) gerekecektir; PLAN kapısında onaylanır
  (`docs/security.md`, Dependencies). Mimari değişiklik (yeni araç projesi) `docs/architecture.md`'ye
  işlenir.
- **OUT OF SCOPE:** entity'ye özgü iş kuralları (ör. "ad benzersiz"), alan doğrulama (validation), migration
  üretimi, Angular istemci kodu, üretilen dilim için test üretimi, Dapper repository, sayfalama/filtreleme,
  mevcut feature'ların yeniden üretilmesi/dönüştürülmesi, NuGet'e yayın, entity dosyasının kendisini
  üretmek.
- Sensitive area: none (D-5)
- Spec revizyonu 1 (plan 0005 R-6/R-7, docs ↔ spec çelişkisi R-10: mesaj anahtarları + `UserId`) — Halit
  Enes Kalaycı, 2026-11-20.
- Kriter sayısı 10, tek yetenek ("entity'den CRUD dilimi üret") + kurulumu — bölünmedi.

## Acceptance criteria
- [ ] AC-1 — Liste: `uludag-gen list` `ECommerce.Domain/Entities` altındaki entity sınıflarını (enum ve
  value object hariç) adlarıyla listeler; çözüm derlenmiyorken de çalışır.
- [ ] AC-2 — Üretim (mutlu yol): Domain'e eklenen bir fixture entity (skaler + tek `Money` + FK Guid +
  navigation içeren) için `uludag-gen crud <Entity>` R-2'deki 16 dosyayı Product ile aynı yol/ad düzeninde
  oluşturur, R-3'teki 3 kaydı ekler ve 0 ile çıkar; ardından `scripts/check` (build + mimari testler F-1…F-5)
  yeşildir.
- [ ] AC-3 — Çalışırlık: AC-2'de üretilen uçlar çalışan API'de yetkili bir kullanıcıyla
  oluştur → listele → id ile getir → güncelle → sil akışını tamamlar; olmayan id'ye GetById/Update/Delete
  404 ProblemDetails döner.
- [ ] AC-4 — Yetki varsayılanı: üretilen 5 isteğin hepsi `ISecuredRequest` uygular ve rolleri R-5'teki
  matristir; token'sız istek 401, ilgili claim'i olmayan kullanıcı 403 alır.
- [ ] AC-5 — F-7: üretilen Command/Query handler'larında `if`/`else`/`switch` ve nesne başlatıcıyla mapping
  bulunmaz; kontroller `XBusinessRules`'ta, dönüşümler `XMapper`'dadır.
- [ ] AC-6 — Özellik eşleme: R-4'ün her kuralı ayrı doğrulanır — `Id` Create'te yok; tek `Money` →
  `Amount`/`Currency`; iki `Money` → önekli alanlar; navigation ve koleksiyon atlanır ve çıktıda uyarı
  olarak listelenir; FK Guid ve nullable skaler korunur; `UserId` komutlara girmez, yanıtta kalır, uyarı verilir.
- [ ] AC-7 — Çakışma ve force: hedef dosyalardan biri varken komut hiçbir dosya yazmadan/değiştirmeden
  sıfırdan farklı kodla çıkar ve çakışanları listeler; force seçeneğiyle üzerine yazar; aynı entity için
  tekrar çalıştırma kayıt eklerini (DbSet, DI, seed) çoğaltmaz.
- [ ] AC-8 — Hatalı girdi: olmayan entity adı, enum/value object adı ve repo dışında (kök bulunamayan
  dizinde) çalıştırma ve `Guid Id` özelliği olmayan entity, açıklayıcı mesaj ve sıfırdan farklı kodla
  sonuçlanır; hiçbir dosyaya dokunulmaz.
  Dry-run seçeneği aynı listeyi yazdırır ve hiçbir dosyaya dokunmaz.
- [ ] AC-9 — Kurulum: belgelenen kurulum komutu sonrası `dotnet tool list -g` aracı gösterir ve
  `uludag-gen --version` repo dışındaki bir dizinden çalışır; belgelenen güncelleme ve kaldırma komutları
  çalışır. (Kanıt: bu bilgisayardaki tekrarlanabilir gözlem.)
- [ ] AC-10 — Ajan yönergesi: AI'ın okuduğu proje belgeleri (`AGENTS.md` "Where things live" ve
  `docs/conventions.md`) generator'ın ne zaman ve nasıl kullanılacağını, üretim sonrası düzenlenecek
  yerleri (entity'ye özgü kurallar, migration) içerir; `docs/architecture.md` yeni aracı modül tablosunda
  gösterir.

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
