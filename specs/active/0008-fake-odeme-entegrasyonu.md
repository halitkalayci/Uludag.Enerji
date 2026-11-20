# Spec 0008 — Fake ödeme entegrasyonu

- Status: Approved
- Mode: lite
- Plan: `specs/plans/0008-plan.md`
- Self-critique: done

## Intent
Proje sahibi, sipariş akışının gerçek bir e-ticarete benzemesi ve ödeme hatası senaryolarının
öğretilebilmesi için siparişe bir ödeme adımı eklenmesini istiyor. Gerçek bir ödeme sağlayıcısı
yok: her sipariş denemesinde sahte (fake) ödeme varsayılan olarak %25 olasılıkla reddedilir, %75
olasılıkla onaylanır. Başarı, ödeme onaylanınca siparişin bugünkü gibi oluşması; reddedilince hiçbir
kalıcı değişiklik olmaması (sipariş yok, stok aynı) ve kullanıcının anlaşılır bir mesajla aynı
sepeti tekrar deneyebilmesidir. Bilinçli olarak yapılmayan: gerçek ödeme sağlayıcısı, kart bilgisi
toplama, iade/iptal, ödeme kaydı veya ödeme durumu alanı, sipariş güncelleme/silme akışlarında ödeme.

## Kararlar (lite: clarify taslağa katlandı — insan onayı 2026-11-20, Halit Enes Kalaycı: tüm öneriler kabul)
- **K-1 Ret anında sipariş hiç oluşmaz.** Ödeme reddedilirse sipariş kaydedilmez ve stok düşmez —
  BR-4'teki "stok yetmezse sipariş hiç oluşmaz" ile aynı davranış. *Alternatif:* sipariş
  "Ödeme başarısız" durumuyla kaydedilir. *Neden değil:* yeni durum alanı, stok iadesi ve listeleme
  filtreleri gerektirir; kapsamı ikiye katlar ve "her sipariş ödenmiştir" değişmezini bozar.
- **K-2 Ödeme, doğrulamalardan sonra denenir.** Kalem/adet/ürün/para birimi/stok/oturum kontrollerinden
  biri başarısız olan istek hiç ödemeye gitmez ve bugünkü hatasını döner. *Gerekçe:* geçersiz bir
  sipariş için ödeme denemek gerçek dünyada para çekip iade etmek demektir.
- **K-3 Tutarı sunucu belirler.** Ödenen tutar ve para birimi, siparişin sunucuda hesaplanan toplamı
  ve para birimidir; istemci tutar gönderemez (BR-5 ile tutarlı).
- **K-4 Ret, kural ihlali gibi raporlanır.** Ret, diğer iş kuralı hataları gibi "400 + mesaj" döner;
  mesaj: **"Ödeme reddedildi. Lütfen tekrar deneyin."** *Alternatif:* 402 Payment Required.
  *Neden değil:* yeni hata tipi ve middleware eşlemesi gerektirir; istemci zaten 400 mesajını
  gösteriyor.
