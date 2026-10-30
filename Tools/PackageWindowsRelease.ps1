[CmdletBinding()]
param(
    [string]$Version = '1.0.0',
    [string]$BuildDirectory = 'Build/WindowsRelease',
    [string]$OutputDirectory = 'Build/Distribution/Windows'
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Use a numeric major.minor.patch version.' }
$buildRoot = (Resolve-Path -LiteralPath (Join-Path $projectRoot $BuildDirectory)).Path
$outputRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory))
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Build')) + [IO.Path]::DirectorySeparatorChar
if (-not $outputRoot.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Package output must stay inside Build/.'
}
$buildInfo = Get-Content -LiteralPath (Join-Path $buildRoot 'build-info.json') -Raw | ConvertFrom-Json
if ($buildInfo.result -ne 'Succeeded' -or $buildInfo.options -ne 'None' -or $buildInfo.errors -ne 0) {
    throw 'A successful WindowsReleaseBuilder build with no Development/debug flags is required.'
}
foreach ($required in @('SquidInkPulse.exe', 'UnityPlayer.dll', 'SquidInkPulse_Data')) {
    if (-not (Test-Path -LiteralPath (Join-Path $buildRoot $required))) { throw "Missing $required" }
}

$packageName = "SquidInkPulse-Windows-x64-v$Version"
$payload = Join-Path $outputRoot $packageName
$archive = Join-Path $outputRoot "$packageName.zip"
if ((Test-Path -LiteralPath $payload) -or (Test-Path -LiteralPath $archive)) {
    throw 'The versioned package already exists; preserve it before repeating.'
}
New-Item -ItemType Directory -Path $payload -Force | Out-Null
# Keep the complete runtime. Fair/reset helpers and debug symbols are not player payload.
Get-ChildItem -LiteralPath $buildRoot -Recurse -File | Where-Object {
    $_.FullName -notlike '*DoNotShip*' -and $_.Extension -notin @('.pdb', '.mdb') -and
    $_.Name -notlike 'REINICIAR_DATOS_JUEGO.*' -and $_.Name -ne 'README_SERVIDOR_FERIA.txt'
} | ForEach-Object {
    $relative = $_.FullName.Substring($buildRoot.Length).TrimStart('\', '/')
    $destination = Join-Path $payload $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
    Copy-Item -LiteralPath $_.FullName -Destination $destination
}

$launcher = @'
@echo off
cd /d "%~dp0"
start "" "%~dp0SquidInkPulse.exe" --fair-disabled
'@
[IO.File]::WriteAllText((Join-Path $payload 'JUGAR.cmd'), $launcher.Replace("`n", "`r`n"), [Text.ASCIIEncoding]::new())
$guide = @'
SQUID INK-PULSE - Yeco Works

Extrae la carpeta completa y abre JUGAR.cmd. No requiere Unity ni servidor de feria.
Conserva juntos el exe, SquidInkPulse_Data, DLL y carpetas runtime.

Mouse: dirigir al jugador. Click/Espacio: Ink-Pulse. P/Escape: pausa.
Q/W: gadgets. B: comprar en la tienda de run. Menus/comics: mouse.
El menu incluye Como Jugar.

El progreso se guarda fuera del ZIP en:
%USERPROFILE%/AppData/LocalLow/Yeco Works/Squid Ink-Pulse/db
Cambiar la carpeta del juego no reinicia tus datos. Respalda los datos antes
de volver a una version anterior. Este paquete no incluye datos del desarrollador.

Creditos: Yeco Works, Chile.
Pablo Guzman: SCRUM Master. Rodrigo Cortes: Product Owner.
Mauricio Munoz: Gameplay Programmer. Matias Palacios: diseno visual y sonoro.
Inti Santibanez: soporte de entrega.

Menciones especiales:
Matias Palacios: artista y compositor principal, autor y dueno de los
soundtracks y sprites principales. Su reutilizacion requiere su permiso.
Rodrigo Cortes: por su aporte a la base mecanica del juego.
Mauricio Munoz: por ser un desarrollador confiable durante el proyecto.
La licencia general de reutilizacion sigue pendiente del acuerdo del equipo.

Fuente, notas, checksums y otras plataformas:
https://github.com/NicoCG32/Squid-Ink-Pulse-Public-Version/releases
'@
[IO.File]::WriteAllText((Join-Path $payload 'LEEME.txt'), $guide, [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSING.md') -Destination (Join-Path $payload 'LICENSING.md')
Copy-Item -LiteralPath (Join-Path $projectRoot 'branding/squid-banner-claro.png') -Destination (Join-Path $payload 'IDENTIDAD.png')
$source = (& git -C $projectRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify source commit.' }
[ordered]@{ sourceCommit=$source; unity=$buildInfo.unity; target=$buildInfo.target;
    development=$false; allowDebugging=$false; version=$Version; fairDisabledByLauncher=$true
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $payload 'source-info.json') -Encoding utf8

Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($payload, $archive, [IO.Compression.CompressionLevel]::Optimal, $true)
if ((Get-Item -LiteralPath $archive).Length -ge 2GB) { throw 'GitHub release assets must be smaller than 2 GiB.' }
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $outputRoot 'SHA256SUMS.txt'), "$hash  $packageName.zip`n", [Text.ASCIIEncoding]::new())
Write-Output "$archive`nSHA256=$hash"
