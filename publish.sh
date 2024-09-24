#!/bin/bash

if [ -z "$1" ]; then
	VER="1.2"
else
	VER="$1"
fi

cd "$(dirname "$0")"

rm -rf PUBLISH/
mkdir PUBLISH

dotnet publish patches/Based.Patch.csproj -c Release --no-self-contained -r win-x64 /nologo
dotnet publish sideload/Based.Sideload.csproj -c Release --no-self-contained -r win-x64 /nologo

# update zips
#declare -a builds=("Windows" "Linux" "macOS")
declare -a builds=("Windows")

## now loop through the above array
for build in "${builds[@]}"
do
	./BasedMarseyLoader/publish_${build}.sh
	mv "BasedMarseyLoader/BasedMarsey.Launcher_${build}.zip" "PUBLISH/"
    rm -rf "PUBLISH/BasedMarsey.Launcher_${build}_v${VER}.zip"
	mkdir -p "Marsey/Mods"
	cp "./bin/Release/net8.0/win-x64/publish/Based.Patch.dll" "Marsey/Mods"
	#python3  cryptor.py "./bin/Release/net8.0/win-x64/publish/Based.Patch.dll" "Marsey/Mods/Based.Patch.dll"
	cp "./bin/Release/net8.0/win-x64/publish/Based.Sideload.dll" "Marsey/Mods"
	#python3  cryptor.py "./bin/Release/net8.0/win-x64/publish/Based.Sideload.dll" "Marsey/Mods/Based.Sideload.dll"
	zip -ur "PUBLISH/BasedMarsey.Launcher_${build}.zip" "Marsey/"
	mv "PUBLISH/BasedMarsey.Launcher_${build}.zip" "PUBLISH/BasedMarsey.Launcher_${build}_v${VER}.zip"
	rm -rf "Marsey"
done