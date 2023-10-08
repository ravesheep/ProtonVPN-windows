@echo off
for /f "delims=" %%a in ('git rev-parse --short HEAD') do @set hash=%%a

echo ^> Updating git submodules...
git submodule update --init

echo ^> Building native dependencies...
C:\nuget\nuget.exe restore ProtonVPN.InstallActions.sln
cmd.exe /c BuildDependencies.bat publish

echo ^> Downloading translations from crowdin...
python ci\build-scripts\main.py add-commit-hash %hash%
python ci\build-scripts\main.py defaultConfig

echo ^> Publishing release...
dotnet publish ProtonVpn.sln -c Release -r win-x64 --self-contained
msbuild src\ProtonVPN.NativeHost\NativeHost.vcxproj /p:Configuration=Release /p:Platform=x64
pause