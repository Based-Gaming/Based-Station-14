#!/bin/bash

if [ -z "$1" ]; then
	VER="1.2"
else
	VER="$1"
fi

cd "$(dirname "$0")"

rm -rf PUBLISH/
mkdir PUBLISH

TYPE="Release"
#TYPE="Debug"

dotnet publish sideload/Based.csproj -c $TYPE --no-self-contained -r win-x64 /nologo

# update zips
#declare -a builds=("Windows" "Linux" "macOS")
declare -a builds=("Windows")

## now loop through the above array
for build in "${builds[@]}"
do
	./BasedMarseyLoader/publish_${build}.sh $TYPE
	mv "BasedMarseyLoader/BasedMarsey.Launcher_${build}.zip" "PUBLISH/"
    rm -rf "PUBLISH/BasedMarsey.Launcher_${build}_v${VER}.zip"
	mkdir -p "Marsey/Mods"
	cp "./bin/$TYPE/net8.0/win-x64/publish/Based.Patch.dll" "Marsey/Mods"
	python3  cryptor.py "./bin/$TYPE/net8.0/win-x64/publish/Based.dll" "Marsey/Mods/Based.dll"
	zip -ur "PUBLISH/BasedMarsey.Launcher_${build}.zip" "Marsey/"
	mv "PUBLISH/BasedMarsey.Launcher_${build}.zip" "PUBLISH/BasedMarsey.Launcher_${build}_v${VER}.zip"
	rm -rf "Marsey"
done