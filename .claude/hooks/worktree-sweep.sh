#!/bin/sh
# SessionStart hook (spec 0006, K-4): clean every worktree whose branch is verifiably merged and
# clean; report the rest. This session's own worktree is never removed. Never blocks the session.
dir=${CLAUDE_PROJECT_DIR:-.}
[ -f "$dir/scripts/worktree" ] || exit 0
if command -v timeout >/dev/null 2>&1; then t="timeout 120"; else t=; fi
out=$(cd "$dir" && $t sh scripts/worktree sweep 2>&1)
[ -n "$out" ] && printf 'Worktree sweep (scripts/worktree sweep — docs/git.md, Worktrees):\n%s\n' "$out"
exit 0
