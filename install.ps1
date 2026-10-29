$ErrorActionPreference = 'Stop'
if (Get-Process -Name TouchpadHotkey -ErrorAction SilentlyContinue) {
    throw 'Exit the running TouchpadHotkey using its tray menu before installation.'
}
& (Join-Path $PSScriptRoot 'build.ps1')
$taskInstallDir = Join-Path $env:LOCALAPPDATA 'TouchpadHotkey'
New-Item -ItemType Directory -Path $taskInstallDir -Force | Out-Null
$taskExe = Join-Path $taskInstallDir 'TouchpadHotkey.exe'
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin\TouchpadHotkey.exe') -Destination $taskExe -Force
$taskShell = New-Object -ComObject WScript.Shell
$taskLink = $taskShell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('Startup')) 'TouchpadHotkey.lnk'))
$taskLink.TargetPath = $taskExe
$taskLink.WorkingDirectory = $taskInstallDir
$taskLink.Description = 'Touchpad toggle: Ctrl + Alt + Space'
$taskLink.Save()
Start-Process -FilePath $taskExe -WindowStyle Hidden
Write-Output "Installed: $taskExe"
