# Spec 0006 — Worktree zorunluluğu ve merge sonrası temizlik (mini)

- Status: In progress
- Mode: lite
- Plan: `specs/plans/0006-plan.md`
- Self-critique: done
- Source: REQ-2026-11-20-worktree — kullanıcı talebi, 2026-11-20 oturumu ("KARAKTERİZASYON: her oturum yeni worktree")
- Design: — (kullanıcı arayüzü yok)
- Supersedes: — (bu davranışı tanımlayan shipped spec yok)

## Intent
Kullanıcı bu projede birden fazla AI oturumunu paralel çalıştırıyor; ortak çalışma ağacını paylaşan
oturumlar birbirinin branch'ini değiştirip dosyalarını sessizce geri alabiliyor (2026-11-20'de
yaşandı). Bundan sonra paralel oturum olsun olmasın, repoyu değiştiren her oturum kendi, anlamlı
isimli worktree'sinde çalışır ve ana çalışma ağacına dokunmaz. Worktree'nin branch'i main'e merge
edildiğinde — merge kullanıcının beyanıyla değil, git kanıtıyla doğrulandıktan sonra — worktree'nin
bütün kalıntıları temizlenir. Bilinçli olarak yapılmayan: merge'ü otomatikleştirmek (merge yine
insanındır), mevcut worktree'leri yeniden adlandırmak.

## Kararlar (lite: clarify taslağa katlandı — insan onayı 2026-11-20)
- **K-1 Kapsam.** Kural, repoda dosya ya da git durumu (branch, commit, stash, index) değiştiren her
  oturuma uygulanır. Yalnız okuyan oturumlar (soru-cevap, read-only `reviewer` alt ajanı) muaftır.
  *Gerekçe:* okuma çakışma üretmez; reviewer zaten builder'ın worktree'sini okur.
- **K-2 Squash-merge kanıtı.** PR'lar squash-merge edildiği için (`docs/git.md`) branch ucu
  `origin/main`'in atası olmaz; tek başına `git merge-base --is-ancestor` merge'ü yanlış "olmamış"
  gösterir. Merge kanıtı: `git fetch --prune` sonrası **ya** branch ucu `origin/main`'in atasıdır,
  **ya da** branch'in GitHub PR'ı `MERGED` durumundadır, PR'ın son commit'i branch'in yerel ucuyla
  aynıdır ve PR'ın merge commit'i `origin/main`'de bulunur.
- **K-3 İsimlendirme.** Worktree dizini repo'nun kardeşidir ve adı branch adından türetilir:
  `../Uludag.Enerji-<branch, "/" → "-">` (ör. `feature/0006-worktree-politikasi` →
  `../Uludag.Enerji-feature-0006-worktree-politikasi`). Branch adları `docs/git.md`'deki kurallara
  uyduğu için dizin adı işi tek bakışta söyler ve çakışmaz.
- **K-4 Tetikleyici.** Temizlik SHIP'in son adımıdır: insan merge ettikten sonra oturum merge'ü
  K-2 ile doğrular ve temizler. Ayrıca her oturum başında mevcut worktree'ler taranır; merge'ü
  doğrulanmış ve temiz olanlar temizlenir, diğerleri yalnız raporlanır.
- **K-5 Zorlama.** Kural yalnız metin olarak kalmaz: Claude Code'da ana çalışma ağacına dosya
  yazma ve ana ağacın branch'ini değiştirme engellenir. Diğer AI araçları kuralı `AGENTS.md`
  üzerinden okur (teknik engel yalnız Claude Code için).
- **K-6 Kuralın yeri.** Kuralın tamamı `docs/git.md`'de yaşar; `AGENTS.md` ona işaret eder ve
  SHIP adımı temizliği adım olarak içerir (tek doğruluk kaynağı ilkesi).

## Changed behavior
- [ ] CB-1 — Repoyu değiştiren her oturum (K-1), paralel oturum olmasa bile, işe kendi worktree'sinde
  başlar: yeni iş için güncel `origin/main`'den yeni branch + worktree; sürdürülen iş için o branch'in
  mevcut worktree'si (yoksa yenisi). Ana çalışma ağacında dosya düzenlenmez, commit atılmaz, branch
  değiştirilmez.
