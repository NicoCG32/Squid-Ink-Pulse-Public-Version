#requires -Version 7.0
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot '../ProcessCpu.ps1')
function Assert-True([bool]$Condition,[string]$Message) { if (-not $Condition) { throw $Message } }
$example='16164 (s.squidinkpulse) S 1277 1277 0 0 -1 1077936448 108155 0 578 0 286 158 0 0 10 -10 73 0 105424 23916564480 105674'
$before=ConvertFrom-AndroidProcessStat $example
Assert-True ($before.ProcessId -eq 16164 -and $before.CpuTicks -eq 444 -and $before.StartTicks -eq 105424) 'Real Android stat fields were misread.'
$named=ConvertFrom-AndroidProcessStat ($example.Replace('(s.squidinkpulse)','(game (worker) name)'))
Assert-True ($named.CpuTicks -eq 444 -and $named.StartTicks -eq 105424) 'comm parentheses/spaces shifted CPU fields.'
Assert-True ($null -eq (ConvertFrom-AndroidProcessStat 'Permission denied')) 'Unavailable /proc data must remain null.'
Assert-True ($null -eq (ConvertFrom-AndroidProcessStat '12 (truncated) S 0')) 'Truncated input must remain null.'
$after=[pscustomobject]@{ProcessId=16164;StartTicks=105424;CpuTicks=744}
$delta=Get-AndroidProcessCpuDelta $before $after 100 2
Assert-True ($delta.CpuTimeMs -eq 3000 -and $delta.OneCorePercent -eq 150) 'One-core CPU utilization must support values above 100%.'
$reused=[pscustomobject]@{ProcessId=16164;StartTicks=105500;CpuTicks=744}
Assert-True ($null -eq (Get-AndroidProcessCpuDelta $before $reused 100 2)) 'PID reuse must invalidate a delta.'
$restarted=[pscustomobject]@{ProcessId=16165;StartTicks=105424;CpuTicks=744}
Assert-True ($null -eq (Get-AndroidProcessCpuDelta $before $restarted 100 2)) 'A process restart must invalidate a delta.'
$reversed=[pscustomobject]@{ProcessId=16164;StartTicks=105424;CpuTicks=400}
Assert-True ($null -eq (Get-AndroidProcessCpuDelta $before $reversed 100 2)) 'Decreasing ticks must invalidate a delta.'
Assert-True ($null -eq (Get-AndroidProcessCpuDelta $before $after 0 2)) 'Unavailable clock rate must remain null.'
Assert-True ($null -eq (Get-AndroidProcessCpuDelta $before $after 100 0)) 'Invalid duration must remain null.'
Assert-True ($null -eq (Get-AndroidProcessCpuDelta $null $after 100 2)) 'Missing samples must remain null.'
Assert-True ($null -eq (Get-AndroidProcessCpuDelta $before $after ([double]::NaN) 2)) 'NaN clock must remain null.'
Assert-True ($null -eq (Get-AndroidProcessCpuDelta $before $after 100 ([double]::PositiveInfinity))) 'Infinite duration must remain null.'
Write-Output '13 process CPU counter checks passed.'
