#!/bin/bash
RHOST="guru@pwnd.top"
RPATH="/var/www/bs14.pwnd.top"

if [ -z "$1" ]; then
	VER="0.0"
else
	VER="$1"
fi

cd "$(dirname "$0")"

mkdir -p "PUBLISH"
declare -a builds=("win" "linux")

ssh "${RHOST}" "rm ${RPATH}/bs14_*"
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
    scp "${output}" "${RHOST}:${RPATH}/downloads/${output}"
    popd
done
scp "README.md" "${RHOST}:${RPATH}/README.md"
