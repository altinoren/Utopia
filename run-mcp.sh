#!/bin/sh
set -eu

root=$(CDPATH= cd "$(dirname "$0")" && pwd)
cd "$root"
dotnet build src/Server/Utopia.csproj --configuration Debug --verbosity quiet 1>&2
exec dotnet src/Server/bin/Debug/net11.0/Utopia.dll
