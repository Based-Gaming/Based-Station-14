#!/bin/bash

if [ -z "$1" ]; then
	VER="1.2"
else
	VER="$1"
fi

cd "$(dirname "$0")"

rm -rf "PUBLISH/based_v${VER}.zip"
mkdir -p "PUBLISH/Mods"

dotnet publish sideload/Based.csproj -c Release --no-self-contained -r win-x64 /nologo

cp "./bin/Release/net8.0/win-x64/publish/Based.dll"  "PUBLISH/Mods"
cd "PUBLISH/"
zip -r "based_v${VER}.zip" "Mods"
rm -rf Mods

