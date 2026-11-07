# Uploads release files to VirusTotal, waits for every scan to finish and writes the results:
# JSON for the landing page (docs/virustotal.json) and a Markdown table for the release notes.
# Needs VT_API_KEY (free key from virustotal.com, stored as a repo secret).
param(
    [Parameter(Mandatory)] [string] $Version,
    [Parameter(Mandatory)] [string[]] $Files,
    [string] $Out = 'virustotal.json',
    [string] $Notes = 'vt-notes.md',
    [int] $TimeoutMinutes = 20
)

$ErrorActionPreference = 'Stop'
if (-not $env:VT_API_KEY) { throw 'VT_API_KEY is not set' }

# Pasting the key into the GitHub secret can bring along spaces, a BOM or zero-width characters
$key = $env:VT_API_KEY -replace '[\s\u200B-\u200D\u2060\uFEFF]', ''
if ($key -notmatch '^[0-9a-fA-F]{64}$') {
    # describe the problem without printing the key itself
    $bad = ($key.ToCharArray() | Where-Object { $_ -notmatch '[0-9a-fA-F]' } |
        ForEach-Object { 'U+{0:X4}' -f [int]$_ } | Select-Object -Unique) -join ', '
    throw "VT_API_KEY should be 64 hex characters but has $($key.Length) characters$(if ($bad) { "; unexpected: $bad" }). Copy it again from virustotal.com > API key and update the repository secret."
}
# the cleaned key may differ from the raw secret, which is the only thing GitHub masks
Write-Host "::add-mask::$key"
$api = 'https://www.virustotal.com/api/v3'
$headers = @{ 'x-apikey' = $key }

function Submit-File([string] $path) {
    # files over 32 MB need a one-off upload URL (the free API takes up to 650 MB)
    $url = "$api/files"
    if ((Get-Item $path).Length -gt 32MB) {
        $url = (Invoke-RestMethod "$api/files/upload_url" -Headers $headers).data
    }
    # curl, not Invoke-RestMethod -Form: VirusTotal answers "Malformed multipart body" to the
    # quoted boundary and filename* field that PowerShell's multipart writer produces
    $response = & curl.exe --silent --show-error --fail-with-body -X POST $url -H "x-apikey: $key" -F "file=@$path"
    if ($LASTEXITCODE -ne 0) { throw "Uploading $path to VirusTotal failed (curl exit $LASTEXITCODE): $response" }
    ($response | ConvertFrom-Json).data.id
}

$analyses = [ordered]@{}
foreach ($file in $Files) {
    $name = Split-Path $file -Leaf
    $analyses[$name] = Submit-File $file
    Write-Host "Uploaded $name"
}

$results = [ordered]@{}
$deadline = (Get-Date).AddMinutes($TimeoutMinutes)
while ($results.Count -lt $analyses.Count) {
    if ((Get-Date) -gt $deadline) { throw "VirusTotal did not finish within $TimeoutMinutes minutes" }
    Start-Sleep -Seconds 60   # free API: 4 requests a minute
    foreach ($name in @($analyses.Keys)) {
        if ($results.Contains($name)) { continue }
        $r = Invoke-RestMethod "$api/analyses/$($analyses[$name])" -Headers $headers
        if ($r.data.attributes.status -ne 'completed') { continue }
        $s = $r.data.attributes.stats
        $sha = $r.meta.file_info.sha256
        $results[$name] = [ordered]@{
            name       = $name
            sha256     = $sha
            malicious  = $s.malicious
            suspicious = $s.suspicious
            engines    = $s.malicious + $s.suspicious + $s.undetected + $s.harmless
            url        = "https://www.virustotal.com/gui/file/$sha"
        }
        Write-Host "$name : $($s.malicious) malicious, $($s.suspicious) suspicious"
    }
}

[ordered]@{
    version   = $Version
    scannedAt = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    files     = @($results.Values)
} | ConvertTo-Json -Depth 4 | Set-Content $Out -Encoding utf8

$rows = $results.Values | ForEach-Object {
    "| [$($_.name)]($($_.url)) | $($_.malicious + $_.suspicious) / $($_.engines) | ``$($_.sha256)`` |"
}
@('', '### VirusTotal', '', '| File | Detections | SHA-256 |', '|---|---|---|') + $rows |
    Set-Content $Notes -Encoding utf8
