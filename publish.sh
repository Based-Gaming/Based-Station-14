#!/bin/bash

if [ -z "$1" ]; then
	VER="0.0"
else
	VER="$1"
fi

cd "$(dirname "$0")"

mkdir -p "PUBLISH"
declare -a builds=("win" "linux")

for build in "${builds[@]}"
do
    dotnet publish sideload/Based.csproj -c Release --no-self-contained -r $build-x64 /nologo

    output="bs14_${build}_v${VER}.zip"
    pushd "PUBLISH/"
    rm -rf "${output}"
    mkdir -p "Mods"
    cp "../bin/Release/net8.0/$build-x64/publish/Based.dll"  "Mods"
    zip -r "${output}" "Mods"
    rm -rf Mods
    scp "${output}" "guru@pwnd.top:/var/www/bs14.pwnd.top/${output}"
    popd
done
