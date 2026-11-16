#!/bin/sh
# The `build` check: restore and build src/ArturRios.UserManagement.sln in Release (every project, the `common`
# submodule's included), then run the unit tests: Domain, Command and Query, which use mocks only.
#
# The three test projects run side by side, each in a process of its own, after the one build; each run's output is
# printed in one piece once all have finished, and the check fails if any of them failed. Only the tests whose
# handler hashes a password with Argon2id at its production settings (600 MB and 16 lanes a hash) pay for it: the
# mocks store random bytes unless a test asks for a real hash, and the hashing classes of Command.Tests run one at a
# time (its PasswordHashingCollection).
#
# Left out: the functional tests (tests/Application: WebApi.Tests, WebApi.Client.Tests, Data.Client.Tests). They talk
# to a PostgreSQL database whose connection string comes from .env files or the environment, and fail at once
# without one ("Database connection string is not configured"); CI has no such database.
#
# yggdrasil.yml runs it in the pinned .NET 10 SDK image from the repository's root, after ci/common.sh has put the
# submodule in place. By hand, from a clone with its git metadata:
#   docker run --rm -v "$PWD:/src" -w /src <image> sh ci/build.sh
# or, with the .NET 10 SDK installed, just `sh ci/build.sh`.
set -eu

# The repository's root, wherever this is run from.
cd "$(dirname "$0")/.."

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

sh ci/common.sh

solution=src/ArturRios.UserManagement.sln
dotnet restore "$solution"
dotnet build "$solution" --no-restore --configuration Release

logs=$(mktemp -d)
trap 'rm -rf "$logs"' EXIT

pids=""
for project in Domain Command Query; do
  dotnet test "tests/Core/ArturRios.UserManagement.$project.Tests" --no-build --configuration Release \
    >"$logs/$project.log" 2>&1 &
  pids="$pids $!:$project"
done

failed=0
for entry in $pids; do
  if wait "${entry%%:*}"; then status=passed; else status=FAILED; failed=1; fi
  echo "== ${entry#*:}.Tests: $status"
  cat "$logs/${entry#*:}.log"
done
exit "$failed"
