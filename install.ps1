param(
    [ValidateSet('Debug', 'Release')] [string] $Configuration = 'Release',
    [switch] $Uninstall
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'FibonaxUvcAscom.csproj'
$dll = Join-Path $PSScriptRoot "bin\\$Configuration\\net481\\FibonaxUvcAscom.dll"
$regasm = Join-Path $env:WINDIR 'Microsoft.NET\\Framework64\\v4.0.30319\\RegAsm.exe'

if (-not (Test-Path $regasm)) { throw "64-bit .NET Framework RegAsm was not found: $regasm" }
if ($Uninstall) {
    if (Test-Path $dll) { & $regasm $dll /unregister; if ($LASTEXITCODE) { throw 'Unregistration failed.' } }
    exit 0
}

dotnet build $project -c $Configuration -p:PlatformTarget=x64
if ($LASTEXITCODE) { throw 'Build failed.' }
& $regasm $dll /codebase
if ($LASTEXITCODE) { throw 'COM registration failed.' }
Write-Host 'Installed ASCOM.OpenUvcColor.Camera. Configure its UVC camera index through the ASCOM chooser Setup button.'
