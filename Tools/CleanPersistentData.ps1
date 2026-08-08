[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [switch]$NoBackup,
    [switch]$IncludePlayerPrefs,
    [string]$CompanyName = "Yeco Works",
    [string]$ProductName = "Squid Ink-Pulse"
)

$ErrorActionPreference = "Stop"

function Get-NormalizedFullPath {
    param([Parameter(Mandatory = $true)][string]$Path)
    return [System.IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
}

function Assert-PathWithin {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$AllowedRoot,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $fullPath = Get-NormalizedFullPath -Path $Path
    $fullRoot = Get-NormalizedFullPath -Path $AllowedRoot
    $prefix = $fullRoot + [System.IO.Path]::DirectorySeparatorChar

    if ($fullPath -ne $fullRoot -and -not $fullPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label apunta fuera del directorio permitido. Path='$fullPath'. Root='$fullRoot'."
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$seedDbPath = Join-Path $repoRoot "Assets\Implementation\Resources\PersistentDbSeeds"

if (-not (Test-Path -LiteralPath $seedDbPath -PathType Container)) {
    throw "No existe la base semilla: $seedDbPath"
}

$localLowRoot = Join-Path $env:USERPROFILE "AppData\LocalLow"
$persistentRoot = Join-Path (Join-Path $localLowRoot $CompanyName) $ProductName
$runtimeDbPath = Join-Path $persistentRoot "db"
$legacyProfilePath = Join-Path $persistentRoot "player-profile.json"
$backupRoot = Join-Path $persistentRoot "_backups"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupPath = Join-Path $backupRoot "clean-$timestamp"

Assert-PathWithin -Path $persistentRoot -AllowedRoot $localLowRoot -Label "persistentRoot"
Assert-PathWithin -Path $runtimeDbPath -AllowedRoot $persistentRoot -Label "runtimeDbPath"
Assert-PathWithin -Path $legacyProfilePath -AllowedRoot $persistentRoot -Label "legacyProfilePath"
Assert-PathWithin -Path $backupRoot -AllowedRoot $persistentRoot -Label "backupRoot"
Assert-PathWithin -Path $backupPath -AllowedRoot $backupRoot -Label "backupPath"

Write-Host "Limpiando progreso persistido de Squid Ink-Pulse"
Write-Host "PersistentDataPath: $persistentRoot"
Write-Host "Semillas: $seedDbPath"

$hasRuntimeDb = Test-Path -LiteralPath $runtimeDbPath
$hasLegacyProfile = Test-Path -LiteralPath $legacyProfilePath

if (-not $NoBackup -and ($hasRuntimeDb -or $hasLegacyProfile)) {
    if ($PSCmdlet.ShouldProcess($backupPath, "Crear respaldo de persistencia actual")) {
        New-Item -ItemType Directory -Force -Path $backupPath | Out-Null
    }

    if ($hasRuntimeDb -and $PSCmdlet.ShouldProcess($runtimeDbPath, "Respaldar db")) {
        Copy-Item -LiteralPath $runtimeDbPath -Destination (Join-Path $backupPath "db") -Recurse -Force
    }

    if ($hasLegacyProfile -and $PSCmdlet.ShouldProcess($legacyProfilePath, "Respaldar player-profile.json legacy")) {
        Copy-Item -LiteralPath $legacyProfilePath -Destination (Join-Path $backupPath "player-profile.legacy.json") -Force
    }
}

if ($hasRuntimeDb -and $PSCmdlet.ShouldProcess($runtimeDbPath, "Borrar db persistida")) {
    Remove-Item -LiteralPath $runtimeDbPath -Recurse -Force
}

if ($hasLegacyProfile -and $PSCmdlet.ShouldProcess($legacyProfilePath, "Borrar player-profile.json legacy")) {
    Remove-Item -LiteralPath $legacyProfilePath -Force
}

if ($PSCmdlet.ShouldProcess($runtimeDbPath, "Recrear db persistida desde semillas limpias")) {
    New-Item -ItemType Directory -Force -Path $runtimeDbPath | Out-Null
    Get-ChildItem -LiteralPath $seedDbPath -Filter "*.json" -File |
        Copy-Item -Destination $runtimeDbPath -Force
}

if ($IncludePlayerPrefs) {
    $playerPrefsKey = "HKCU:\Software\$CompanyName\$ProductName"
    if (Test-Path -LiteralPath $playerPrefsKey) {
        if ($PSCmdlet.ShouldProcess($playerPrefsKey, "Borrar PlayerPrefs del juego")) {
            Remove-Item -LiteralPath $playerPrefsKey -Recurse -Force
        }
    }
}

Write-Host ""
if ($WhatIfPreference) {
    Write-Host "Simulacion terminada. No se modificaron archivos."
} else {
    Write-Host "Limpieza terminada."
}
Write-Host "- Mejoras permanentes: nivel 0."
Write-Host "- Camarones: 0."
Write-Host "- Best score/runs/leaderboard local: limpio."
Write-Host "- Skins: solo skin.default desbloqueada y equipada."

if (-not $WhatIfPreference -and -not $NoBackup -and ($hasRuntimeDb -or $hasLegacyProfile)) {
    Write-Host "- Respaldo: $backupPath"
}

if (-not $IncludePlayerPrefs) {
    Write-Host "- PlayerPrefs no fueron borrados. Usa -IncludePlayerPrefs si tambien quieres limpiar opciones/URL de feria."
}
