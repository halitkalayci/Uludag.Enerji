# Spec 0002 — Auth: hesap ve giriş testleri

- Status: Shipped
- Mode: lite
- Plan: `specs/plans/0002-plan.md`
- Self-critique: done

## Intent
Ekip, kimlik ve yetki sisteminin tamamını, sonraki domain testlerine (Product, Order, OrderItem) temel
olacak şekilde, sistemin dışından — HTTP üzerinden, gerçek veritabanı ve cache ile — test etmek istiyor.
Bu spec üç parçalı Auth test serisinin ilkidir ve **hesap açma ve giriş** yolunu kapsar: kayıt, e-posta
benzersizliği (BR-8), şifre ile giriş, iki adımlı doğrulamanın (MFA/TOTP) kurulumu ve doğrulanması,
hatalı kod kilidi (BR-9) ve beklenmeyen hatada sözleşmeye uygun yanıt. Testler **"koda güvenme"**
ilkesiyle yazılır: beklenen davranış koddan değil, `docs/domain.md` iş kurallarından, `docs/security.md`
kurallarından ve bu spec'teki kararlardan türetilir; kodla çelişen test kırmızı kalır ve bulgu olur.
Başarı: her kabul kriterinin bir kanıt testi vardır ve Auth kodu için ölçülmüş satır/dal coverage
oranı raporlanır. Bilinçli olarak yapılmayan: unit test, üretim kodunda değişiklik, tarayıcı e2e.

## Requirements
- R-1 **Kara kutu, uçtan uca.** Her test sistemi bir istemcinin göreceği gibi kullanır: isteği HTTP ile
  gönderir, yanıtı (durum kodu + gövde) ve gerekiyorsa kalıcı sonucu (veritabanı, cache) doğrular.
  Uygulama her koşu için yeni açılan temiz veritabanı ve cache'e karşı çalışır (spec 0001 altyapısı).
- R-2 **Beklenti kaynağı.** Beklenen davranış BR-8, BR-9, `docs/security.md` ve bu spec'in kararlarıdır;
  mevcut kodun yaptığı şey beklenti kaynağı değildir. Domain'in sessiz kaldığı noktalar bu spec'te
  kararla sabitlenmiştir (Constraints, "Kararlar").
- R-3 **Konu ↔ hazırlık ayrımı.** Testin konusu olan adım her zaman gerçek HTTP sözleşmesiyle yapılır
  (ör. giriş testi yalnızca e-posta + şifre gönderir). Konusu olmayan hazırlık adımları (bir kullanıcının
  var olması, bir MFA oturumunun açık olması) uygulamanın kendi servisleriyle yapılabilir; böylece bir
  akıştaki hata başka akışların testlerini maskelemez. Sözleşmede olmayan alan gönderilerek hata
  atlatılmaz.
- R-4 **Hata sözleşmesi.** Kural ihlali 400, kimlik doğrulama hatası 401 döner; tüm hata yanıtları
  ProblemDetails biçimindedir ve iç detay (istisna tipi, yığın, iç mesaj) sızdırmaz.
- R-5 **Coverage.** Spec sonunda Auth kodunun bu testlerle ölçülen satır ve dal coverage oranı
  raporlanır; rapor repoya konmaz (`docs/testing.md`, Evidence policy), oran ve yeniden üretme komutu
  verilir.

## User experience
none — no user interface

## Constraints & out of scope
- **Kararlar** (netleştirme, Halit Enes Kalaycı, 2026-11-20 — tüm öneriler kabul):
  - Katman: testler mevcut entegrasyon katmanında (`Category=Integration`, full profil) HTTP üzerinden
    yazılır; "e2e/API" ihtiyacı bu katmanda karşılanır. Tarayıcı e2e yok (bütçe 0, ADR gerektirir).
  - Unit test yok (kullanıcı kararı; `docs/testing.md` "Test layers 1" bu seri için bilinçli olarak
    uygulanmaz).
  - Kırmızı testler atlanmaz, gevşetilmez; her biri R-02 ile triage'a gider, düzeltme ayrı `/fix-bug`
    spec'iyle yapılır. Bu spec, `scripts/check` full yeşil olana kadar ship edilmez.
  - Geçersiz e-posta biçimi 400'dür; e-posta benzersizliği büyük/küçük harfe duyarsızdır; eşzamanlı
    aynı e-posta kayıtlarından yalnızca biri başarılı olur; geçerli bir TOTP kodu ikinci kez kabul
    edilmez (RFC 6238 §5.2); beklenmeyen hata 500 ProblemDetails'tir ve detay sızdırmaz.
  - Zamana bağlı davranışlar beklenmeden test edilir: kilidin süresi ölçülür, kendiliğinden açılması
    test edilmez.
