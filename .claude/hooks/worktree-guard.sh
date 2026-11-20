#!/bin/sh
# PreToolUse hook (spec 0006, CB-2): the main working tree is nobody's workspace — every session
# that changes the repo works in its own worktree (docs/git.md, Worktrees).
# Blocks (exit 2): a file tool (Edit/Write/MultiEdit/NotebookEdit) writing under the main tree, and a
# Bash git command that changes branch/index/commits there. Read-only git, `git fetch` and
# `git worktree add` stay free. Known limit: plain shell writes (sed -i, >) are not detected.
input=$(cat)

# first JSON string value of key $1, unescaped enough for paths and commands
field() {
  printf '%s' "$input" | grep -oE "\"$1\"[[:space:]]*:[[:space:]]*\"([^\"\\\\]|\\\\.)*\"" | head -n 1 \
    | sed -E "s/^\"$1\"[[:space:]]*:[[:space:]]*\"//; s/\"\$//; s/\\\\\"/\"/g; s/\\\\\\\\/\\\\/g"
}

# comparable form of a path: forward slashes, Windows drive form, no trailing slash; on Windows
# (cygpath present) case-insensitive
WIN=0; command -v cygpath >/dev/null 2>&1 && WIN=1
norm() {
  p=$(printf '%s' "$1" | tr '\\' '/')
  if [ $WIN = 1 ]; then
    p=$(cygpath -m "$p" 2>/dev/null || printf '%s' "$p")
    p=$(printf '%s' "$p" | tr '[:upper:]' '[:lower:]')
  fi
  printf '%s' "$p" | sed 's#/*$##'
}

# resolve directory $2 relative to directory $1 (existing dirs are canonicalized)
resolve() {
  q=$(printf '%s' "$2" | sed "s/^[\"']//; s/[\"']\$//" | tr '\\' '/')
  case "$q" in
    /*|[A-Za-z]:*) p=$q ;;
    "~"|"~/"*) p="$HOME${q#\~}" ;;
    *) p="$1/$q" ;;
  esac
  r=$(cd "$p" 2>/dev/null && pwd) && p=$r
  norm "$p"
}

cwd=$(field cwd)
[ -n "$cwd" ] || cwd=${CLAUDE_PROJECT_DIR:-.}
common=$(git -C "$cwd" rev-parse --path-format=absolute --git-common-dir 2>/dev/null) \
  || common=$(git -C "${CLAUDE_PROJECT_DIR:-.}" rev-parse --path-format=absolute --git-common-dir 2>/dev/null) \
  || exit 0
case "$common" in */.git) ;; *) exit 0 ;; esac
MAIN=$(norm "${common%/.git}")

inside() { case "$1/" in "$MAIN"/*) return 0 ;; esac; return 1; }

block() {
  echo "Blocked: $1 in the main working tree (${common%/.git}). Every session works in its own worktree (docs/git.md, Worktrees): run 'scripts/worktree new <branch>' and work under the directory it prints." >&2
  exit 2
}

# --- file tools ---
path=$(field file_path); [ -n "$path" ] || path=$(field notebook_path)
if [ -n "$path" ]; then
  case "$(printf '%s' "$path" | tr '\\' '/')" in
    /*|[A-Za-z]:*) p=$(norm "$path") ;;
    *) p=$(resolve "$cwd" "$(dirname -- "$path")")/$(basename -- "$path") ;;
  esac
  inside "$p" && block "writing $path"
  exit 0
fi

# --- Bash: follow `cd` through the chain, honor `git -C`, judge the git subcommand ---
cmd=$(field command)
[ -n "$cmd" ] || exit 0
dir=$(resolve "$cwd" .)
segments=$(printf '%s\n' "$cmd" | sed 's/&&/\n/g; s/||/\n/g; s/;/\n/g; s/|/\n/g')
set -f
while IFS= read -r seg; do
  set -- $seg
  [ $# -gt 0 ] || continue
  if [ "$1" = cd ]; then
    [ $# -ge 2 ] && dir=$(resolve "$dir" "$2")
    continue
  fi
  while [ $# -gt 0 ] && [ "${1##*/}" != git ] && [ "${1##*/}" != git.exe ]; do shift; done
  [ $# -gt 0 ] || continue
  shift
  d=$dir; sub=
  while [ $# -gt 0 ]; do
    case "$1" in
      -C) [ $# -ge 2 ] || break; d=$(resolve "$dir" "$2"); shift 2 ;;
      -c) shift; [ $# -gt 0 ] && shift ;;
      -*) shift ;;
      *) sub=$1; break ;;
    esac
  done
  case "$sub" in
    checkout|switch|commit|merge|pull|stash|reset|restore|add) inside "$d" && block "git $sub" ;;
  esac
done <<EOF
$segments
EOF
exit 0
