$ErrorActionPreference = 'Stop'
$app = Join-Path $PSScriptRoot 'app\NightCityNameChanger.exe'
if (!(Test-Path -LiteralPath $app)) { throw 'Keep this script next to the app folder.' }
$programs = [Environment]::GetFolderPath('Programs')
$shell = New-Object -ComObject WScript.Shell
$link = $shell.CreateShortcut((Join-Path $programs 'NcNc - Night City Name Changer.lnk'))
$link.TargetPath = $app
$link.WorkingDirectory = Split-Path $app
$link.IconLocation = "$app,0"
$link.Description = 'Rename Cyberpunk 2077 protagonist text'
$link.Save()
Write-Output 'Start menu shortcut created: NcNc - Night City Name Changer'
