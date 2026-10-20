#!/usr/bin/env bash
# Reads a Secrets Manager JSON object on stdin, registers every value as a GitHub Actions mask and writes the
# values as KEY=VALUE lines to the file given as $1 (the format read by FileSecretsLoader).
set -euo pipefail

out="$1"
json=$(cat)

# the .env format is line based and GitHub masks are registered per line, so multi-line values cannot be handled safely
if ! jq -e 'type == "object" and all(.[]; type == "string" and (test("[\r\n]") | not))' <<< "$json" > /dev/null; then
  echo "::error::secret values must be single-line strings"
  exit 1
fi

# workflow command data must escape %, CR and LF (see actions/toolkit command.ts escapeData)
jq -r '.[] | select(length > 0) | "::add-mask::" + (gsub("%"; "%25") | gsub("\r"; "%0D") | gsub("\n"; "%0A"))' <<< "$json"
jq -r 'to_entries[] | "\(.key)=\(.value)"' <<< "$json" > "$out"
