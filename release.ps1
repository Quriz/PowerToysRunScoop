# Script for copying the release files and creating the release zip file

$version = "1.1.0"
$release = "$env:USERPROFILE\Downloads\powertoys-run-scoop-$version.zip"
$path = (dotnet msbuild $PSScriptRoot\Community.PowerToys.Run.Plugin.Scoop.csproj /p:Platform=x64 /p:Configuration=Release /t:GetSourcePath -nologo).Trim()
$path = "$PSScriptRoot\$path"

# Pack the files from path and exclude PowerToys*, Backup*, Ijwhost*
7z a -aoa -bb0 -bso0 -xr!PowerToys* -xr!Backup* -xr!Ijwhost* -tzip $release $path
