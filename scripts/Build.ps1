param([switch]$SingleFile)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    $out = Join-Path $root 'artifacts\app'
    $arguments = @('publish','source/Ncnc.App/Ncnc.App.csproj','-c','Release','-r','win-x64','--self-contained','true','-p:RuntimeFrameworkVersion=8.0.32','-p:Version=1.2.0','-o',$out)
    if ($SingleFile) { $arguments += @('-p:PublishSingleFile=true','-p:IncludeNativeLibrariesForSelfExtract=true') }
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed: $LASTEXITCODE" }
    Copy-Item -LiteralPath (Join-Path $root 'reviewed.json') -Destination $out
    Write-Output "Application built in $out. Toolchain is not bundled by this script."
} finally { Pop-Location }
