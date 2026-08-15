[CmdletBinding()]
param(
    [string]$DotnetPath = "dotnet",
    [string]$LegacyVersion = "0.1.0-alpha.19",
    [string]$CurrentVersion = "0.1.0-alpha.22"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path $PSScriptRoot -Parent | Split-Path -Parent
$integrationDirectory = Join-Path $repositoryRoot "tests\SearchPulse.Umbraco.Integration"
$project = Join-Path $integrationDirectory "SearchPulse.Umbraco.Integration.csproj"
$artifacts = Join-Path $repositoryRoot "artifacts"
$executable = Join-Path $integrationDirectory "bin\Debug\net10.0\SearchPulse.Umbraco.Integration.exe"
$temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("SearchPulse.Upgrade." + [Guid]::NewGuid().ToString("N"))
$databasePath = Join-Path $temporaryDirectory "Umbraco.sqlite.db"
$process = $null
$environmentBackup = @{}

foreach ($name in @("ASPNETCORE_ENVIRONMENT", "ASPNETCORE_URLS", "ConnectionStrings__umbracoDbDSN", "ConnectionStrings__umbracoDbDSN_ProviderName")) {
    $environmentBackup[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
}

function Invoke-Dotnet([string[]]$Arguments) {
    & $DotnetPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code ${LASTEXITCODE}: $($Arguments -join ' ')"
    }
}

function Build-Version([string]$Version) {
    Invoke-Dotnet @("restore", $project, "--no-cache", "-p:SearchPulsePackageVersion=$Version")
    Invoke-Dotnet @("build", $project, "--no-restore", "--configuration", "Debug", "-p:SearchPulsePackageVersion=$Version", "--verbosity", "minimal")
}

function Wait-ForHost([string]$BaseUrl) {
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        if ($null -ne $script:process -and $script:process.HasExited) { throw "The upgrade verification host exited with code $($script:process.ExitCode)." }
        try {
            $response = Invoke-WebRequest -Uri "$BaseUrl/searchpulse-review" -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) { return }
        }
        catch { Start-Sleep -Seconds 1 }
    }

    throw "The upgrade verification host at $BaseUrl did not become ready."
}

function Start-Host([int]$Port) {
    $baseUrl = "http://127.0.0.1:$Port"
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new($executable)
    $startInfo.WorkingDirectory = $integrationDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development"
    $startInfo.Environment["ASPNETCORE_URLS"] = $baseUrl
    $startInfo.Environment["ConnectionStrings__umbracoDbDSN"] = "Data Source=$databasePath;Cache=Shared;Foreign Keys=True;Pooling=True"
    $startInfo.Environment["ConnectionStrings__umbracoDbDSN_ProviderName"] = "Microsoft.Data.Sqlite"
    $script:process = [System.Diagnostics.Process]::Start($startInfo)
    Wait-ForHost $baseUrl
    return $baseUrl
}
function Stop-Host([string]$BaseUrl) {
    if ($null -eq $script:process -or $script:process.HasExited) { return }
    try { Invoke-WebRequest -Uri "$BaseUrl/searchpulse-test/stop" -Method Post -UseBasicParsing -TimeoutSec 5 | Out-Null } catch { }
    if (-not $script:process.WaitForExit(30000)) { Stop-Process -Id $script:process.Id -Force }
    $script:process = $null
}

function Post-Event([string]$BaseUrl, [string]$Body) {
    $session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
    $session.Cookies.Add([System.Net.Cookie]::new("SearchPulseIntegrationConsent", "yes", "/", "127.0.0.1"))
    try {
        $response = Invoke-WebRequest -Uri "$BaseUrl/searchpulse/collect" -Method Post -UseBasicParsing -ContentType "application/json" -Headers @{ Origin = $BaseUrl } -WebSession $session -Body $Body
    }
    catch {
        $errorResponse = $_.Exception.Response
        $reader = [System.IO.StreamReader]::new($errorResponse.GetResponseStream())
        $bodyText = $reader.ReadToEnd()
        throw "Collection failed with HTTP $([int]$errorResponse.StatusCode): $bodyText"
    }
    if ($response.StatusCode -ne 202) { throw "Expected HTTP 202 from collection, received $($response.StatusCode)." }
}
try {
    New-Item -ItemType Directory -Force $temporaryDirectory | Out-Null
    foreach ($version in @($LegacyVersion, $CurrentVersion)) {
        if (-not (Test-Path (Join-Path $artifacts "SearchPulse.Umbraco.$version.nupkg"))) { throw "Missing local package artifact for $version in $artifacts." }
    }

    Build-Version $LegacyVersion
    $legacyUrl = Start-Host 5127
    Stop-Host $legacyUrl

    Build-Version $CurrentVersion
    $currentUrl = Start-Host 5128
    Post-Event $currentUrl '{"type":"page-view","path":"/upgrade-check","contentKey":"upgrade-home","referrerDomain":"partner.example","utmSource":"newsletter","utmMedium":"email","utmCampaign":"upgrade"}'

    $deadline = [DateTime]::UtcNow.AddSeconds(45)
    do {
        $schema = Invoke-RestMethod -Uri "$currentUrl/searchpulse-test/searchpulse-schema" -TimeoutSec 5
        if ([int64]$schema.ContextEvents -ge 1) {
            if ([int64]$schema.GoalRows -lt 0) { throw "Goal table returned an invalid row count." }
            Write-Output "SearchPulse upgrade verification passed: $LegacyVersion database upgraded to $CurrentVersion with context columns and goals table available."
            return
        }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "The upgraded database did not persist the context event within the timeout."
}
finally {
    if ($null -ne $process -and -not $process.HasExited) { Stop-Process -Id $process.Id -Force }
    foreach ($name in $environmentBackup.Keys) { [Environment]::SetEnvironmentVariable($name, $environmentBackup[$name], "Process") }
    if (Test-Path $temporaryDirectory) { Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force -ErrorAction SilentlyContinue }
}