- **Üretim koduna dokunulmaz.** Değişiklik yalnızca `tests/` altında (ve gerekirse test ayarlarında) olur.
  Yeni test paketi (coverage) PLAN kapısında onaylanır (`docs/security.md`, Dependencies).
- **Analizden gelen hipotezler** (kanıt değil; testler doğrular ya da çürütür): A-1 sözleşmeye uygun
  giriş isteği sunucu hatası veriyor; A-2 e-posta için veritabanı düzeyinde benzersizlik yok (eşzamanlı
  kayıt); A-3 e-posta biçimi kontrol edilmiyor; A-4 TOTP kodu tekrar kullanılabiliyor.
- **OUT OF SCOPE:**
  - Refresh token rotasyonu/reuse, token iptali, çıkış, access token reddi → spec 0003.
  - Yetkilendirme (claim matrisi, global Admin, yetki atama, varsayılan reddet taraması), admin seed → spec 0004.
  - Şifre politikası (uzunluk/karmaşıklık) — domain'de tanımlı değil; backlog sorusu.
  - Girişte e-postanın büyük/küçük harf duyarlılığı; zamanlama tabanlı kullanıcı numaralandırma.
  - Kilidin kendiliğinden açılması; rate limiting (429).
  - Reuse tespitinin açık access token oturumlarını kapatmaması (A-5) ve şifre hash algoritmasının
    gücü (A-6) — review/backlog konuları, bu spec'in testi değil.
- Self-critique G-1…G-8 kabul edildi ve kriterlere işlendi (Halit Enes Kalaycı, 2026-11-20). Kriter sayısı 12: tek
  yetenek ("hesap aç ve giriş yap"), AC-12 raporlama kriteri — bölünmedi.
- Sensitive area: Kimlik ve yetki
- Split: Auth serisi üçe bölündü — 0002 hesap ve giriş, 0003 token yaşam döngüsü, 0004 yetkilendirme
  (bu sırayla); ardından alt domain nesneleri (Product, Order, OrderItem). Kabul: Halit Enes Kalaycı,
  2026-11-20.

