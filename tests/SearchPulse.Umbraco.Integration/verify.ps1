[CmdletBinding()]
param(
    [string]$DotnetPath = "dotnet",
    [switch]$ResetDatabase
)

$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot
$databasePath = Join-Path $projectRoot "umbraco/Data/Umbraco.sqlite.db"
$dataProtectionKeysPath = Join-Path $projectRoot "umbraco/Data/SearchPulseIntegration-DataProtection-Keys"
$logPath = Join-Path $projectRoot "host.log"
$errorLogPath = Join-Path $projectRoot "host.error.log"
$endpoint = "http://127.0.0.1:5109/searchpulse/collect"
$origin = "http://127.0.0.1:5109"
$payload = Get-Content (Join-Path $projectRoot "fixtures/page-view.json") -Raw
$backofficeUrl = "$origin/umbraco"
$overviewApiUrl = "$origin/umbraco/management/api/v1/searchpulse/overview"
$overviewScriptUrl = "$origin/App_Plugins/SearchPulse/searchpulse-overview.js"
$hostProcess = $null
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT

# Some Windows hosts expose both PATH and Path. Start-Process treats them as
# separate dictionary keys, while Windows does not. Normalize the child path.
$effectivePath = $env:Path
Remove-Item Env:PATH -ErrorAction SilentlyContinue
$env:Path = $effectivePath

function Remove-TestDatabase {
    foreach ($path in @("$databasePath", "$databasePath-shm", "$databasePath-wal")) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }

    if (Test-Path -LiteralPath $dataProtectionKeysPath) {
        Remove-Item -LiteralPath $dataProtectionKeysPath -Recurse -Force
    }
}

function Wait-ForHost {
    for ($attempt = 1; $attempt -le 60; $attempt++) {
        try {
            $statusCode = Send-CollectionRequest -RequestOrigin $origin -HasConsent $false
            if ($statusCode -eq 204) {
                return
            }
        }
        catch {
            # The host has not opened its listener yet.
        }

        Start-Sleep -Seconds 1
    }

    throw "The Umbraco integration host did not start within 60 seconds."
}

function Send-CollectionRequest {
    param(
        [string]$RequestOrigin,
        [bool]$HasConsent
    )

    $headers = @{ Origin = $RequestOrigin }
    $webSession = $null
    if ($HasConsent) {
        $webSession = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
        $webSession.Cookies.Add([System.Net.Cookie]::new("SearchPulseIntegrationConsent", "yes", "/", "127.0.0.1"))
    }

    try {
        $requestParameters = @{
            Uri = $endpoint
            Method = "Post"
            ContentType = "application/json"
            Headers = $headers
            Body = $payload
            UseBasicParsing = $true
        }

        if ($null -ne $webSession) {
            $requestParameters.WebSession = $webSession
        }

        $response = Invoke-WebRequest @requestParameters

        # Windows PowerShell returns $null for a successful response without a body.
        if ($null -eq $response) {
            return 204
        }

        return [int]$response.StatusCode
    }
    catch {
        if ($null -ne $_.Exception.Response) {
            return [int]$_.Exception.Response.StatusCode
        }

        throw
    }
}

function Get-StatusCode {
    param([string]$Uri)
    try {
        $response = Invoke-WebRequest -Uri $Uri -UseBasicParsing
        return [int]$response.StatusCode
    }
    catch {
        if ($null -ne $_.Exception.Response) {
            return [int]$_.Exception.Response.StatusCode
        }
        throw
    }
}

try {
    if ($ResetDatabase) {
        Remove-TestDatabase
    }

    Remove-Item -LiteralPath $logPath, $errorLogPath -Force -ErrorAction SilentlyContinue
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $hostProcess = Start-Process -FilePath $DotnetPath -ArgumentList @("run", "--configuration", "Release", "--no-build", "--no-launch-profile", "--urls", "http://127.0.0.1:5109") -WorkingDirectory $projectRoot -RedirectStandardOutput $logPath -RedirectStandardError $errorLogPath -PassThru

    Wait-ForHost

    $withoutConsent = Send-CollectionRequest -RequestOrigin $origin -HasConsent $false
    $withConsent = Send-CollectionRequest -RequestOrigin $origin -HasConsent $true
    $crossOrigin = Send-CollectionRequest -RequestOrigin "https://untrusted.example" -HasConsent $true

    if ($withoutConsent -ne 204 -or $withConsent -ne 202 -or $crossOrigin -ne 400) {
        throw "Unexpected endpoint results: no consent=$withoutConsent, consent=$withConsent, cross-origin=$crossOrigin."
    }

    $backoffice = Invoke-WebRequest -Uri $backofficeUrl -UseBasicParsing
    $overviewScript = Invoke-WebRequest -Uri $overviewScriptUrl -UseBasicParsing
    $overviewApiStatus = Get-StatusCode -Uri $overviewApiUrl
    if ($backoffice.StatusCode -ne 200) {
        throw "The Umbraco backoffice did not load. Status=$($backoffice.StatusCode)."
    }
    if ($overviewScript.StatusCode -ne 200 -or $overviewScript.Content -notmatch "Popular interactions" -or $overviewScript.Content -notmatch "generatedAtUtc") {
        throw "The packaged SearchPulse overview did not include the interactions and freshness UI."
    }
    if ($overviewApiStatus -ne 401) {
        throw "The SearchPulse overview API was not protected as a backoffice endpoint. Status=$overviewApiStatus."
    }
    $migrationLog = Get-Content -LiteralPath $logPath -Raw
    if ($migrationLog -notmatch '"target" TEXT COLLATE NOCASE NULL') {
        throw "The SearchPulse migration did not create a nullable target column."
    }

    Write-Output "SearchPulse integration verification passed: migration, consent, origin, backoffice assets, and API authorization boundaries are correct."
}
finally {
    if ($null -ne $hostProcess -and -not $hostProcess.HasExited) {
        Stop-Process -Id $hostProcess.Id -Force
    }

    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
}
