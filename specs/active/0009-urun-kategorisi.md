# Spec 0009 — Ürün kategorisi

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0009-plan.md`
- Self-critique: done

## Intent
Mağaza yöneticisi ürünleri kategorilere ayırabilmek istiyor: kategoriler yönetilebilir bir liste
olarak tutulur ve her ürün en fazla bir kategoriye bağlanır. Başarı: yetkili kullanıcı kategori
oluşturup ürünlere atayabilir, ürünü okuyan herkes ürünün kategorisini (kimlik + ad) görür, mevcut
ürünler hiçbir veri kaybı olmadan "kategorisiz" olarak çalışmaya devam eder. Bilinçli olarak
yapılmayanlar: kullanıcı arayüzü (Angular), kategoriye göre ürün filtreleme, alt kategori /
hiyerarşi, bir ürünün birden fazla kategorisi.

## Requirements
- **R-1 Kategori yönetimi.** Yetkili kullanıcı kategori oluşturur, listeler, tek kategoriyi
  görüntüler, adını değiştirir ve siler. Kategorinin tek iş alanı **ad**dır.
- **R-2 Kategori adı.** Zorunludur; baştaki/sondaki boşluklar kırpılarak saklanır; kırpıldıktan
  sonra boş olamaz ve en fazla 100 karakterdir. Ad benzersizdir, büyük/küçük harf farkı
  gözetilmez ("Elektronik" ile "elektronik" aynı addır). Ad değiştirilirken kategorinin kendi
  mevcut adı çakışma sayılmaz.
- **R-3 Ürünün kategorisi.** Bir ürünün **sıfır ya da bir** kategorisi olur. Kategori, ürün
  oluşturulurken ve güncellenirken verilebilir; verilmezse ürün kategorisizdir. Güncellemede
  kategori değiştirilebilir ya da kaldırılabilir (kategorisiz yapılabilir). Güncelleme ürünün tüm
  alanlarını yeniden yazar: kategori gönderilmezse ürün kategorisiz olur.
- **R-4 Var olmayan kategori.** Ürüne var olmayan bir kategori verilirse istek "bulunamadı" ile
  reddedilir; ürün oluşmaz / değişmez.
- **R-5 Ürün okuma.** Ürün listesi ve tekil ürün yanıtı ürünün kategorisinin kimliğini ve adını
  içerir; kategorisiz üründe ikisi de boştur (null).
- **R-6 Kategori silme.** Kendisine bağlı en az bir ürünü olan kategori silinemez (iş kuralı
  ihlali); ürünler başka kategoriye taşınınca ya da kategorisiz yapılınca silinebilir.
- **R-10 Liste sırası.** Kategori listesinin sırası tanımsızdır; istemci sıraya güvenmez.
- **R-7 Yetki.** Kategori işlemleri ürünlerle aynı desende yetkilendirilir (BR-11):
  oluştur/güncelle/sil için `Category.Admin`, `Category.Write` ya da işlemin kendi yetkisi
  (`Category.Create` / `Category.Update` / `Category.Delete`); listeleme ve tekil okuma için
  `Category.Admin` ya da `Category.Read`. Global `Admin` her işlemden geçer. Ürüne kategori
  atamak ürünün mevcut oluştur/güncelle yetkisiyle yapılır; ek kategori yetkisi istenmez.
- **R-8 Mevcut veri.** Değişiklik devreye alındığında mevcut ürünlerin (seed ürün dahil) adı,
  fiyatı ve stoğu değişmez; hepsi kategorisiz olur.
- **R-9 Domain dili.** `docs/domain.md`'ye Category (Kategori) terimi ve kurallar eklenir:
  kategori adı benzersizdir (büyük/küçük harf duyarsız); bir ürünün en fazla bir kategorisi
  vardır; ürünü olan kategori silinemez.

## User experience
none — no user interface. Ekranlar (kategori yönetimi, ürün formunda kategori seçimi) ayrı bir
spec'in işidir.

## Constraints & out of scope
- Sensitive area: yok — mevcut yetki mekanizması (`ISecuredRequest`, operation claim'ler)
  değiştirilmeden yalnızca yeni isteklere uygulanır; Auth koduna dokunulmaz. Yetki kriterleri
  (AC-10) yine de var olduğundan one-pass review koşulu sağlanmaz: REVIEW ve VERIFY ayrı koşar.
- Proje kuralı: yeni Domain entity'sinin CRUD dilimi önce `uludag-gen crud Category` ile üretilir,
  sonra düzenlenir (`docs/conventions.md`, Code generation) — "nasıl" plana aittir.
- Kapsam dışı (V1): Angular arayüzü; kategoriye göre ürün filtreleme/arama; alt kategori ve
  hiyerarşi; çoklu kategori; kategori açıklaması, görseli, slug'ı, sıralaması; listede sayfalama;
  aynı adla **eşzamanlı** iki kategori oluşturma yarışı ve silme ↔ atama yarışı (B-19 ile aynı
  kök neden sınıfı; sıralı istekler kapsamda).
- Split: proposed 0009a "Kategori yönetimi" → 0009b "Ürüne kategori atama"; kept as one — Halit Enes
  Kalaycı (delegated), 2026-11-20, kategori olmadan atama test edilemez; CRUD dilimi üretilir.
- Onay: spec, clarify kararları (1–9) ve self-critique gap'leri (G-1…G-5) analistin önerileriyle
  kabul edildi — Halit Enes Kalaycı (delegated: "tüm süreçler önerinle, ship'e kadar"), 2026-11-20.

## Acceptance criteria
- [ ] AC-1 — `Category.Create` yetkili kullanıcı geçerli adla kategori oluşturur; kategori
  listede ve tekil okumada aynı adla (kırpılmış) görünür.
- [ ] AC-2 — Boş, yalnız boşluk ya da 100 karakterden uzun adla oluşturma ve ad değiştirme 400 ile
  reddedilir; tam 100 karakter kabul edilir. Reddedilen istekte hiçbir kategori oluşmaz/değişmez.
- [ ] AC-3 — Var olan bir kategori adıyla (büyük/küçük harf farkıyla dahil) oluşturma ya da başka
  kategorinin adına değiştirme 400 ile reddedilir; kategoriyi kendi adının farklı harf düzeniyle
  yeniden adlandırmak kabul edilir.
- [ ] AC-4 — Var olmayan kategori kimliğiyle tekil okuma, ad değiştirme ve silme 404 döner.
- [ ] AC-5 — Hiç ürünü olmayan kategori silinir; ardından listede görünmez ve tekil okuması 404.
- [ ] AC-6 — Ürünü olan kategoriyi silme 400 ile reddedilir ve kategori yerinde kalır; ürünü
  kategorisiz yapıldıktan sonra aynı silme başarılı olur.
- [ ] AC-7 — Kategoriyle oluşturulan ürünün liste ve tekil yanıtında kategori kimliği ve adı
  görünür; kategorisiz oluşturulan üründe ikisi null'dır. Kategori yeniden adlandırılınca ürün
  yanıtı yeni adı gösterir.
- [ ] AC-8 — Ürün güncellemesi kategorisini başka kategoriyle değiştirebilir ve kaldırabilir
  (null ya da alanı hiç göndermeden); her adımda okuma yanıtı yeni durumu gösterir.
- [ ] AC-9 — Var olmayan kategori kimliğiyle (tamamı sıfır olan kimlik dahil) ürün oluşturma 404 döner ve ürün oluşmaz; aynı
  kimlikle güncelleme 404 döner ve ürünün kategorisi değişmez.
- [ ] AC-10 — Yetki: token'sız kategori isteği 401; ilgili `Category.*` yetkisi olmayan kullanıcı
  için her kategori işlemi 403; R-7'deki yetkilerden herhangi biri (ve global `Admin`) yeterlidir.
  Kanıt: her işlem için yetkisiz → 403, kabul edilen yetkilerden bir örnek ve global `Admin`;
  tam kombinasyon matrisi gerekmez.
- [ ] AC-11 — Değişiklik sonrası mevcut ürünler (seed ürün dahil) aynı ad, fiyat ve stokla
  okunur ve kategorisizdir.

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
