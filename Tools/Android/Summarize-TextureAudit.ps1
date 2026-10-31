[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SnapshotPath,
    [Parameter(Mandatory)][string]$ResourceAuditPath,
    [Parameter(Mandatory)][string]$FamilyPath
)
$ErrorActionPreference='Stop'
$snapshot=Get-Content -LiteralPath $SnapshotPath -Raw|ConvertFrom-Json
$audit=Get-Content -LiteralPath $ResourceAuditPath -Raw|ConvertFrom-Json
if(-not $snapshot.buildGuid -or $snapshot.buildGuid -ne $audit.buildGuid){throw 'Snapshot and BuildReport must identify the same player build.'}
$family=$FamilyPath.TrimEnd('/')+'/'
$index=[Collections.Generic.Dictionary[string,Collections.Generic.List[object]]]::new([StringComparer]::Ordinal)
foreach($asset in $audit.assets){
    if($asset.kind -ne 'Texture'){continue}
    $name=[IO.Path]::GetFileNameWithoutExtension($asset.path)
    $key="$name|$($asset.sourceWidth)|$($asset.sourceHeight)"
    if(-not $index.ContainsKey($key)){$index[$key]=[Collections.Generic.List[object]]::new()}
    $index[$key].Add($asset)
}
$matched=[Collections.Generic.List[object]]::new()
$ambiguous=0
foreach($texture in $snapshot.textures){
    $key="$($texture.name)|$($texture.width)|$($texture.height)"
    if(-not $index.ContainsKey($key)){continue}
    $sources=$index[$key]
    if($sources.Count -ne 1){$ambiguous++;continue}
    if(-not $sources[0].path.StartsWith($family,[StringComparison]::Ordinal)){continue}
    $matched.Add([pscustomobject]@{
        path=$sources[0].path;instanceId=$texture.instanceId;format=$texture.format
        graphicsFormat=$texture.graphicsFormat;width=$texture.width;height=$texture.height
        runtimeMemoryBytes=$(if($texture.runtimeMemoryBytes -gt 0){[long]$texture.runtimeMemoryBytes}else{$null})
    })
}
$expected=@($audit.assets|Where-Object{$_.kind -eq 'Texture' -and $_.path.StartsWith($family,[StringComparison]::Ordinal)})
$unique=@($matched|Select-Object -ExpandProperty path -Unique)
$unavailable=@($matched|Where-Object{$null -eq $_.runtimeMemoryBytes}).Count
$nativeBytes=if($matched.Count -gt 0 -and $unavailable -eq 0){[long]($matched|Measure-Object runtimeMemoryBytes -Sum).Sum}else{$null}
[ordered]@{
    buildGuid=$snapshot.buildGuid;reportedSourceCommit=$audit.reportedSourceCommit
    snapshotSha256=(Get-FileHash -LiteralPath $SnapshotPath).Hash.ToLowerInvariant()
    resourceAuditSha256=(Get-FileHash -LiteralPath $ResourceAuditPath).Hash.ToLowerInvariant()
    scene=$snapshot.scene;development=$snapshot.development;gpu=$snapshot.gpu;graphicsApi=$snapshot.graphicsApi
    astc4x4Supported=$snapshot.astc4x4Supported;family=$family;includedTextureCount=$expected.Count
    loadedTextureObjects=$matched.Count;loadedUniqueAssetPaths=$unique.Count
    reportedNativeBytes=$nativeBytes;unavailableMemoryRows=$unavailable
    referencePackedBytes=[long]($expected|Measure-Object packedBytes -Sum).Sum
    ambiguousGlobalSignatures=$ambiguous
    missingFamilyPaths=@($expected|Where-Object{$_.path -notin $unique}|Select-Object -ExpandProperty path)
    formats=@($matched|Group-Object format|ForEach-Object{[ordered]@{format=$_.Name;count=$_.Count}})
    previewFile=$snapshot.previewFile;enumerationMilliseconds=$snapshot.enumerationMilliseconds
    limitations='Exact name/source-dimension correlation only; resized or ambiguous textures are not attributed. Loaded objects and Unity native-memory estimates do not prove physical VRAM residency. A null byte total means unavailable/not loaded, not zero allocation. No FPS or GPU timing attribution.'
    textures=$matched.ToArray()
}|ConvertTo-Json -Depth 6
