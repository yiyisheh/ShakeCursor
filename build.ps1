# Builds ShakeCursor.exe and ShakeCursor.msi into .\dist (needs the .NET SDK; WiX comes from NuGet).
param([switch]$NoInstaller)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null

dotnet build (Join-Path $root 'src\ShakeCursor.csproj') -c Release -nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Building ShakeCursor.exe failed' }
Copy-Item (Join-Path $root 'src\bin\Release\net48\ShakeCursor.exe') $dist -Force

if (-not $NoInstaller) {
    dotnet build (Join-Path $root 'installer\ShakeCursor.Installer.wixproj') -c Release -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'Building ShakeCursor.msi failed' }
    Copy-Item (Join-Path $root 'installer\bin\Release\ShakeCursor.msi') $dist -Force
}

Get-ChildItem $dist | Format-Table Name, Length -AutoSize
