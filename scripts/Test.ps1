$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    & dotnet run --project source/Ncnc.Tests/Ncnc.Tests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw "Test runner failed: $LASTEXITCODE" }
} finally { Pop-Location }