## Acceptance criteria
- [x] AC-1 — Kayıt: daha önce kullanılmamış geçerli e-posta ve şifreyle kayıt başarılı olur; kullanıcı kalıcı olarak oluşur ve şifre düz metin olarak hiçbir yerde saklanmaz (kayıtta şifre metni bulunmaz, hash ve salt doludur).
- [x] AC-2 — BR-8 benzersizlik: kayıtlı bir e-posta ile — aynı yazımla da, yalnızca büyük/küçük harfi farklı yazımla da — yapılan kayıt 400 ProblemDetails ile reddedilir ve ikinci kullanıcı oluşmaz.
- [x] AC-3 — Eşzamanlı kayıt: aynı e-postayla aynı anda gönderilen 10 kayıt isteğinden tam olarak biri başarılı olur, diğerleri 400 ile reddedilir; sonunda o e-postayla tam olarak bir kullanıcı vardır.
- [x] AC-4 — Geçersiz kayıt girdisi: e-posta ya da şifre eksik veya boşsa, ya da e-posta biçimi açıkça geçersizse (`abc`, `a@`, `@x.com`, `a b@x.com`) kayıt 400 ProblemDetails ile reddedilir ve kullanıcı oluşmaz.
- [x] AC-5 — Giriş (doğru bilgiler): yalnızca e-posta ve şifre gönderilen giriş başarılı olur ve erişim token'ı değil, süreli (gelecekte biten, UTC) bir MFA token'ı döner; MFA'sı henüz kurulmamış kullanıcı için durum "kurulum gerekli", kurulu kullanıcı için "kod gerekli"dir.
- [x] AC-6 — BR-8 giriş hatası: kayıtlı olmayan e-posta ile ve kayıtlı e-postanın yanlış şifresiyle yapılan girişler aynı 400 yanıtını verir (durum kodu, başlık ve mesaj birebir aynı; yanıt süresi karşılaştırılmaz); hiçbiri MFA token'ı döndürmez — yanıttan kullanıcının var olup olmadığı anlaşılamaz.
- [x] AC-7 — MFA kurulumu: geçerli bir MFA token'ıyla kurulum, doğrulayıcı uygulamaya eklenebilecek bir gizli anahtar ve TOTP biçiminde (`otpauth://totp/` ile başlayan), kullanıcının e-postasını ve ayarlardaki uygulama adını (issuer) içeren bir anahtar URI'si döner; geçersiz/bilinmeyen MFA token'ı ile ve MFA'sı zaten doğrulanmış kullanıcı için kurulum 400 ile reddedilir; doğrulamadan önce kurulum tekrarlanırsa yalnızca son anahtarın kodu kabul edilir.
- [x] AC-8 — MFA doğrulama (başarılı): doğru TOTP koduyla doğrulama erişim token'ı ve refresh token'ı (bitiş zamanlarıyla) döner; erişim token'ı yalnızca kimlik doğrulama isteyen çıkış isteğini geçirir (token olmadan aynı istek 401); kullanılan MFA token'ı ikinci kez kullanılamaz (400); refresh token'ın kendisi saklanmaz, yalnızca SHA-256 özeti saklanır; doğrulama sonrası kullanıcının MFA'sı "kurulu" sayılır (sonraki giriş "kod gerekli" döner).
- [x] AC-9 — MFA doğrulama (reddedilen kod): yanlış kod, 6 haneli rakam olmayan kod ve daha önce başarıyla kullanılmış geçerli bir kodun tekrarı 400 ile reddedilir ve token dönmez.
- [x] AC-10 — BR-9 kilit: hatalı denemeler kullanıcı başına sayılır ve MFA oturumları arasında toplanır (ör. bir oturumda 3, yeni bir girişle açılan diğerinde 2); toplam 5 hatalı denemeden sonra 6. deneme — kod doğru olsa bile — 400 ile reddedilir ve token dönmez; yeni MFA oturumu sayacı sıfırlamaz; kilidin süresi 15 dakikadır (ölçülür, beklenmez); limite ulaşmadan yapılan başarılı doğrulama sayacı sıfırlar.
- [x] AC-11 — Beklenmeyen hata: bir Auth isteği sırasında beklenmeyen bir sunucu hatası oluştuğunda (testte bir bağımlılığın hata fırlatan test ikiziyle değiştirilmesiyle üretilir; mevcut bir kod hatasına dayanılmaz) yanıt 500, `application/problem+json`'dur ve gövdede istisna tipi, yığın izi ya da iç hata mesajı bulunmaz.
- [x] AC-12 — Coverage: Auth kodunun (hesap/giriş/MFA akışları, Auth kuralları, ilgili güvenlik yardımcıları ve hata eşleme) bu spec'in testleriyle ölçülen satır ve dal coverage oranı, yeniden üretme komutuyla birlikte raporlanır; rapor dosyası repoda değildir.

## Definition of Done
- [x] Every acceptance criterion mapped to proof (test or reproducible observation)
- [x] `scripts/check` green
- [x] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [x] Docs / ADRs updated if behavior or architecture changed
- [x] Spec moved to `specs/done/` (it becomes immutable there)

## Scorecard (fill at ship — honest numbers make the process improvable)
| Metric | Value |
|---|---|
| Spec revisions | 0 (öz-eleştiri G-1…G-8 onay öncesi işlendi) |
| Fix rounds | 2 test fix round'u (fix-1, fix-2) + 5 `/fix-bug` lane'i (A-1, A-8, A-4 [+fix-1], A-2, A-3+A-7) |
| Review findings: real / noise / deferred | 10 / 6 / 14 (B-7…B-23; triage: plan `## Triage`) |
| Regressions introduced | 0 |
| Bugs escaped to production | — (ship anında bilinmiyor). Not: testler 6 mevcut kod hatasını yakaladı ve düzeltildi (A-1 giriş 500, A-2, A-3, A-4, A-7, A-8) |