- **K-5 Ret oranı yapılandırılabilir.** Varsayılan 0,25. 0 her zaman onay, 1 her zaman ret demektir;
  testler ve demolar sonucu böylece sabitler. 0–1 aralığı dışındaki değerle uygulama açılmaz ve
  hatayı adıyla söyler. *Gerekçe:* rastgele sonuç başka türlü test edilemez (`docs/testing.md`:
  rastgelelik sabitlenir, üçüncü taraf servisler stub'lanır).
- **K-6 Her deneme bağımsızdır.** Bir ret sonraki denemeyi etkilemez; kullanıcı aynı sepeti hemen
  tekrar deneyebilir. Deneme sayısı sınırı yok.
- **K-7 Hassas alan değil.** Gerçek ödeme ve kart verisi yok (`docs/domain.md`: sipariş/stok/para
  hassas alan değil); Kimlik ve yetki koduna dokunulmaz.

## Requirements
- R-1 Bir sipariş oluşturma isteği tüm mevcut doğrulamalardan geçtikten sonra, siparişin toplam
  tutarı ve para birimi için bir ödeme denenir (K-2, K-3).
- R-2 Ödeme onaylanırsa sipariş bugünkü gibi oluşur: kalemleriyle kaydedilir, stok düşer.
- R-3 Ödeme reddedilirse sipariş oluşmaz, hiçbir ürünün stoğu değişmez ve kullanıcı K-4'teki mesajı
  alır (K-1).
- R-4 Ödemenin reddedilme olasılığı yapılandırmadan okunur; varsayılan 0,25 (K-5).
- R-5 Sipariş güncelleme, silme ve mevcut siparişe kalem ekleme/güncelleme/silme ödeme denemez.
- R-6 Sepet ekranında ödeme reddi kullanıcıya gösterilir; sepet korunur ve sipariş tekrar
  denenebilir (K-6).
- R-7 `docs/domain.md` ödeme terimi ve yeni iş kuralıyla (BR-12) güncellenir.

## User experience
- Flow: Kullanıcı sepete gider → bir para birimi grubu için "Sipariş ver"e basar → (a) ödeme
  onaylanırsa bugünkü başarı akışı (başarı bildirimi, o grup sepetten çıkar, sepet boşaldıysa
  siparişlerim sayfası); (b) reddedilirse sepet ekranında hata mesajı görünür, sepet olduğu gibi
  kalır, kullanıcı aynı butonla tekrar dener.
- States: sipariş veriliyor (buton meşgul — mevcut), başarı (mevcut), ödeme reddi (yeni: mesaj
  "Ödeme reddedildi. Lütfen tekrar deneyin.", sepet dolu, buton tekrar etkin), diğer hatalar (mevcut).
- Design reference: none — the flow above is the design (mevcut sepet ekranının hata alanı kullanılır).
- Accessibility: hata mesajı mevcut hata alanında görünür; buton klavye ile tekrar erişilebilir —
  mevcut ekranın davranışı korunur.

## Constraints & out of scope
- Sensitive area: yok (K-7).
- Out of scope: gerçek ödeme sağlayıcısı; kart/ödeme bilgisi toplama; iade, iptal, kısmi ödeme;
  ödeme kaydı, işlem numarası, sipariş üzerinde ödeme durumu alanı; ödeme denemelerinin
  raporlanması/loglanması; deneme sayısı sınırı; ödeme onaylandıktan sonra kaydın teknik bir
  nedenle başarısız olması (gerçek para yok, telafi gerekmez).
- Architecture: `docs/architecture.md` "Gerçek ödeme entegrasyonları" kapsam dışı — bu spec gerçek
  değil fake ödeme getirir; kapsam dışı maddesi değişmez.

## Acceptance criteria
- [ ] AC-1 — Ret oranı 0 iken geçerli bir sipariş isteği başarılı döner; sipariş kullanıcının
  siparişlerinde kalemleriyle görünür ve her ürünün stoğu kalem adedi kadar düşer.
- [ ] AC-2 — Ret oranı 1 iken geçerli bir sipariş isteği 400 ve "Ödeme reddedildi. Lütfen tekrar
  deneyin." mesajıyla döner; kullanıcının sipariş sayısı değişmez ve hiçbir ürünün stoğu değişmez.
- [ ] AC-3 — Ödemeye giden tutar ve para birimi, siparişin sunucuda hesaplanan toplamı ve para
  birimiyle aynıdır (çok kalemli, aynı üründen birden fazla kalemli bir siparişle doğrulanır).
- [ ] AC-4 — Doğrulamadan geçemeyen istekler (kalemsiz, adet ≤ 0, olmayan ürün, yetersiz stok,
  para birimi uyuşmazlığı) bugünkü hatalarını döner ve hiç ödeme denenmez — ret oranı 1 iken de
  ödeme mesajı değil, kendi mesajları gelir.
- [ ] AC-5 — Ret oranının varsayılanı 0,25'tir: ödeme bileşeni varsayılan ayarla 10.000 denemede
  %23 ile %27 arasında ret üretir.
- [ ] AC-6 — Ret oranı 0–1 aralığı dışında (ör. -0,1 veya 1,5) yapılandırılırsa uygulama açılmaz
  ve hata ayarın adını söyler.
- [ ] AC-7 — Sipariş güncelleme, silme ve kalem ekleme/güncelleme/silme ret oranı 1 iken de bugünkü
  gibi çalışır (ödeme denenmez).
- [ ] AC-8 — Sepet ekranı: ödeme reddedilince hata mesajı görünür, sepetin içeriği değişmez,
  "Sipariş ver" butonu tekrar etkin olur; aynı sepetle tekrar deneme onaylanırsa bugünkü başarı
  akışı çalışır.
- [ ] AC-9 — `docs/domain.md` ödeme terimini ve BR-12'yi (ödeme reddedilen sipariş oluşmaz, stok
  değişmez; tutarı sunucu belirler) içerir.

## Self-critique (öneriler onaylandı ve katlandı — 2026-11-20)
- **G-1 Ret oranı 0,25 iken tek bir HTTP isteğinin sonucu öngörülemez.** HTTP testleri oranı 0 veya 1
  yapılandırarak sabitler; istatistiksel kontrol (AC-5) yalnız ödeme bileşeni üzerinde, HTTP'siz
  yapılır. *Öneri:* böyle kalsın — 10.000 HTTP isteği hem yavaş hem stok tüketir.
- **G-2 AC-5'in sınırları.** 10.000 denemede beklenen sapma ≈ %0,43; %23–%27 aralığı ≈ 4,6 sapmadır,
  testin rastgele kırmızı olma olasılığı ihmal edilebilir. *Öneri:* aralık böyle kalsın; plan sabit
  bir rastgele tohum kullanmaya karar verirse aralık yine geçerlidir.
- **G-3 Eşzamanlı iki sipariş aynı son stoğu isterse.** Bu spec stok yarışını değiştirmez; bugünkü
  davranış korunur. *Öneri:* kapsam dışı (stok eşzamanlılığı ayrı bir konu, ödeme onu değiştirmez).
- **G-4 Ödeme onaylandı ama sipariş kaydı teknik nedenle düştü.** Gerçek para yok, telafi yok.
  *Öneri:* out of scope'ta yazılı kalsın (yazıldı).
- **G-5 Ret oranı ortama göre farklı mı olmalı?** Geliştirme/demo ortamında varsayılan 0,25 kalır;
  testler kendi değerini verir. *Öneri:* ortam bazlı farklı varsayılan yok.
- **G-6 Mesaj dili.** Hata mesajı mevcut yerelleştirme düzeninde Türkçe ve İngilizce verilir
  (Order locale dosyaları). *Öneri:* İngilizce karşılığı "Payment was declined. Please try again."

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
