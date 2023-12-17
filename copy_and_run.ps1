# Copies the latest build to PowerToys installation and restarts PowerToys

# Source and destination paths
$sourcePath = (dotnet msbuild $PSScriptRoot\Community.PowerToys.Run.Plugin.Scoop.csproj /p:Platform=x64 /p:Configuration=Release /t:GetSourcePath -nologo).Trim()
$sourcePath = "$PSScriptRoot\$sourcePath"
$destinationPath = "$env:LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Plugins\Scoop"

# Check if Plugins folder exists
if (-not (Test-Path -Path "$destinationPath\.." -PathType Container)) {
    Write-Host "Plugins folder does not exist. Exiting."
    Exit
}

# Prompt for admin rights
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Start-Process powershell -Verb RunAs -ArgumentList ("-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"")
    Exit
}

# Close PowerToys if it's running
$powerToysProcess = Get-Process -Name "PowerToys"
if ($powerToysProcess -ne $null) {
    Stop-Process -Name "PowerToys" -Force
    Write-Host "PowerToys process terminated."
    Start-Sleep -Seconds 2
}

# Check if the source folder exists
if (-not (Test-Path -Path $sourcePath -PathType Container)) {
    Write-Host "Source folder does not exist."
    Exit
}

# Create the destination folder if it doesn't exist
if (-not (Test-Path -Path $destinationPath -PathType Container)) {
    New-Item -Path $destinationPath -ItemType Directory -Force
}

# Copy the contents of the source folder to the destination folder
Copy-Item -Path $sourcePath\* -Destination $destinationPath -Recurse -Force -ErrorAction Stop

Write-Host "Folder successfully copied from $sourcePath to $destinationPath"

# Start PowerToys program
Start-Process "C:\Program Files\PowerToys\PowerToys.exe"