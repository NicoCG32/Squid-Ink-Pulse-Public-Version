using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class FairBuildReadmePostprocessor : IPostprocessBuildWithReport
{
    private const string ReadmeFileName = "README_CLIENTE_FERIA.txt";
    private const string ResetDataScriptFileName = "REINICIAR_DATOS_JUEGO.ps1";
    private const string ResetDataBatchFileName = "REINICIAR_DATOS_JUEGO.bat";
    private const string HostIpPlaceholder = "<IP_DEL_HOST>";
    private const string ClientMachinePlaceholder = "<NOMBRE_PC_CLIENTE>";

    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report == null)
        {
            return;
        }

        string buildDirectory = ResolveBuildDirectory(report.summary.outputPath);
        if (string.IsNullOrWhiteSpace(buildDirectory))
        {
            Debug.LogWarning("[FairBuildReadmePostprocessor] No se pudo resolver la carpeta del build.");
            return;
        }

        Directory.CreateDirectory(buildDirectory);
        string executableName = ResolveExecutableName(report.summary.outputPath);
        string readmePath = Path.Combine(buildDirectory, ReadmeFileName);
        string resetScriptPath = Path.Combine(buildDirectory, ResetDataScriptFileName);
        string resetBatchPath = Path.Combine(buildDirectory, ResetDataBatchFileName);

        File.WriteAllText(readmePath, BuildReadmeText(executableName), Encoding.UTF8);
        File.WriteAllText(resetScriptPath, BuildResetScriptText(executableName), Encoding.UTF8);
        File.WriteAllText(resetBatchPath, BuildResetBatchText(), Encoding.ASCII);

        Debug.Log($"[FairBuildReadmePostprocessor] README de cliente generado: {readmePath}");
        Debug.Log($"[FairBuildReadmePostprocessor] Script de reinicio generado: {resetBatchPath}");
    }

    private static string ResolveBuildDirectory(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return null;
        }

        string fullPath = Path.GetFullPath(outputPath);
        if (Directory.Exists(fullPath))
        {
            return fullPath;
        }

        string directory = Path.GetDirectoryName(fullPath);
        return string.IsNullOrWhiteSpace(directory) ? null : directory;
    }

    private static string ResolveExecutableName(string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return "Squid Ink Pulse.exe";
        }

        string fileName = Path.GetFileName(outputPath);
        return string.IsNullOrWhiteSpace(fileName) ? "Squid Ink Pulse.exe" : fileName;
    }

    private static string BuildReadmeText(string executableName)
    {
        StringBuilder builder = new();
        builder.AppendLine("SQUID INK PULSE - CONFIGURACION CLIENTE FERIA");
        builder.AppendLine("================================================");
        builder.AppendLine();
        builder.AppendLine("Este archivo se genera automaticamente junto al build de Unity.");
        builder.AppendLine("Debe quedar en la misma carpeta que el ejecutable del juego.");
        builder.AppendLine("Tambien se generan scripts para reiniciar datos locales del cliente:");
        builder.AppendLine($"- {ResetDataBatchFileName}");
        builder.AppendLine($"- {ResetDataScriptFileName}");
        builder.AppendLine();
        builder.AppendLine("1. Descomprimir el ZIP completo");
        builder.AppendLine("--------------------------------");
        builder.AppendLine("- No mover solo el .exe.");
        builder.AppendLine("- El .exe debe quedar junto a su carpeta *_Data, UnityPlayer.dll y demas archivos del build.");
        builder.AppendLine();
        builder.AppendLine("2. Confirmar la IP del servidor host");
        builder.AppendLine("------------------------------------");
        builder.AppendLine("En el PC que ejecuta el servidor:");
        builder.AppendLine("- Abrir PowerShell.");
        builder.AppendLine("- Ejecutar: ipconfig");
        builder.AppendLine("- Buscar la direccion IPv4 del adaptador Wi-Fi o Ethernet activo.");
        builder.AppendLine();
        builder.AppendLine("Ejemplo de IP:");
        builder.AppendLine("192.168.1.50");
        builder.AppendLine();
        builder.AppendLine("En las instrucciones de abajo, reemplazar:");
        builder.AppendLine($"{HostIpPlaceholder} por la IPv4 real del host.");
        builder.AppendLine($"{ClientMachinePlaceholder} por el nombre de este PC cliente, por ejemplo PC-02.");
        builder.AppendLine();
        builder.AppendLine("3. Probar conexion desde el cliente");
        builder.AppendLine("-----------------------------------");
        builder.AppendLine("En este PC cliente, abrir navegador y entrar a:");
        builder.AppendLine($"http://{HostIpPlaceholder}:8080/health");
        builder.AppendLine();
        builder.AppendLine("Si responde texto JSON con ok=true, el servidor es visible desde este PC.");
        builder.AppendLine("Si no responde, revisar IP, firewall, servidor apagado o red distinta.");
        builder.AppendLine();
        builder.AppendLine("4. Crear acceso directo del juego");
        builder.AppendLine("---------------------------------");
        builder.AppendLine("- Click derecho sobre el ejecutable del juego.");
        builder.AppendLine("- Crear acceso directo.");
        builder.AppendLine("- Click derecho sobre el acceso directo > Propiedades.");
        builder.AppendLine("- En el campo Destino, usar este formato:");
        builder.AppendLine();
        builder.AppendLine($"\"C:\\RUTA\\DEL\\JUEGO\\{executableName}\" --fair-server=http://{HostIpPlaceholder}:8080 --fair-machine={ClientMachinePlaceholder}");
        builder.AppendLine();
        builder.AppendLine("Ejemplo real:");
        builder.AppendLine();
        builder.AppendLine($"\"C:\\SquidFeria\\Game\\{executableName}\" --fair-server=http://192.168.1.50:8080 --fair-machine=PC-02");
        builder.AppendLine();
        builder.AppendLine("Reglas importantes:");
        builder.AppendLine("- Las comillas van solo alrededor de la ruta del .exe.");
        builder.AppendLine("- Los argumentos van fuera de las comillas.");
        builder.AppendLine("- Debe haber un espacio entre la comilla final y --fair-server.");
        builder.AppendLine("- La URL no debe terminar en /health.");
        builder.AppendLine();
        builder.AppendLine("Correcto:");
        builder.AppendLine($"--fair-server=http://{HostIpPlaceholder}:8080");
        builder.AppendLine();
        builder.AppendLine("Incorrecto:");
        builder.AppendLine($"--fair-server=http://{HostIpPlaceholder}:8080/health");
        builder.AppendLine();
        builder.AppendLine("5. Entrar al juego");
        builder.AppendLine("------------------");
        builder.AppendLine("- Abrir el juego desde el acceso directo configurado.");
        builder.AppendLine("- Debe aparecer la interfaz de feria.");
        builder.AppendLine("- Para jugador nuevo: escribir Nick y presionar Nuevo jugador.");
        builder.AppendLine("- Para jugador existente: escribir Nick + Codigo y presionar Entrar.");
        builder.AppendLine();
        builder.AppendLine("6. Empezar de cero en este cliente");
        builder.AppendLine("----------------------------------");
        builder.AppendLine("Si este PC ya jugo antes y se quiere borrar progreso local:");
        builder.AppendLine($"- Cerrar el juego.");
        builder.AppendLine($"- Ejecutar {ResetDataBatchFileName}.");
        builder.AppendLine("- Confirmar escribiendo SI cuando el script lo pida.");
        builder.AppendLine("- Volver a abrir el juego.");
        builder.AppendLine();
        builder.AppendLine("Este reinicio limpia mejoras, skins compradas, camarones, best score, runs y leaderboard local.");
        builder.AppendLine("No borra opciones ni URL de feria guardada, salvo que se ejecute el .ps1 con -IncludePlayerPrefs.");
        builder.AppendLine();
        builder.AppendLine("Comando avanzado:");
        builder.AppendLine();
        builder.AppendLine($"powershell -NoProfile -ExecutionPolicy Bypass -File .\\{ResetDataScriptFileName} -Force");
        builder.AppendLine();
        builder.AppendLine("7. Problemas frecuentes");
        builder.AppendLine("-----------------------");
        builder.AppendLine("- Si /health funciona pero no aparece la interfaz, confirmar que se abrio el acceso directo y no el .exe directo.");
        builder.AppendLine("- Si aun asi se necesita abrir el login, usar el area invisible BotonLog en el MainMenu como respaldo.");
        builder.AppendLine("- Si Windows dice que el destino no es valido, revisar las comillas del campo Destino.");
        builder.AppendLine("- Si el servidor no responde, confirmar que el host tiene abierto Tools/FairServer/start_fair_server.");
        builder.AppendLine("- Si varios PCs usan el mismo jugador a la vez, el servidor puede bloquear la sesion duplicada.");
        builder.AppendLine();
        builder.AppendLine("Comando alternativo desde PowerShell en la carpeta del juego:");
        builder.AppendLine();
        builder.AppendLine($"& \".\\{executableName}\" --fair-server=http://{HostIpPlaceholder}:8080 --fair-machine={ClientMachinePlaceholder}");
        builder.AppendLine();
        return builder.ToString();
    }

    private static string BuildResetBatchText()
    {
        StringBuilder builder = new();
        builder.AppendLine("@echo off");
        builder.AppendLine("setlocal");
        builder.AppendLine($"powershell -NoProfile -ExecutionPolicy Bypass -File \"%~dp0{ResetDataScriptFileName}\" %*");
        builder.AppendLine("set EXIT_CODE=%ERRORLEVEL%");
        builder.AppendLine("echo.");
        builder.AppendLine("if not \"%EXIT_CODE%\"==\"0\" (");
        builder.AppendLine("  echo No se pudo reiniciar la persistencia local. Revisa el mensaje anterior.");
        builder.AppendLine(") else (");
        builder.AppendLine("  echo Persistencia local reiniciada.");
        builder.AppendLine(")");
        builder.AppendLine("pause");
        builder.AppendLine("exit /b %EXIT_CODE%");
        return builder.ToString();
    }

    private static string BuildResetScriptText(string executableName)
    {
        string companyName = string.IsNullOrWhiteSpace(PlayerSettings.companyName)
            ? "DefaultCompany"
            : PlayerSettings.companyName;
        string productName = string.IsNullOrWhiteSpace(PlayerSettings.productName)
            ? "Squid Ink-Pulse"
            : PlayerSettings.productName;
        string dataDirectoryName = $"{Path.GetFileNameWithoutExtension(executableName)}_Data";

        StringBuilder builder = new();
        builder.AppendLine("[CmdletBinding(SupportsShouldProcess = $true)]");
        builder.AppendLine("param(");
        builder.AppendLine("    [switch]$Force,");
        builder.AppendLine("    [switch]$NoBackup,");
        builder.AppendLine("    [switch]$IncludePlayerPrefs");
        builder.AppendLine(")");
        builder.AppendLine();
        builder.AppendLine("$ErrorActionPreference = \"Stop\"");
        builder.AppendLine($"$CompanyName = '{EscapePowerShellSingleQuotedString(companyName)}'");
        builder.AppendLine($"$ProductName = '{EscapePowerShellSingleQuotedString(productName)}'");
        builder.AppendLine($"$ExpectedDataDirectoryName = '{EscapePowerShellSingleQuotedString(dataDirectoryName)}'");
        builder.AppendLine();
        builder.AppendLine("function Get-NormalizedFullPath {");
        builder.AppendLine("    param([Parameter(Mandatory = $true)][string]$Path)");
        builder.AppendLine("    return [System.IO.Path]::GetFullPath($Path).TrimEnd('\\', '/')");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("function Assert-PathWithin {");
        builder.AppendLine("    param(");
        builder.AppendLine("        [Parameter(Mandatory = $true)][string]$Path,");
        builder.AppendLine("        [Parameter(Mandatory = $true)][string]$AllowedRoot,");
        builder.AppendLine("        [Parameter(Mandatory = $true)][string]$Label");
        builder.AppendLine("    )");
        builder.AppendLine();
        builder.AppendLine("    $fullPath = Get-NormalizedFullPath -Path $Path");
        builder.AppendLine("    $fullRoot = Get-NormalizedFullPath -Path $AllowedRoot");
        builder.AppendLine("    $prefix = $fullRoot + [System.IO.Path]::DirectorySeparatorChar");
        builder.AppendLine();
        builder.AppendLine("    if ($fullPath -ne $fullRoot -and -not $fullPath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {");
        builder.AppendLine("        throw \"$Label apunta fuera del directorio permitido. Path='$fullPath'. Root='$fullRoot'.\"");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("function Resolve-SeedDbPath {");
        builder.AppendLine("    param([Parameter(Mandatory = $true)][string]$ScriptRoot)");
        builder.AppendLine();
        builder.AppendLine("    $expected = Join-Path $ScriptRoot (Join-Path $ExpectedDataDirectoryName \"StreamingAssets\\db\")");
        builder.AppendLine("    if (Test-Path -LiteralPath $expected -PathType Container) {");
        builder.AppendLine("        return $expected");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    $candidates = @(");
        builder.AppendLine("        Get-ChildItem -LiteralPath $ScriptRoot -Directory -Filter \"*_Data\" -ErrorAction SilentlyContinue |");
        builder.AppendLine("            ForEach-Object { Join-Path $_.FullName \"StreamingAssets\\db\" } |");
        builder.AppendLine("            Where-Object { Test-Path -LiteralPath $_ -PathType Container }");
        builder.AppendLine("    )");
        builder.AppendLine();
        builder.AppendLine("    if ($candidates.Count -eq 1) {");
        builder.AppendLine("        return $candidates[0]");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    throw \"No se encontro StreamingAssets\\db dentro del build. Ejecuta este script desde la carpeta del .exe y no muevas los archivos del build.\"");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path");
        builder.AppendLine("$seedDbPath = Resolve-SeedDbPath -ScriptRoot $scriptRoot");
        builder.AppendLine("$localLowRoot = Join-Path $env:USERPROFILE \"AppData\\LocalLow\"");
        builder.AppendLine("$persistentRoot = Join-Path (Join-Path $localLowRoot $CompanyName) $ProductName");
        builder.AppendLine("$runtimeDbPath = Join-Path $persistentRoot \"db\"");
        builder.AppendLine("$legacyProfilePath = Join-Path $persistentRoot \"player-profile.json\"");
        builder.AppendLine("$backupRoot = Join-Path $persistentRoot \"_backups\"");
        builder.AppendLine("$timestamp = Get-Date -Format \"yyyyMMdd-HHmmss\"");
        builder.AppendLine("$backupPath = Join-Path $backupRoot \"clean-$timestamp\"");
        builder.AppendLine();
        builder.AppendLine("Assert-PathWithin -Path $persistentRoot -AllowedRoot $localLowRoot -Label \"persistentRoot\"");
        builder.AppendLine("Assert-PathWithin -Path $runtimeDbPath -AllowedRoot $persistentRoot -Label \"runtimeDbPath\"");
        builder.AppendLine("Assert-PathWithin -Path $legacyProfilePath -AllowedRoot $persistentRoot -Label \"legacyProfilePath\"");
        builder.AppendLine("Assert-PathWithin -Path $backupRoot -AllowedRoot $persistentRoot -Label \"backupRoot\"");
        builder.AppendLine("Assert-PathWithin -Path $backupPath -AllowedRoot $backupRoot -Label \"backupPath\"");
        builder.AppendLine();
        builder.AppendLine("Write-Host \"Reinicio local de datos - Squid Ink Pulse\"");
        builder.AppendLine("Write-Host \"PersistentDataPath: $persistentRoot\"");
        builder.AppendLine("Write-Host \"Semillas del build: $seedDbPath\"");
        builder.AppendLine();
        builder.AppendLine("if (-not $Force -and -not $WhatIfPreference) {");
        builder.AppendLine("    Write-Host \"\"");
        builder.AppendLine("    Write-Host \"Esto borrara progreso local de este PC: camarones, best, mejoras, skins compradas y leaderboard local.\"");
        builder.AppendLine("    Write-Host \"No borra el servidor de feria ni otros PCs.\"");
        builder.AppendLine("    $answer = Read-Host \"Escribe SI para continuar\"");
        builder.AppendLine("    if ($answer -ne \"SI\") {");
        builder.AppendLine("        Write-Host \"Operacion cancelada.\"");
        builder.AppendLine("        return");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("$hasRuntimeDb = Test-Path -LiteralPath $runtimeDbPath");
        builder.AppendLine("$hasLegacyProfile = Test-Path -LiteralPath $legacyProfilePath");
        builder.AppendLine();
        builder.AppendLine("if (-not $NoBackup -and ($hasRuntimeDb -or $hasLegacyProfile)) {");
        builder.AppendLine("    if ($PSCmdlet.ShouldProcess($backupPath, \"Crear respaldo de persistencia actual\")) {");
        builder.AppendLine("        New-Item -ItemType Directory -Force -Path $backupPath | Out-Null");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    if ($hasRuntimeDb -and $PSCmdlet.ShouldProcess($runtimeDbPath, \"Respaldar db\")) {");
        builder.AppendLine("        Copy-Item -LiteralPath $runtimeDbPath -Destination (Join-Path $backupPath \"db\") -Recurse -Force");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    if ($hasLegacyProfile -and $PSCmdlet.ShouldProcess($legacyProfilePath, \"Respaldar player-profile.json legacy\")) {");
        builder.AppendLine("        Copy-Item -LiteralPath $legacyProfilePath -Destination (Join-Path $backupPath \"player-profile.legacy.json\") -Force");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("if ($hasRuntimeDb -and $PSCmdlet.ShouldProcess($runtimeDbPath, \"Borrar db persistida\")) {");
        builder.AppendLine("    Remove-Item -LiteralPath $runtimeDbPath -Recurse -Force");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("if ($hasLegacyProfile -and $PSCmdlet.ShouldProcess($legacyProfilePath, \"Borrar player-profile.json legacy\")) {");
        builder.AppendLine("    Remove-Item -LiteralPath $legacyProfilePath -Force");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("if ($PSCmdlet.ShouldProcess($runtimeDbPath, \"Recrear db persistida desde semillas limpias\")) {");
        builder.AppendLine("    New-Item -ItemType Directory -Force -Path $runtimeDbPath | Out-Null");
        builder.AppendLine("    Get-ChildItem -LiteralPath $seedDbPath -Filter \"*.json\" -File |");
        builder.AppendLine("        Copy-Item -Destination $runtimeDbPath -Force");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("if ($IncludePlayerPrefs) {");
        builder.AppendLine("    $playerPrefsKey = \"HKCU:\\Software\\$CompanyName\\$ProductName\"");
        builder.AppendLine("    if (Test-Path -LiteralPath $playerPrefsKey) {");
        builder.AppendLine("        if ($PSCmdlet.ShouldProcess($playerPrefsKey, \"Borrar PlayerPrefs del juego\")) {");
        builder.AppendLine("            Remove-Item -LiteralPath $playerPrefsKey -Recurse -Force");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("Write-Host \"\"");
        builder.AppendLine("if ($WhatIfPreference) {");
        builder.AppendLine("    Write-Host \"Simulacion terminada. No se modificaron archivos.\"");
        builder.AppendLine("} else {");
        builder.AppendLine("    Write-Host \"Reinicio terminado.\"");
        builder.AppendLine("}");
        builder.AppendLine("Write-Host \"- Mejoras permanentes: nivel 0.\"");
        builder.AppendLine("Write-Host \"- Camarones: 0.\"");
        builder.AppendLine("Write-Host \"- Best score/runs/leaderboard local: limpio.\"");
        builder.AppendLine("Write-Host \"- Skins: solo skin.default desbloqueada y equipada.\"");
        builder.AppendLine();
        builder.AppendLine("if (-not $WhatIfPreference -and -not $NoBackup -and ($hasRuntimeDb -or $hasLegacyProfile)) {");
        builder.AppendLine("    Write-Host \"- Respaldo: $backupPath\"");
        builder.AppendLine("}");
        builder.AppendLine();
        builder.AppendLine("if (-not $IncludePlayerPrefs) {");
        builder.AppendLine("    Write-Host \"- PlayerPrefs no fueron borrados. Usa -IncludePlayerPrefs si tambien quieres limpiar opciones/URL de feria.\"");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string EscapePowerShellSingleQuotedString(string value)
    {
        return (value ?? string.Empty).Replace("'", "''");
    }
}
