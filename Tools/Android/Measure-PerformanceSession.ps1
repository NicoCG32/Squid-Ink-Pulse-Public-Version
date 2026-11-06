#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$AdbPath,
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Label,
    [Parameter(Mandatory)][ValidatePattern('^[a-f0-9]{7,40}$')][string]$SourceCommit,
    [Parameter(Mandatory)][string]$ApkPath,
    [ValidateSet('Development','Performance')][string]$Configuration='Performance',
    [ValidateRange(1,60)][int]$Windows=12,
    [ValidateRange(5,30)][int]$SecondsPerWindow=15,
    [string]$ScenarioNote='Unclassified session; review every window screenshot before interpreting gameplay.'
)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$relativeOutput='TestResults/MobilePerformance/'+$Label
$output=Join-Path $root $relativeOutput
if (Test-Path -LiteralPath $output) { throw 'Session output already exists; preserve it and use another label.' }
$apk=(Resolve-Path -LiteralPath $ApkPath).Path
$captureScript=Join-Path $PSScriptRoot 'Measure-FramePacing.ps1'
$manifest=[ordered]@{
    Label=$Label;SourceCommit=$SourceCommit;ApkSha256=(Get-FileHash -LiteralPath $apk -Algorithm SHA256).Hash
    Configuration=$Configuration;ScenarioNote=$ScenarioNote;Status='Running';StartedUtc=[DateTime]::UtcNow.ToString('o')
    RequestedWindows=$Windows;RequestedSecondsPerWindow=$SecondsPerWindow;CompletedWindows=0
    SessionScriptSha256=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash
    FrameScriptSha256=(Get-FileHash -LiteralPath $captureScript -Algorithm SHA256).Hash
    Windows=[Collections.Generic.List[object]]::new();ErrorMessage=$null;WallSeconds=$null;WholeSessionFps=$null
    Limitations='Consecutive sampled windows separated by screenshots/dumps/commands; not an uninterrupted frame trace. No automatic scene/overlay classification. Do not aggregate mixed scenarios. No input injection, CPU/GPU/GC attribution, profile writes or thermal endurance certification.'
}
[void][IO.Directory]::CreateDirectory($output)
$manifestPath=Join-Path $output 'session.json'
$timer=[Diagnostics.Stopwatch]::StartNew()
Push-Location $root
try {
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    for ($index=1;$index -le $Windows;$index++) {
        $windowLabel=('{0}-{1:d3}' -f $Label,$index)
        & $captureScript -AdbPath $AdbPath -Label $windowLabel -SourceCommit $SourceCommit -ApkPath $apk `
            -Configuration $Configuration -Seconds $SecondsPerWindow -OutputDirectory $relativeOutput | Out-Null
        $window=Get-Content -LiteralPath (Join-Path $output ($windowLabel+'.json')) -Raw | ConvertFrom-Json
        $manifest.Windows.Add([ordered]@{
            Label=$windowLabel;Report=($windowLabel+'.json');ReviewStatus='Unreviewed'
            StartedUtc=$window.StartedUtc;ElapsedSeconds=$window.ElapsedSeconds;ApproxFps=$window.CountOverWallTimeFpsApprox
            P95BucketMs=$window.P95PresentIntervalBucketMs;P99BucketMs=$window.P99PresentIntervalBucketMs
            PssAfterKiB=$window.PssAfterKiB;ThermalAfter=$window.ThermalStatusAfter
            ProcessCpuOneCorePercent=$window.ProcessCpuOneCorePercent
        })
        $manifest.CompletedWindows=$index
        $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8
        Write-Output ("Window {0}/{1}: ~{2} FPS; p95/p99 buckets {3}/{4} ms; review required." -f $index,$Windows,$window.CountOverWallTimeFpsApprox,$window.P95PresentIntervalBucketMs,$window.P99PresentIntervalBucketMs)
    }
    $manifest.Status='CompleteUnreviewed'
} catch {
    $manifest.Status='Interrupted'
    $manifest.ErrorMessage=$_.Exception.Message
    throw
} finally {
    $timer.Stop();$manifest.WallSeconds=$timer.Elapsed.TotalSeconds
    $manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding utf8
    Pop-Location
    Write-Output $manifestPath
}
