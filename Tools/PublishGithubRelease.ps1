[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Tag,
    [Parameter(Mandatory)][string]$Name,
    [Parameter(Mandatory)][string]$TargetCommit,
    [Parameter(Mandatory)][string]$BodyFile,
    [Parameter(Mandatory)][string[]]$AssetPaths,
    [string]$Repository = 'NicoCG32/Squid-Ink-Pulse-Public-Version',
    [switch]$Prerelease,
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'
if ($Repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$') { throw 'Invalid repository.' }
if ($Tag -notmatch '^[A-Za-z0-9_.-]+$') { throw 'Invalid tag.' }
if ($TargetCommit -notmatch '^[a-f0-9]{40}$') { throw 'Specify the full source commit.' }
$body = Get-Content -LiteralPath $BodyFile -Raw
$assets = @($AssetPaths | ForEach-Object {
    $file = Get-Item -LiteralPath $_
    if ($file.PSIsContainer -or $file.Length -ge 2GB) { throw 'Each release asset must be a file smaller than 2 GiB.' }
    if ($file.Extension -notin @('.zip', '.apk', '.txt', '.json', '.patch', '.md')) { throw 'Unexpected release asset type.' }
    $file
})
if (($assets.Name | Select-Object -Unique).Count -ne $assets.Count) { throw 'Asset names must be unique.' }

# Reuse Git Credential Manager; never print or persist the credential.
$credentialLines = "protocol=https`nhost=github.com`n`n" | git credential fill
if ($LASTEXITCODE -ne 0) { throw 'GitHub credential unavailable.' }
$credential = @{}
$credentialLines | ForEach-Object {
    if ($_ -match '^([^=]+)=(.*)$') { $credential[$matches[1]] = $matches[2] }
}
if (-not $credential.password) { throw 'GitHub credential unavailable.' }
$headers = @{ Authorization=('Bearer ' + $credential.password); 'User-Agent'='SquidInkPulse-Distribution';
    Accept='application/vnd.github+json'; 'X-GitHub-Api-Version'='2022-11-28' }
$api = "https://api.github.com/repos/$Repository"
try {
    $repo = Invoke-RestMethod -Uri $api -Headers $headers
    if (-not $repo.permissions.push) { throw 'GitHub write permission is required.' }
    $release = $null
    try { $release = Invoke-RestMethod -Uri "$api/releases/tags/$Tag" -Headers $headers }
    catch { if ([int]$_.Exception.Response.StatusCode -ne 404) { throw } }
    if ($release -and -not $release.draft) { throw 'This tag is already published; do not replace its assets.' }
    if (-not $release) {
        $request = @{ tag_name=$Tag; target_commitish=$TargetCommit; name=$Name; body=$body;
            draft=$true; prerelease=[bool]$Prerelease; make_latest='false' } | ConvertTo-Json
        $release = Invoke-RestMethod -Method Post -Uri "$api/releases" -Headers $headers -Body $request -ContentType 'application/json; charset=utf-8'
    }
    foreach ($asset in $assets) {
        $localDigest = (Get-FileHash -LiteralPath $asset.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $remoteAssets = Invoke-RestMethod -Uri "$api/releases/$($release.id)/assets" -Headers $headers
        $existing = $remoteAssets | Where-Object { $_.name -eq $asset.Name }
        if ($existing) {
            if ($existing.digest -ne "sha256:$localDigest" -or $existing.size -ne $asset.Length) {
                throw 'Existing draft asset differs; preserve it and investigate.'
            }
            Write-Output "Verified existing asset: $($asset.Name)"
            continue
        }
        Write-Output "Uploading: $($asset.Name) ($($asset.Length) bytes)"
        $encodedName = [Uri]::EscapeDataString($asset.Name)
        $uploadUri = "https://uploads.github.com/repos/$Repository/releases/$($release.id)/assets?name=$encodedName"
        $uploaded = Invoke-RestMethod -Method Post -Uri $uploadUri -Headers $headers -InFile $asset.FullName -ContentType 'application/octet-stream' -TimeoutSec 1800
        if ($uploaded.digest -ne "sha256:$localDigest" -or $uploaded.size -ne $asset.Length) {
            throw 'GitHub asset digest or size did not match the local artifact; draft kept unpublished.'
        }
        Write-Output "Verified upload: $($asset.Name)"
    }
    $update = @{ name=$Name; body=$body; prerelease=[bool]$Prerelease; draft=(-not [bool]$Publish);
        make_latest=$(if ($Publish -and -not $Prerelease) { 'true' } else { 'false' }) } | ConvertTo-Json
    $release = Invoke-RestMethod -Method Patch -Uri "$api/releases/$($release.id)" -Headers $headers -Body $update -ContentType 'application/json; charset=utf-8'
    [ordered]@{ id=$release.id; url=$release.html_url; tag=$release.tag_name; draft=$release.draft;
        prerelease=$release.prerelease; assets=@($release.assets | Select-Object name,size,digest)
    } | ConvertTo-Json -Depth 5
}
finally {
    $headers.Clear()
    $credential.Clear()
    $credentialLines = $null
}
