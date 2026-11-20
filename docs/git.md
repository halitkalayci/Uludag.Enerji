# Git

> Bootstrap 2026-11-19: ANEW varsayılanları olduğu gibi kabul edildi. Remote: GitHub (`origin`),
> CI: `.github/workflows/check.yml`.

## Branching
- `feature/<spec-no>-<short-name>` — **no branch without a spec.** Also for CHANGE-lane mini-specs.
- Refactors: `refactor/<spec-no>-<short-name>` (mini-spec with Changed behavior = none).
- Fixes: `fix/<report-id>-<short-name>` (bug fixes carry a report, not a spec number);
  incidents: `incident/<date>-<short-name>`; trivial changes: `trivial/<short-name>` (see below).
- Bootstrap: `chore/bootstrap` — the bootstrap output (`workflows/bootstrap.md`), merged like
  any branch.

## Worktrees
<!-- Spec 0006. Kullanıcı oturumları paralel çalıştırır; ortak çalışma ağacı bir oturumun dosyalarını
     başka bir oturumun branch değişikliğiyle sessizce geri aldırır (2026-11-20'de yaşandı). -->
- **Her oturum kendi worktree'sinde.** Repoda dosya ya da git durumu (branch, index, commit, stash)
  değiştiren her oturum — paralel oturum olmasa bile — kendi worktree'sinde çalışır. Yalnız okuyan
  oturumlar (soru-cevap, read-only `reviewer` alt ajanı) muaftır. **Ana çalışma ağacı**
  (`Uludag.Enerji/`) kimsenin çalışma alanı değildir: orada dosya düzenlenmez, commit atılmaz, branch
  değiştirilmez.
- **Açmak / sürdürmek:** `scripts/worktree new <branch>` — yeni iş için güncel `origin/main`'den
  branch + worktree açar; branch'in worktree'si varsa onu verir (sürdürme); branch varsa bağlar.
  Branch adı yukarıdaki Branching kurallarına uyar. Sonra yalnız yazdırdığı dizinde çalışılır; ilk iş
  orada `npm ci --prefix ECommerce/client` (`scripts/check`'in client-build adımı ister).
- **İsim:** `../Uludag.Enerji-<branch, "/" → "-">` — ana ağacın kardeşi; ör. `feature/0006-x` →
  `../Uludag.Enerji-feature-0006-x`. Branch'in upstream'i `origin/main` olmaz (yanlışlıkla main'e
  push edilmez). Elle açılırsa: **mutlak yol** (`git -C <ana ağaç> worktree add <göreli>` hedefi ana
  ağaca göre çözer ve içine açar).
- **Merge kanıtı — kullanıcı beyanı kanıt değildir:** `scripts/worktree merged <branch>`. PR'lar
  squash-merge edildiği için tek başına `git merge-base --is-ancestor` merge'ü "olmamış" gösterir.
  Kanıt, `git fetch --prune` sonrası **ya** branch ucunun `origin/main`'e merge edilmiş olması (ucu
  main'in first-parent çizgisinde olan, kendine ait commit'i olmayan branch sayılmaz), **ya da**
  branch'in PR'ının `MERGED` olması + PR head'inin yerel uca eşit olması + PR merge commit'inin
  `origin/main`'de olmasıdır (`gh pr view <branch> --json state,headRefOid,mergeCommit`). Her kontrol
  `ok`/`FAIL` satırıyla raporlanır; `gh` yoksa kanıt yoktur.
- **Temizlik:** `scripts/worktree clean <branch>` — merge kanıtlanınca worktree dizini,
  `git worktree list` kaydı, local ve remote branch silinir, bayat ref'ler budanır; dördünün durumu `absent` /
  `REMAINS` satırıyla raporlanır. Commit edilmemiş değişiklik, izlenmeyen dosya ya da push edilmemiş
  commit varsa **hiçbir şey silinmez** (ignore edilen `node_modules`, `bin/`, `obj/` iş sayılmaz).
  Silinemeyen kalıntı (ör. Windows'ta kullanımdaki dizin) adıyla raporlanır.
- **Ne zaman:** SHIP'in son adımı — insan merge eder, oturum `clean` ile doğrular ve temizler. Ayrıca
  her Claude Code oturumu açılışta `scripts/worktree sweep` çalıştırır: merge'ü kanıtlanmış ve temiz
  worktree'ler temizlenir, diğerleri yalnız raporlanır; oturumun kendi worktree'si silinmez.
- **Zorlama (Claude Code):** `.claude/hooks/worktree-guard.sh` ana ağaca Edit/Write/NotebookEdit ile
  yazmayı ve ana ağaçta `checkout|switch|commit|merge|pull|stash|reset|restore|add` git komutlarını
  engeller; `git fetch`, `git worktree add` ve okuma komutları serbesttir. **Sınır:** düz shell
  yazmaları (`sed -i`, `>`) yakalanmaz — kural yine geçerlidir. Diğer AI araçları kuralı bu bölümden
  okur.

## Commits
- Conventional Commits, with a plan reference: `feat(catalog): paging endpoint [plan 0001/3]`.
  Review fixes name their fix round instead of the step:
  `fix(catalog): empty page size [plan 0001/fix-2]` — the reviewer counts rounds from these
  (R-06 after round 3).
- Agent commits follow the same standard: the agent writes the message, the human approves it
  through the gate before the commit, not one by one — an approved plan covers its
  `[plan NNNN/n]` and `[plan NNNN/fix-k]` commits, the ship approval the ship commit. Every
  message stays visible in the branch and the PR. Lite and strict alike.
- Only `./scripts/check`'s own exit code gates a commit or a push. Never pipe its output into
  another command in the chain that decides it: in `./scripts/check | grep … && git commit` the
  chain takes grep's exit code, so a red check commits. Keep the output aside and gate on check
  alone: `./scripts/check > check.log 2>&1 && git commit …` (then show `check.log`).
- Commit named paths only: `git commit -- <paths>`, never `git add -A`, `git add .` or
  `commit -a`. Another session or tool may have written or staged files in the same tree.

## Forbidden
- Direct commits to the default branch.
- Force push, history rewriting on shared branches. Undo = `git revert` (see recovery R-11).

## Trivial changes
<!-- Applies only to requests the change-request triage rubric
     (workflows/change-request.md) classifies as TRIVIAL: no acceptance criterion changes, no shared code. -->
- Policy: **(1) PR + one reviewer, no spec** — seçildi (bootstrap 2026-11-19). Branch: `trivial/<short-name>`.
- Always: one narrow commit, `scripts/check` green. A second file or a new test means re-triage.

## Pull requests
- PR template checklist completed; `scripts/check` green in CI; squash-merge.
