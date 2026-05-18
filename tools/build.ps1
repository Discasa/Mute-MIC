$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$release = Join-Path $root 'release'
$payload = Join-Path $release 'payload'
$installerPayload = Join-Path $root 'src\MuteMIC.Installer\Payload'

if (Test-Path -LiteralPath $release) {
    $resolvedRelease = (Resolve-Path -LiteralPath $release).Path
    if (-not $resolvedRelease.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean unexpected release path: $resolvedRelease"
    }
    Remove-Item -LiteralPath $resolvedRelease -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $payload | Out-Null
if (Test-Path -LiteralPath $installerPayload) {
    Remove-Item -LiteralPath $installerPayload -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $installerPayload | Out-Null

$commonPublishArgs = @(
    '-c', 'Release',
    '-r', 'win-x64',
    '--self-contained', 'false',
    '-p:PublishSingleFile=true',
    '-p:EnableCompressionInSingleFile=true',
    '-p:DebugType=None',
    '-p:DebugSymbols=false'
)

dotnet publish (Join-Path $root 'src\MuteMIC.App\MuteMIC.App.csproj') @commonPublishArgs -o $payload
dotnet publish (Join-Path $root 'src\MuteMIC.Uninstaller\MuteMIC.Uninstaller.csproj') @commonPublishArgs -o $payload
Copy-Item -LiteralPath (Join-Path $payload 'Mute MIC.exe') -Destination $installerPayload -Force
Copy-Item -LiteralPath (Join-Path $payload 'Mute MIC Uninstaller.exe') -Destination $installerPayload -Force
dotnet publish (Join-Path $root 'src\MuteMIC.Installer\MuteMIC.Installer.csproj') @commonPublishArgs -o $release

$zipPath = Join-Path $release 'Mute-MIC-1.0.0-win-x64.zip'
$zipSource = Join-Path $release 'Mute MIC Installer.exe'
Compress-Archive -LiteralPath $zipSource -DestinationPath $zipPath -Force

Get-ChildItem -LiteralPath $release -Force
