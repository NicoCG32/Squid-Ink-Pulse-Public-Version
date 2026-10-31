#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$AdbPath,
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9-]+$')][string]$Label,
    [Parameter(Mandatory)][ValidatePattern('^[a-f0-9]{7,40}$')][string]$SourceCommit,
    [Parameter(Mandatory)][string]$ApkPath,
    [ValidateSet('Development','Performance')][string]$Configuration='Performance',
    [ValidateRange(5,30)][int]$Seconds=15,
    [string]$OutputDirectory='TestResults/MobilePerformance'
)
$ErrorActionPreference='Stop'
$package='com.yecoworks.squidinkpulse'
$activity="$package/com.unity3d.player.UnityPlayerGameActivity"
function Invoke-Adb([string[]]$Arguments) {
    $result=& $AdbPath -d @Arguments
    if ($LASTEXITCODE -ne 0) { throw "ADB failed: $($Arguments -join ' ')" }
    return $result
}
function Assert-GameForeground {
    $top=(Invoke-Adb @('shell','dumpsys','activity','activities') | Select-String 'topResumedActivity|mResumedActivity') -join "`n"
    $focus=(Invoke-Adb @('shell','dumpsys','window') | Select-String 'mCurrentFocus=') -join "`n"
    if (-not $top.Contains($activity) -or -not $focus.Contains($activity)) { throw 'Game must own the foreground for the whole sample.' }
}
function Save-GameScreenshot([string]$name) {
    Assert-GameForeground
    Invoke-Adb @('shell','screencap','-p','/sdcard/squid-performance.png') | Out-Null
    Invoke-Adb @('pull','/sdcard/squid-performance.png',(Join-Path $output $name)) | Out-Null
    Invoke-Adb @('shell','rm','/sdcard/squid-performance.png') | Out-Null
}
function Read-Number([string]$text,[string]$key) {
    $match=[regex]::Match($text,'(?m)^'+[regex]::Escape($key)+'\s*=\s*([\d.]+)')
    if (-not $match.Success) { return $null }
    return [double]::Parse($match.Groups[1].Value,[Globalization.CultureInfo]::InvariantCulture)
}
function Read-Pss([string]$text) {
    $match=[regex]::Match($text,'TOTAL PSS:\s+(\d+)')
    if ($match.Success) { return [long]$match.Groups[1].Value }; return $null
}
function Get-BucketPercentile($buckets,[double]$percentile) {
    $count=($buckets | Measure-Object Count -Sum).Sum
    if ($count -le 0) { return $null }
    $threshold=[Math]::Ceiling($count*$percentile); $cumulative=0L
    foreach ($bucket in $buckets) { $cumulative+=$bucket.Count; if ($cumulative -ge $threshold) { return $bucket.Ms } }
}
$projectRoot=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '../..')).Path
$output=[IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory))
$allowed=[IO.Path]::GetFullPath((Join-Path $projectRoot 'TestResults'))+[IO.Path]::DirectorySeparatorChar
if (-not $output.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)) { throw 'Output must be a child of TestResults/.' }
[void][IO.Directory]::CreateDirectory($output)
if (Test-Path -LiteralPath (Join-Path $output ($Label+'.json'))) { throw 'Sample label already exists; keep the previous evidence.' }
$apk=(Resolve-Path -LiteralPath $ApkPath).Path
$apkHash=(Get-FileHash -LiteralPath $apk -Algorithm SHA256).Hash
$installedPath=(Invoke-Adb @('shell','pm','path',$package) | Select-Object -First 1) -replace '^package:',''
if ([string]::IsNullOrWhiteSpace($installedPath)) { throw 'Package is not installed.' }
$installedHash=((Invoke-Adb @('shell','sha256sum',$installedPath)) -split '\s+')[0].ToUpperInvariant()
if ($installedHash -ne $apkHash) { throw 'Installed APK does not match the supplied artifact.' }
Assert-GameForeground
$model=(Invoke-Adb @('shell','getprop','ro.product.model')) -join ''
$memoryBefore=(Invoke-Adb @('shell','dumpsys','meminfo',$package)) -join "`n"
$batteryBefore=(Invoke-Adb @('shell','dumpsys','battery')) -join "`n"
$thermalBefore=(Invoke-Adb @('shell','dumpsys','thermalservice') | Select-String '^Thermal Status:') -join ''
Save-GameScreenshot ($Label+'-before.png')
try {
    Invoke-Adb @('shell','dumpsys','SurfaceFlinger','--timestats','-clear','-enable') | Out-Null
    $started=[DateTime]::UtcNow
    Start-Sleep -Seconds $Seconds
    Assert-GameForeground
    $allLines=@(Invoke-Adb @('shell','dumpsys','SurfaceFlinger','--timestats','-dump'))
    $elapsed=([DateTime]::UtcNow-$started).TotalSeconds
} finally { Invoke-Adb @('shell','dumpsys','SurfaceFlinger','--timestats','-disable') | Out-Null }
$sections=[Collections.Generic.List[string]]::new()
for ($index=0;$index -lt $allLines.Count;$index++) {
    if ($allLines[$index] -notmatch '^layerName = .*SurfaceView\[com\.yecoworks\.squidinkpulse/') { continue }
    $section=[Collections.Generic.List[string]]::new(); $section.Add($allLines[$index]); $index++
    while ($index -lt $allLines.Count -and $allLines[$index] -notmatch '^layerName = ') { $section.Add($allLines[$index]); $index++ }
    $index--; $sections.Add(($section -join "`n"))
}
if ($sections.Count -eq 0) { throw 'SurfaceFlinger did not expose the game surface; no FPS result is available.' }
$selected=$sections | Sort-Object { Read-Number $_ 'totalFrames' } -Descending | Select-Object -First 1
$histogram=[regex]::Match($selected,'(?m)^present2present histogram is as below:\r?\n([^\r\n]+)')
if (-not $histogram.Success) { throw 'Missing present-to-present histogram.' }
$buckets=@([regex]::Matches($histogram.Groups[1].Value,'(\d+)ms=(\d+)') | ForEach-Object { [pscustomobject]@{Ms=[int]$_.Groups[1].Value;Count=[long]$_.Groups[2].Value} } | Sort-Object Ms)
$sampleCount=($buckets | Measure-Object Count -Sum).Sum
if ($sampleCount -lt 2) { throw 'Too few presented frames to compute intervals.' }
$memoryAfter=(Invoke-Adb @('shell','dumpsys','meminfo',$package)) -join "`n"
$batteryAfter=(Invoke-Adb @('shell','dumpsys','battery')) -join "`n"
$thermalAfter=(Invoke-Adb @('shell','dumpsys','thermalservice') | Select-String '^Thermal Status:') -join ''
Save-GameScreenshot ($Label+'-after.png')
$selected | Set-Content -LiteralPath (Join-Path $output ($Label+'-surface.txt')) -Encoding utf8
$memoryBefore | Set-Content -LiteralPath (Join-Path $output ($Label+'-memory-before.txt')) -Encoding utf8
$memoryAfter | Set-Content -LiteralPath (Join-Path $output ($Label+'-memory-after.txt')) -Encoding utf8
@($batteryBefore,$batteryAfter,$thermalBefore,$thermalAfter) | Set-Content -LiteralPath (Join-Path $output ($Label+'-conditions.txt')) -Encoding utf8
$summary=[ordered]@{
    Label=$Label;SourceCommit=$SourceCommit;ApkSha256=$apkHash;InstalledApkHashVerified=$true
    Configuration=$Configuration;Model=$model;StartedUtc=$started.ToString('o');RequestedSeconds=$Seconds;ElapsedSeconds=$elapsed
    Layer=($selected -split "`n")[0];SurfaceFrameRateVote=(Read-Number $selected 'frameRate');AverageFps=(Read-Number $selected 'averageFPS')
    PresentedHistogramCount=$sampleCount;P50PresentIntervalBucketMs=(Get-BucketPercentile $buckets 0.50)
    P95PresentIntervalBucketMs=(Get-BucketPercentile $buckets 0.95);P99PresentIntervalBucketMs=(Get-BucketPercentile $buckets 0.99)
    FramesInBucketsAtLeast34ms=(($buckets | Where-Object Ms -ge 34 | Measure-Object Count -Sum).Sum)
    DroppedFramesReported=(Read-Number $selected 'droppedFrames');JankyFramesReported=(Read-Number $selected 'jankyFrames')
    BufferStuffingFramesReported=(Read-Number $selected 'appBufferStuffingJankyFrames');LowSampleCount=($sampleCount -lt 120)
    PssBeforeKiB=(Read-Pss $memoryBefore);PssAfterKiB=(Read-Pss $memoryAfter)
    ThermalStatusBefore=$thermalBefore;ThermalStatusAfter=$thermalAfter
    CpuGpuFrameTimesMs=$null;GcAllocations=$null
    Limitations='Presented-frame histogram, quantized bucket percentiles. Scenario is caller-labelled; review screenshots. No script/GPU execution times, GC attribution or thermal endurance certification.'
}
$summary | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output ($Label+'.json')) -Encoding utf8
[pscustomobject]$summary | Select-Object Label,AverageFps,P95PresentIntervalBucketMs,P99PresentIntervalBucketMs,PresentedHistogramCount,PssAfterKiB
