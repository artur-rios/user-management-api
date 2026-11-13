#!/bin/sh
# Puts the `common` submodule in place when it is missing: Jenkins checks out no submodules, and every project of
# the solution references common/src/... (ArturRios.Common.*), so nothing builds or formats without it. The
# submodule is the public GitHub repository .gitmodules names (artur-rios/common, now dotnet-common), fetched
# anonymously at the very commit this checkout pins, exactly as `git submodule update --init` does on your machine.
# A common/ that already has files (a local clone with its submodules) is left as it is.
#
# ci/lint.sh and ci/build.sh run it first. It needs the checkout's git metadata (Jenkins' checkout has it) and git
# (the .NET SDK image has it). By hand, from the repository's root: sh ci/common.sh
set -eu

# The repository's root, wherever this is run from.
cd "$(dirname "$0")/.."

if [ -n "$(ls -A common 2>/dev/null)" ]; then
  echo "ci/common.sh: common/ is already there"
  exit 0
fi

# The check runs as root in a checkout another user may own: trust it and the submodule for these commands only.
git -c safe.directory="$PWD" -c safe.directory="$PWD/common" submodule update --init common
