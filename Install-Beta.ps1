param(
    [switch]$ElevatedRelaunch
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $PSCommandPath
$LogFile = Join-Path $ScriptDir "Install-Beta.log"
$TranscriptFile = Join-Path $ScriptDir "Install-Beta-Transcript.log"
$TranscriptStarted = $false
$InstallSucceeded = $false

function Write-Log {
    param([string]$Message)

    $line = "{0} {1}{2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"), $Message, [Environment]::NewLine
    try {
        [System.IO.File]::AppendAllText($LogFile, $line, [System.Text.Encoding]::UTF8)
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
        $elevatedProcess = Start-Process powershell.exe `
            -Verb RunAs `
            -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -ElevatedRelaunch" `
            -Wait `
            -PassThru

        if ($elevatedProcess.ExitCode -eq 0) {
            Write-Log "Elevation relaunch succeeded and installation completed."
            exit 0
        }

        Write-Host "Elevated installer exited with code $($elevatedProcess.ExitCode)." -ForegroundColor Red
        Write-Log "Elevated installer failed with exit code $($elevatedProcess.ExitCode)."
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
        Start-Transcript -Path $TranscriptFile -Append -ErrorAction Stop | Out-Null
        $TranscriptStarted = $true
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

    try {
        Start-Process explorer.exe "shell:appsFolder\Microsoft.XboxGamingOverlay_8wekyb3d8bbwe!App" | Out-Null
        Write-Log "Xbox Game Bar launch requested."
    }
    catch {
        Write-Log ("Xbox Game Bar launch failed: " + $_.Exception.Message)
    }

    $InstallSucceeded = $true
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
    if ($TranscriptStarted) {
        try {
            Stop-Transcript | Out-Null
        }
        catch {
            # Ignore transcript stop errors.
        }
    }

    Write-Host ""
    Write-Host "Log file:"
    Write-Host (Join-Path (Split-Path -Parent $PSCommandPath) "Install-Beta.log")

    if (-not $InstallSucceeded) {
        Read-Host "Press Enter to close"
        exit 1
    }

    exit 0
}