- [ ] CB-2 — Claude Code'da bir ajan (a) dosya düzenleme araçlarıyla (Edit, Write, NotebookEdit) ana
  çalışma ağacındaki bir dosyaya yazmaya, ya da (b) ana çalışma ağacında branch/index/commit
  değiştiren bir git komutu (`checkout`, `switch`, `commit`, `merge`, `pull`, `stash`, `reset`,
  `restore`, `add`) çalıştırmaya kalkarsa işlem engellenir ve mesaj worktree açmayı söyler. Aynı
  işlemler bir worktree içinde engellenmez; ana ağaçta `fetch`, `worktree add`, okuma komutları
  serbesttir. Genel shell yazmaları (`sed -i`, `>`) teknik olarak yakalanmaz — belgelenmiş sınır,
  kuralın metni yine geçerlidir.
- [ ] CB-3 — Yeni açılan worktree'nin dizin adı K-3 kuralına uyar; branch upstream'i `origin/main`'e
  bağlı kalmaz (yanlışlıkla main'e push riski olmaz).
- [ ] CB-4 — Merge durumu yalnız K-2 kanıtıyla kabul edilir. Kullanıcı "merge ettim" dese de kanıt
  yoksa branch merge edilmemiş sayılır, temizlik yapılmaz ve hangi kontrolün başarısız olduğu
  raporlanır.
- [ ] CB-5 — Merge doğrulandığında: worktree dizini diskten silinir, `git worktree list`'te kaydı
  kalmaz, local branch ve remote branch silinir (remote zaten silinmişse hata değildir), bayat
  remote-tracking ref'ler budanır. Temizlik sonunda bu dört kalıntının yokluğu kanıt olarak
  raporlanır. Bir adım başarısız olursa (ör. Windows'ta dizin başka bir süreç tarafından
  kullanılıyor) hata gizlenmez: kalan kalıntı adıyla raporlanır, sonraki tarama yeniden dener.
- [ ] CB-6 — Merge doğrulanmış olsa bile worktree'de commit edilmemiş değişiklik, izlenmeyen
  (ignore edilmemiş) dosya ya da push edilmemiş commit varsa hiçbir şey silinmez; durum raporlanır
  ve karar insana bırakılır.

## Preserved behavior
- [ ] PB-1 — `docs/git.md`'deki branch adlandırma, commit, PR ve squash-merge kuralları ile "merge'ü
  insan yapar" ilkesi değişmez; hiçbir oturum PR merge etmez.
- [ ] PB-2 — Merge'ü K-2 ile doğrulanmamış hiçbir worktree, branch ya da dizin silinmez; mevcut
  `Uludag.Enerji-auth`, `Uludag.Enerji-client` worktree'leri ve diğer oturumların branch'leri
  dokunulmadan kalır.
- [ ] PB-3 — `scripts/check` ve `scripts/doctor --strict` yeşil kalır; `protect-shipped` hook'u
  `specs/done/` düzenlemelerini engellemeye devam eder.

## Out of scope
- Merge'ün otomatikleştirilmesi ya da bir ajanın merge etmesi.
- Mevcut, eski adlı worktree'lerin (`-auth`, `-client`) yeniden adlandırılması — branch'leri merge
  edildiğinde CB-5 ile temizlenirler.
- Ana çalışma ağacının hangi branch'te duracağı: ana ağaç kimsenin çalışma alanı değildir, ideal
  olarak `main`'de durur; ama bu zorlanmaz ve şu anki branch'i değiştirilmez (başka oturum kullanıyor).
- Hook'un geçiş dönemi: hook ana ağaçta, ana ağaç bu değişikliği içeren bir branch'e geçtiğinde
  devreye girer; o zamana kadar kural `AGENTS.md`/`docs/git.md` metni ve ajan belleğiyle uygulanır.
  Kullanıcı seviyesi (proje dışı) ayar kullanılmaz.
- İnsanın kendi elle yaptığı işler; Claude Code dışındaki araçlar için teknik engel (K-5).
- CI tarafında worktree denetimi (worktree yereldir; CI'da anlamı yok).

## Definition of Done
- [ ] `scripts/check` green
- [ ] Independent review done; real findings fixed, noise rejected with written rationale, deferred ones in `specs/backlog.md`
- [ ] Criterion ↔ evidence table complete for CB-* **and** PB-* (UI: before/after screenshots, agent-captured, judged at SHIP)
- [ ] Spec moved to `specs/done/` (immutable there)
