Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$backendPath = Join-Path $root 'TeamConnect.Api'
$frontendPath = Join-Path $root 'UI'

function Get-ExecutablePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommandName
    )

    $command = Get-Command $CommandName -ErrorAction Stop | Select-Object -First 1

    if ($command.Source) {
        return $command.Source
    }

    if ($command.Path) {
        return $command.Path
    }

    if ($command.Definition) {
        return $command.Definition
    }

    throw "Unable to resolve executable path for $CommandName."
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet is not available in PATH.'
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw 'npm is not available in PATH.'
}

$dotnetExe = Get-ExecutablePath -CommandName 'dotnet'
$npmExe = Get-ExecutablePath -CommandName 'npm'

$backend = $null
$frontend = $null

function Stop-ChildProcesses {
    foreach ($process in @($backend, $frontend)) {
        if ($null -ne $process) {
            try {
                if (-not $process.HasExited) {
                    Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
                }
            } catch {
                # Ignore cleanup errors so shutdown can complete cleanly.
            }
        }
    }
}

$exitHandler = Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action {
    Stop-ChildProcesses
}

Write-Host 'Starting backend...' -ForegroundColor Cyan
$backend = Start-Process -FilePath $dotnetExe -ArgumentList 'watch', 'run', '--launch-profile', 'http' -WorkingDirectory $backendPath -PassThru

Write-Host 'Starting frontend...' -ForegroundColor Cyan
$frontend = Start-Process -FilePath 'cmd.exe' -ArgumentList '/c', "`"$npmExe`" start" -WorkingDirectory $frontendPath -PassThru

Write-Host ''
Write-Host "Backend PID : $($backend.Id)" -ForegroundColor Green
Write-Host "Frontend PID: $($frontend.Id)" -ForegroundColor Green
Write-Host 'Backend URLS:' -ForegroundColor Cyan
Write-Host '  HTTP : http://localhost:5217/swagger/index.html' -ForegroundColor White
Write-Host 'Frontend URL: http://localhost:4200' -ForegroundColor Cyan
Write-Host 'To stop them, close the processes or press Ctrl+C if you are running the script from a console that monitors them.' -ForegroundColor Yellow

try {
    Wait-Process -Id $backend.Id, $frontend.Id
}
finally {
    Stop-ChildProcesses
    if ($exitHandler) {
        Unregister-Event -SourceIdentifier PowerShell.Exiting -ErrorAction SilentlyContinue
    }
}