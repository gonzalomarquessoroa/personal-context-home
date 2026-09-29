param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-p]{32}$')]
    [string]$ExtensionId
)

$ErrorActionPreference = 'Stop'
$repository = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$hostExe = Join-Path $repository '.tools/native-host/publish/PersonalContext.NativeHost.exe'
if (-not (Test-Path -LiteralPath $hostExe)) {
    throw "Publique primero el host autocontenido. No se encontró: $hostExe"
}

$manifestDirectory = Join-Path $repository '.tools/native-host'
New-Item -ItemType Directory -Path $manifestDirectory -Force | Out-Null
$manifestPath = Join-Path $manifestDirectory 'com.personal_context_home.probe.json'
$manifest = @{
    name = 'com.personal_context_home.probe'
    description = 'Personal Context Home local capture feasibility probe'
    path = $hostExe
    type = 'stdio'
    allowed_origins = @("chrome-extension://$ExtensionId/")
} | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText($manifestPath, $manifest, [System.Text.UTF8Encoding]::new($false))

$registryKey = 'HKCU:\Software\Google\Chrome\NativeMessagingHosts\com.personal_context_home.probe'
New-Item -Path $registryKey -Force | Out-Null
Set-Item -Path $registryKey -Value $manifestPath
Write-Output "Host de prueba registrado para $ExtensionId. Manifiesto: $manifestPath"
