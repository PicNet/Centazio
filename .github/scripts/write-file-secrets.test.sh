#!/usr/bin/env bash
# Regression cases for write-file-secrets.sh, run in CI without any real secrets.
set -uo pipefail

script="$(dirname "$0")/write-file-secrets.sh"
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
failures=0

fail() { echo "FAIL: $1"; failures=$((failures + 1)); }

expect_ok() {
  local name="$1" input="$2" masks="$3" env="$4"
  # jq on windows writes CRLF, strip CR so the cases also run locally
  local out; out=$(printf '%s' "$input" | bash "$script" "$tmp/$name.env" | tr -d '\r') || { fail "$name exited non-zero"; return; }
  local written; written=$(tr -d '\r' < "$tmp/$name.env")
  [ "$out" == "$masks" ] || fail "$name masks [$out] expected [$masks]"
  [ "$written" == "$env" ] || fail "$name env [$written] expected [$env]"
}

expect_rejected() {
  local name="$1" input="$2"
  local out; out=$(printf '%s' "$input" | bash "$script" "$tmp/$name.env" 2> /dev/null) && fail "$name was accepted"
  [ ! -e "$tmp/$name.env" ] || fail "$name wrote a file"
  [[ "$out" != *add-mask* ]] || fail "$name registered masks"
}

expect_ok plain '{"A":"plain","B":"x=y=z"}' $'::add-mask::plain\n::add-mask::x=y=z' $'A=plain\nB=x=y=z'
expect_ok percent '{"A":"50%off"}' '::add-mask::50%25off' 'A=50%off'
expect_ok empty '{"A":""}' '' 'A='
expect_rejected multiline '{"A":"line1\nline2"}'
expect_rejected carriage '{"A":"a\rb"}'
expect_rejected number '{"A":5}'
expect_rejected array '["a"]'
expect_rejected invalid 'nope'

[ "$failures" -eq 0 ] && echo "write-file-secrets: all cases passed"
exit "$failures"
