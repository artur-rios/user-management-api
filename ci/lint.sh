#!/bin/sh
# The `lint` check: `dotnet format --verify-no-changes` over src/ArturRios.UserManagement.sln, which fails when a
# whitespace, code style or analyzer fix would change a file, as .editorconfig says. The solution includes the
# `common` submodule's projects, which are another repository's code: common/ is excluded, so only this
# repository's files are checked. The same command without --verify-no-changes applies the fixes.
#
# yggdrasil.yml runs it in the pinned .NET 8 SDK image from the repository's root, after ci/common.sh has put the
# submodule in place (the projects cannot load without it). By hand, from a clone with its git metadata:
#   docker run --rm -v "$PWD:/src" -w /src <image> sh ci/lint.sh
# or, with the .NET 8 SDK installed, just `sh ci/lint.sh`.
set -eu

# The repository's root, wherever this is run from.
cd "$(dirname "$0")/.."

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

sh ci/common.sh
dotnet format src/ArturRios.UserManagement.sln --verify-no-changes --exclude common --verbosity minimal
