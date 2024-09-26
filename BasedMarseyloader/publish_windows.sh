#!/bin/bash

cd "$(dirname "$0")"

./download_net_runtime.py windows

# Clear out previous build.
rm -rf **/bin bin/publish/Windows
rm -f BasedMarsey.Launcher_Windows.zip

#TYPE="Release"
if [ "$1" = "Debug" ]; then
	TYPE="Debug"
else
	TYPE="Release"
fi

dotnet publish SS14.Launcher/SS14.Launcher.csproj /p:FullRelease=True -c $TYPE --no-self-contained -r win-x64 /nologo /p:RobustILLink=true
dotnet publish SS14.Launcher/SS14.Launcher.csproj /p:FullRelease=True -c $TYPE --no-self-contained -r win-x64 /nologo /p:RobustILLink=true
dotnet publish SS14.Loader/SS14.Loader.csproj -c $TYPE --no-self-contained -r win-x64 /nologo
dotnet publish SS14.Launcher.Strap/SS14.Launcher.Strap.csproj -c $TYPE /nologo

./exe_set_subsystem.py "SS14.Launcher/bin/$TYPE/net8.0/win-x64/publish/SS14.Launcher.exe" 2
./exe_set_subsystem.py "SS14.Loader/bin/$TYPE/net8.0/win-x64/publish/SS14.Loader.exe" 2

# Create intermediate directories.
mkdir -p bin/publish/Windows/bin
mkdir -p bin/publish/Windows/bin/loader
mkdir -p bin/publish/Windows/dotnet
mkdir -p bin/publish/Windows/Marsey/Mods
mkdir -p bin/publish/Windows/Marsey/ResourcePacks

cp -r Dependencies/dotnet/windows/* bin/publish/Windows/dotnet
cp "SS14.Launcher.Strap/bin/$TYPE/net45/publish/Marseyloader.exe" bin/publish/Windows/BasedMarseyloader.exe
cp "SS14.Launcher.Strap/console.bat" bin/publish/Windows
cp SS14.Launcher/bin/$TYPE/net8.0/win-x64/publish/* bin/publish/Windows/bin
rm bin/publish/Windows/bin/*.pdb
cp SS14.Loader/bin/$TYPE/net8.0/win-x64/publish/* bin/publish/Windows/bin/loader
rm bin/publish/Windows/bin/loader/*.pdb

pushd bin/publish/Windows
zip -r ../../../BasedMarsey.Launcher_Windows.zip *

popd
