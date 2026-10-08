#!/usr/bin/env bash
# Stores a Neon (or any Postgres) connection string in .NET user-secrets for local development.
#
# Paste the postgresql://… URL from Neon's "Connect" dialog when asked. It isn't echoed, isn't
# saved in shell history and never touches the repository. Both connection strings the API uses
# are set: the pooled host for queries and the direct host for migrations.
#
# Usage (from the repo root): ./server/scripts/set-database.sh
set -euo pipefail

cd "$(dirname "$0")/../Novashelf.Api"

read -r -s -p "Paste the connection string (postgresql://…): " url
echo

pattern='^postgres(ql)?://([^:/@]+):([^@]+)@([^/:?]+)(:([0-9]+))?/([^?]+)'
if [[ ! $url =~ $pattern ]]; then
  echo "That doesn't look like a postgresql:// connection string. Nothing was saved." >&2
  exit 1
fi

user=${BASH_REMATCH[2]}
password=${BASH_REMATCH[3]}
host=${BASH_REMATCH[4]}
port=${BASH_REMATCH[6]:-5432}
database=${BASH_REMATCH[7]}

# Neon's pooled host has "-pooler" after the endpoint id; the direct host doesn't.
direct_host=${host/-pooler/}
if [[ $host == *-pooler* ]]; then
  pooled_host=$host
else
  pooled_host=${host/./-pooler.}
fi

connection() {
  echo "Host=$1;Port=$port;Database=$database;Username=$user;Password=$password;SSL Mode=Require;Channel Binding=Require"
}

dotnet user-secrets set "ConnectionStrings:Default" "$(connection "$pooled_host")" > /dev/null
dotnet user-secrets set "ConnectionStrings:Migrations" "$(connection "$direct_host")" > /dev/null

echo "Saved. Queries use $pooled_host; migrations use $direct_host."
echo "Restart the API to use it (migrations are applied on startup)."
