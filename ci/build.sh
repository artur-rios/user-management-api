#!/bin/sh
# The `build` check: restore and build src/ArturRios.UserManagement.sln in Release (every project, the `common`
# submodule's included), then run the unit tests CI can run: Domain, which uses mocks only and hashes nothing.
#
# Left out:
#   - the functional tests (tests/Application: WebApi.Tests, WebApi.Client.Tests, Data.Client.Tests). They talk to
#     a PostgreSQL database whose connection string comes from .env files or the environment, and fail at once
#     without one ("Database connection string is not configured"); CI has no such database.
#   - Command.Tests. Its handlers hash passwords with Argon2id at ArturRios.Common.Util's defaults (600 MB and 16
#     lanes per hash) while xUnit runs the test classes in parallel: the test host grows past 14 GB and is killed
#     ("Test host process crashed"). It passes only where that much memory is free.
#   - Query.Tests, for the same reason: its mocks hash passwords too (Hash.NewFromText), and it peaks near 11 GB,
#     more than the CI agents' shared memory can give one check next to the others. Both come back once the tests
#     hash with a cheap test-only setting, as heimdall-api's do (HEIMDALL_INSECURE_TEST_PASSWORD_HASHING).
#
# yggdrasil.yml runs it in the pinned .NET 8 SDK image from the repository's root, after ci/common.sh has put the
# submodule in place. By hand, from a clone with its git metadata:
#   docker run --rm -v "$PWD:/src" -w /src <image> sh ci/build.sh
# or, with the .NET 8 SDK installed, just `sh ci/build.sh`.
set -eu

# The repository's root, wherever this is run from.
cd "$(dirname "$0")/.."

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

sh ci/common.sh

solution=src/ArturRios.UserManagement.sln
dotnet restore "$solution"
dotnet build "$solution" --no-restore --configuration Release

dotnet test tests/Core/ArturRios.UserManagement.Domain.Tests --no-build --configuration Release
