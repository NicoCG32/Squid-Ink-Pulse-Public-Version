function ConvertFrom-AndroidProcessStat([string]$Stat) {
    # comm may contain spaces and parentheses; fields after its final ')' start at field 3.
    $match=[regex]::Match($Stat.Trim(),'^(\d+)\s+\((.*)\)\s+(.+)$')
    if (-not $match.Success) { return $null }
    $fields=$match.Groups[3].Value -split '\s+'
    if ($fields.Count -lt 20) { return $null }
    [long]$userTicks=0;[long]$kernelTicks=0;[long]$startTicks=0
    if (-not [long]::TryParse($fields[11],[ref]$userTicks) -or
        -not [long]::TryParse($fields[12],[ref]$kernelTicks) -or
        -not [long]::TryParse($fields[19],[ref]$startTicks) -or
        $userTicks -lt 0 -or $kernelTicks -lt 0 -or $startTicks -lt 0) { return $null }
    [pscustomobject]@{ProcessId=[long]$match.Groups[1].Value;StartTicks=$startTicks;CpuTicks=($userTicks+$kernelTicks)}
}

function Get-AndroidProcessCpuDelta($Before,$After,[double]$TicksPerSecond,[double]$ElapsedSeconds) {
    if ($null -eq $Before -or $null -eq $After -or
        $TicksPerSecond -le 0 -or $ElapsedSeconds -le 0 -or
        [double]::IsNaN($TicksPerSecond) -or [double]::IsInfinity($TicksPerSecond) -or
        [double]::IsNaN($ElapsedSeconds) -or [double]::IsInfinity($ElapsedSeconds) -or
        $Before.ProcessId -ne $After.ProcessId -or $Before.StartTicks -ne $After.StartTicks -or
        $After.CpuTicks -lt $Before.CpuTicks) { return $null }
    $cpuSeconds=($After.CpuTicks-$Before.CpuTicks)/$TicksPerSecond
    [pscustomobject]@{
        CpuTimeMs=[Math]::Round($cpuSeconds*1000,2)
        OneCorePercent=[Math]::Round(100*$cpuSeconds/$ElapsedSeconds,2)
        ElapsedSeconds=$ElapsedSeconds
    }
}
