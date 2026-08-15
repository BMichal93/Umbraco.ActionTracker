[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$ConnectionString,

    [int]$EventCount = 200
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($EventCount -lt 2) {
    throw "EventCount must be at least 2."
}

$repositoryRoot = Split-Path $PSScriptRoot -Parent | Split-Path -Parent
$integrationDirectory = Join-Path $repositoryRoot "tests\SearchPulse.Umbraco.Integration"
$executable = Join-Path $integrationDirectory "bin\Release\net10.0\SearchPulse.Umbraco.Integration.exe"
if (-not (Test-Path $executable)) {
    throw "Build the integration host first: dotnet build tests\SearchPulse.Umbraco.Integration\SearchPulse.Umbraco.Integration.csproj --configuration Release"
}

function Get-FreePort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    try {
        return ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally {
        $listener.Stop()
    }
}

function Wait-ForHost([string]$BaseUrl) {
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri "$BaseUrl/searchpulse-review" -SkipCertificateCheck -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    throw "The integration host at $BaseUrl did not become ready."
}

$firstUrl = "https://localhost:$(Get-FreePort)"
$secondUrl = "https://localhost:$(Get-FreePort)"
$environmentBackup = @{}
foreach ($name in @("ASPNETCORE_ENVIRONMENT", "ASPNETCORE_URLS", "ConnectionStrings__umbracoDbDSN", "ConnectionStrings__umbracoDbDSN_ProviderName")) {
    $environmentBackup[$name] = [Environment]::GetEnvironmentVariable($name, "Process")
}

$firstProcess = $null
$secondProcess = $null
try {
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ConnectionStrings__umbracoDbDSN = $ConnectionString
    $env:ConnectionStrings__umbracoDbDSN_ProviderName = "Microsoft.Data.SqlClient"

    $env:ASPNETCORE_URLS = $firstUrl
    $firstProcess = Start-Process -FilePath $executable -WorkingDirectory $integrationDirectory -PassThru
    Wait-ForHost $firstUrl

    $env:ASPNETCORE_URLS = $secondUrl
    $secondProcess = Start-Process -FilePath $executable -WorkingDirectory $integrationDirectory -PassThru
    Wait-ForHost $secondUrl

    for ($index = 0; $index -lt $EventCount; $index++) {
        $baseUrl = if ($index % 2 -eq 0) { $firstUrl } else { $secondUrl }
        Invoke-WebRequest -Uri "$baseUrl/searchpulse/collect" -Method Post -SkipCertificateCheck -ContentType "application/json" -Headers @{ Origin = $baseUrl; Cookie = "SearchPulseIntegrationConsent=yes" } -Body '{"type":"custom-action","path":"/searchpulse-review/multi-node","target":"sqlserver-multi-node"}' | Out-Null
    }

    $deadline = [DateTime]::UtcNow.AddSeconds(45)
    do {
        $count = [long](Invoke-RestMethod -Uri "$firstUrl/searchpulse-test/event-count" -SkipCertificateCheck -TimeoutSec 5)
        if ($count -eq $EventCount) {
            Write-Host "SQL Server multi-node verification passed: $count of $EventCount accepted events were persisted exactly once."
            return
        }

        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Expected $EventCount persisted events across two nodes, but found $count. Use a fresh empty test database for this script."
}
finally {
    foreach ($process in @($firstProcess, $secondProcess)) {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
        }
    }

    foreach ($name in $environmentBackup.Keys) {
        [Environment]::SetEnvironmentVariable($name, $environmentBackup[$name], "Process")
    }
}