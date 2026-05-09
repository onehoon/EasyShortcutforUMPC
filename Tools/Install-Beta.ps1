param(
    [switch]$ElevatedRelaunch
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $PSCommandPath
$LogFile = Join-Path $ScriptDir "Install-Beta.log"

function Write-Log {
    param([string]$Message)

    $line = "{0} {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"), $Message
    try {
        Add-Content -Path $LogFile -Value $line -Encoding UTF8
    }
    catch {
        # Ignore file logging failures.
    }
}

function Test-IsAdmin {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Wait-AndExit {
    Write-Host ""
    Read-Host "Press Enter to close"
}

Write-Log "Installer start. ElevatedRelaunch=$ElevatedRelaunch"

if (-not (Test-IsAdmin)) {
    try {
        Write-Host "Restarting as Administrator..."
        Write-Log "Attempting elevation relaunch."
        Start-Process powershell.exe `
            -Verb RunAs `
            -ArgumentList "-NoProfile -ExecutionPolicy Bypass -NoExit -File `"$PSCommandPath`" -ElevatedRelaunch"
        Write-Host "An elevated installer window was opened."
        Write-Log "Elevation relaunch succeeded."
    }
    catch {
        Write-Host "Failed to relaunch as Administrator:" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        Write-Log ("Elevation relaunch failed: " + $_.Exception.Message)
    }

    Wait-AndExit
    exit
}

try {
    Write-Log "Running with admin privileges."

    try {
        Start-Transcript -Path $LogFile -Append | Out-Null
        Write-Log "Transcript started."
    }
    catch {
        Write-Log ("Transcript start failed: " + $_.Exception.Message)
    }

    Write-Host "Install folder:"
    Write-Host $ScriptDir
    Write-Host ""

    $CerFile = Get-ChildItem -Path $ScriptDir -Filter "*.cer" -File | Select-Object -First 1
    $PackageFile = Get-ChildItem -Path $ScriptDir -File |
        Where-Object { $_.Extension.ToLowerInvariant() -in @('.msixbundle', '.appxbundle', '.msix', '.appx') } |
        Select-Object -First 1

    if (-not $CerFile) {
        throw "Certificate file (.cer) was not found in the install folder."
    }

    if (-not $PackageFile) {
        throw "App package file (.msixbundle/.appxbundle/.msix/.appx) was not found in the install folder."
    }

    Write-Host "Certificate:"
    Write-Host $CerFile.FullName
    Write-Host ""

    Write-Host "Package:"
    Write-Host $PackageFile.FullName
    Write-Host ""

    Write-Host "Installing certificate to LocalMachine\TrustedPeople..."
    Write-Log "Installing certificate."
    Import-Certificate `
        -FilePath $CerFile.FullName `
        -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" | Out-Null

    Write-Host "Installing app package..."
    Write-Log "Installing package."
    Add-AppxPackage -Path $PackageFile.FullName

    Write-Host ""
    Write-Host "Installation completed."
    Write-Host "Open Xbox Game Bar with Win + G, then open Easy Shortcut for UMPC from the widget list."
    Write-Log "Installation completed successfully."
}
catch {
    Write-Host ""
    Write-Host "Installation failed:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Log ("Installation failed: " + $_.Exception.Message)
    if ($_.ScriptStackTrace) {
        Write-Host ""
        Write-Host "Stack trace:" -ForegroundColor Red
        Write-Host $_.ScriptStackTrace -ForegroundColor Red
        Write-Log ("Stack trace: " + $_.ScriptStackTrace)
    }
}
finally {
    try {
        Stop-Transcript | Out-Null
    }
    catch {
        # Ignore transcript stop errors.
    }

    Write-Host ""
    Write-Host "Log file:"
    Write-Host (Join-Path (Split-Path -Parent $PSCommandPath) "Install-Beta.log")
    Read-Host "Press Enter to close"
}
