param(
    [int]$Port = 5051
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

$envPath = Join-Path $repoRoot ".env"
if (Test-Path $envPath) {
    Get-Content $envPath | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith("#") -or -not $line.Contains("=")) {
            return
        }

        $parts = $line.Split("=", 2)
        $name = $parts[0].Trim()
        $value = $parts[1].Trim().Trim("'").Trim('"')

        if ($name -and -not [Environment]::GetEnvironmentVariable($name, "Process")) {
            [Environment]::SetEnvironmentVariable($name, $value, "Process")
        }
    }
}

$ucuDbHost = if ($env:UCU_DB_HOST) { $env:UCU_DB_HOST } else { "mysql.reto-ucu.net" }
$ucuDbPort = if ($env:UCU_DB_PORT) { $env:UCU_DB_PORT } else { "50006" }
$ucuDbName = if ($env:UCU_DB_NAME) { $env:UCU_DB_NAME } else { "XR_Grupo4" }
$ucuDbUser = if ($env:UCU_DB_USER) { $env:UCU_DB_USER } else { "xr_g4_admin" }

if (-not $env:UCU_DB_PASSWORD) {
    $securePassword = Read-Host "Password de $ucuDbUser@$ucuDbHost" -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try {
        $env:UCU_DB_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

$env:DB_CONNECTION_STRING = "Server=$ucuDbHost;Port=$ucuDbPort;Database=$ucuDbName;User ID=$ucuDbUser;Password=$env:UCU_DB_PASSWORD"

$usedPort = Get-NetTCPConnection -LocalAddress 127.0.0.1 -LocalPort $Port -ErrorAction SilentlyContinue
if ($usedPort) {
    Write-Error "El puerto $Port ya esta en uso. Cerra el otro server o ejecuta: powershell -ExecutionPolicy Bypass -File .\scripts\run-ucu.ps1 -Port 5051"
    exit 1
}

Write-Host ">> Base: UCU ($ucuDbUser@$ucuDbHost`:$ucuDbPort/$ucuDbName)"
dotnet run --project server --no-launch-profile --urls "http://localhost:$Port"
