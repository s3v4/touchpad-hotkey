$ErrorActionPreference = 'Stop'
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $taskCompiler)) {
    $taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $taskCompiler)) { throw '.NET Framework C# compiler was not found.' }
$taskBin = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $taskBin -Force | Out-Null
& $taskCompiler /nologo /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$taskBin\TouchpadHotkey.exe" (Join-Path $PSScriptRoot 'src\TouchpadHotkey.cs')
if ($LASTEXITCODE -ne 0) { throw "Build failed: $LASTEXITCODE" }
Write-Output "Built: $taskBin\TouchpadHotkey.exe"
