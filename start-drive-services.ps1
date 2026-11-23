<#
.SYNOPSIS
    Starts the local Drive dependencies (SeaweedFS and the OnlyOffice Document Server) and leaves them running.

.DESCRIPTION
    Both services are checked before anything is started:
      * SeaweedFS  - if the S3 port already answers, the running instance is reused; otherwise every port the
                     server needs (master, volume, filer, S3) must be free, so a second copy can never be
                     started and no port is taken by force.
      * OnlyOffice - if http://localhost:<port>/healthcheck already answers "true", the running install is
                     reused; otherwise the three Windows services are started (this needs an elevated shell).
    Nothing is ever killed and no port is reassigned: a busy port owned by another program is reported and
    the component is skipped.

    Processes this script starts are detached, so they keep running after the PowerShell window closes.
    The API's own SeaweedFS hosted service (Drive:SeaweedFs in appsettings.Development.json) only stops a
    weed.exe that it started itself, so a server started here is not shut down when the API stops.

.PARAMETER SeaweedFsPath
    Full path to weed.exe. Default C:\windows_amd64\weed.exe (Drive:SeaweedFs:ExecutablePath).

.PARAMETER SeaweedDataPath
    SeaweedFS data directory. Default C:\windows_amd64\data\institutionaldiwan
    (Drive:SeaweedFs:DataDirectory). It holds the Drive objects - never delete it.

.PARAMETER OnlyOfficePath
    OnlyOffice Document Server install directory. Default C:\Program Files\ONLYOFFICE\DocumentServer.

.PARAMETER S3Port
    SeaweedFS S3 gateway port used by Drive:Storage:Endpoint. Default 8333.

.PARAMETER VolumePort
    SeaweedFS volume port. Default 8080 - this port belongs to SeaweedFS, not to OnlyOffice.

.PARAMETER FilerPort
    SeaweedFS filer port. Default 8888.

.PARAMETER MasterPort
    SeaweedFS master port. Default 9333.

.PARAMETER OnlyOfficePort
    OnlyOffice proxy (Nginx) port. Default 80.

.PARAMETER TimeoutSeconds
    How long to wait for a started component to answer. Default 60.

.PARAMETER SkipSeaweedFs
    Only check/start OnlyOffice.

.PARAMETER SkipOnlyOffice
    Only check/start SeaweedFS.

.EXAMPLE
    .\start-drive-services.ps1

.EXAMPLE
    .\start-drive-services.ps1 -SeaweedFsPath 'D:\weed\weed.exe' -OnlyOfficePath 'D:\ONLYOFFICE\DocumentServer'

.EXAMPLE
    .\start-drive-services.ps1 -S3Port 8333 -OnlyOfficePort 80 -TimeoutSeconds 120

.NOTES
    No secret is read from or written to this script: the OnlyOffice JWT secret stays in
    config\local.json and in the API's user-secrets.
#>
[CmdletBinding()]
param(
    [string]$SeaweedFsPath = 'C:\windows_amd64\weed.exe',
    [string]$SeaweedDataPath = 'C:\windows_amd64\data\institutionaldiwan',
    [string]$OnlyOfficePath = 'C:\Program Files\ONLYOFFICE\DocumentServer',
    [int]$S3Port = 8333,
    [int]$VolumePort = 8080,
    [int]$FilerPort = 8888,
    [int]$MasterPort = 9333,
    [int]$OnlyOfficePort = 80,
    [int]$TimeoutSeconds = 60,
    [switch]$SkipSeaweedFs,
    [switch]$SkipOnlyOffice
)

$ErrorActionPreference = 'Stop'
$onlyOfficeServices = @('DsProxySvc', 'DsDocServiceSvc', 'DsConverterSvc')
$results = New-Object System.Collections.Generic.List[object]

function Add-Result {
    param([string]$Component, [string]$State, [string]$Detail)
    $colour = switch ($State) {
        'Running' { 'Green' }
        'Started' { 'Green' }
        'Skipped' { 'Yellow' }
        default { 'Red' }
    }
    Write-Host ("[{0}] {1} - {2}" -f $State, $Component, $Detail) -ForegroundColor $colour
    $results.Add([pscustomobject]@{ Component = $Component; State = $State; Detail = $Detail })
}

function Test-TcpPort {
    param([int]$Port)
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $connect = $client.BeginConnect('127.0.0.1', $Port, $null, $null)
        if (-not $connect.AsyncWaitHandle.WaitOne(400)) { return $false }
        $client.EndConnect($connect)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Close()
    }
}

function Get-PortOwner {
    param([int]$Port)
    $ownerPid = $null

    try {
        $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop |
            Select-Object -First 1
        if ($connection) { $ownerPid = $connection.OwningProcess }
    }
    catch {
        # Get-NetTCPConnection is unavailable or refused; fall back to netstat.
    }

    if (-not $ownerPid) {
        $line = & netstat -ano | Select-String -Pattern (":{0}\s+\S+\s+LISTENING" -f $Port) |
            Select-Object -First 1
        if ($line) { $ownerPid = ($line.Line.Trim() -split '\s+')[-1] }
    }

    if (-not $ownerPid) {
        return [pscustomobject]@{ Pid = $null; Name = 'unknown'; Display = 'unknown process' }
    }

    $process = Get-Process -Id $ownerPid -ErrorAction SilentlyContinue
    if ($process) {
        return [pscustomobject]@{
            Pid     = $process.Id
            Name    = $process.ProcessName
            Display = ("PID {0} ({1})" -f $process.Id, $process.ProcessName)
        }
    }

    return [pscustomobject]@{ Pid = $ownerPid; Name = 'unknown'; Display = ("PID {0}" -f $ownerPid) }
}

function Test-Health {
    param([string]$Url)
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
        return ($response.StatusCode -eq 200)
    }
    catch {
        return $false
    }
}

function Wait-Until {
    param([scriptblock]$Condition, [int]$Seconds)
    $deadline = (Get-Date).AddSeconds($Seconds)
    while ((Get-Date) -lt $deadline) {
        if (& $Condition) { return $true }
        Start-Sleep -Milliseconds 500
    }
    return (& $Condition)
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Write-Host ''
Write-Host 'Institutional Diwan - local Drive services' -ForegroundColor Cyan
Write-Host '------------------------------------------' -ForegroundColor Cyan

# ---------------------------------------------------------------------------------------------
# SeaweedFS
# ---------------------------------------------------------------------------------------------
if ($SkipSeaweedFs) {
    Add-Result 'SeaweedFS' 'Skipped' 'skipped with -SkipSeaweedFs'
}
else {
    $s3Url = "http://127.0.0.1:$S3Port"

    if (Test-TcpPort $S3Port) {
        $owner = Get-PortOwner $S3Port
        if ($owner.Name -eq 'weed') {
            Add-Result 'SeaweedFS' 'Running' ("S3 port {0} already answers on {1} - owned by {2}; nothing was started." -f `
                    $S3Port, $s3Url, $owner.Display)
        }
        else {
            Add-Result 'SeaweedFS' 'Failed' ("Port {0} already answers but it is held by {1}, not weed.exe. SeaweedFS is not running; free the port or rerun with -S3Port <free port>." -f `
                    $S3Port, $owner.Display)
        }
    }
    elseif (-not (Test-Path -LiteralPath $SeaweedFsPath)) {
        Add-Result 'SeaweedFS' 'Failed' ("weed.exe was not found at '{0}'. Pass the real path with -SeaweedFsPath." -f $SeaweedFsPath)
    }
    elseif (@(Get-Process -Name 'weed' -ErrorAction SilentlyContinue).Count -gt 0) {
        $running = @(Get-Process -Name 'weed' -ErrorAction SilentlyContinue | ForEach-Object { $_.Id }) -join ','
        Add-Result 'SeaweedFS' 'Failed' ("weed.exe is already running (PID {0}) but not on S3 port {1}. This weed build also takes fixed auxiliary ports (Iceberg 8181, Lance 9101), so a second instance cannot start - stop that process, or pass the ports it actually uses." -f `
                $running, $S3Port)
    }
    else {
        $weedPorts = @(
            [pscustomobject]@{ Name = 'master'; Port = $MasterPort },
            [pscustomobject]@{ Name = 'volume'; Port = $VolumePort },
            [pscustomobject]@{ Name = 'filer'; Port = $FilerPort },
            [pscustomobject]@{ Name = 'S3'; Port = $S3Port }
        )

        $clashes = @($weedPorts | Where-Object { Test-TcpPort $_.Port })
        if ($clashes.Count -gt 0) {
            $details = ($clashes | ForEach-Object {
                    "port $($_.Port) ($($_.Name)) held by $((Get-PortOwner $_.Port).Display)"
                }) -join '; '
            $flag = '-' + $clashes[0].Name.Substring(0, 1).ToUpperInvariant() + $clashes[0].Name.Substring(1) + 'Port'
            Add-Result 'SeaweedFS' 'Failed' ("Not started to avoid a port clash: {0}. Nothing was stopped; free those ports, or pass the ports your running instance uses (for example {1})." -f `
                    $details, $flag)
        }
        else {
            if (-not (Test-Path -LiteralPath $SeaweedDataPath)) {
                New-Item -ItemType Directory -Path $SeaweedDataPath -Force | Out-Null
                Write-Host "Created data directory $SeaweedDataPath" -ForegroundColor DarkGray
            }

            $logPath = Join-Path $env:TEMP 'diwan-seaweedfs.log'
            $weedArgs = @(
                'server',
                "--dir=$SeaweedDataPath",
                '--ip=127.0.0.1',
                '--ip.bind=127.0.0.1',
                "--master.port=$MasterPort",
                "--volume.port=$VolumePort",
                '--filer=true',
                "--filer.port=$FilerPort",
                '--s3',
                '--s3.ip.bind=127.0.0.1',
                "--s3.port=$S3Port"
            )

            try {
                $weed = Start-Process -FilePath $SeaweedFsPath -ArgumentList $weedArgs -PassThru `
                    -WindowStyle Hidden -RedirectStandardOutput $logPath -RedirectStandardError "$logPath.err"
            }
            catch {
                Add-Result 'SeaweedFS' 'Failed' ("weed.exe could not be started: {0}" -f $_.Exception.Message)
                $weed = $null
            }

            if ($weed) {
                $ready = Wait-Until { Test-TcpPort $S3Port } $TimeoutSeconds
                if ($ready) {
                    Add-Result 'SeaweedFS' 'Started' ("PID {0}; S3 {1}, filer {2}, master {3}, volume {4}; data {5}; log {6}" -f `
                            $weed.Id, $s3Url, $FilerPort, $MasterPort, $VolumePort, $SeaweedDataPath, $logPath)
                }
                else {
                    Add-Result 'SeaweedFS' 'Failed' ("PID {0} did not open S3 port {1} within {2}s. See {3} and {3}.err" -f `
                            $weed.Id, $S3Port, $TimeoutSeconds, $logPath)
                }
            }
        }
    }
}

# ---------------------------------------------------------------------------------------------
# OnlyOffice Document Server
# ---------------------------------------------------------------------------------------------
if ($SkipOnlyOffice) {
    Add-Result 'OnlyOffice' 'Skipped' 'skipped with -SkipOnlyOffice'
}
else {
    $healthUrl = "http://localhost:$OnlyOfficePort/healthcheck"

    if (Test-Health $healthUrl) {
        Add-Result 'OnlyOffice' 'Running' ("{0} already answers true on port {1}; nothing was started." -f $healthUrl, $OnlyOfficePort)
    }
    elseif (Test-TcpPort $OnlyOfficePort) {
        Add-Result 'OnlyOffice' 'Failed' ("Port {0} is held by {1}, but {2} does not answer true. Resolve that port before starting OnlyOffice." -f `
                $OnlyOfficePort, (Get-PortOwner $OnlyOfficePort).Display, $healthUrl)
    }
    else {
        if (-not (Test-Path -LiteralPath $OnlyOfficePath)) {
            Write-Host ("Install directory '{0}' was not found; relying on the registered services." -f $OnlyOfficePath) -ForegroundColor Yellow
        }

        $services = @(Get-Service -Name $onlyOfficeServices -ErrorAction SilentlyContinue)
        if ($services.Count -eq 0) {
            Add-Result 'OnlyOffice' 'Failed' ("None of {0} is installed. Install the Document Server or pass -OnlyOfficePath." -f ($onlyOfficeServices -join ', '))
        }
        else {
            $started = @()
            $failures = @()
            foreach ($name in $onlyOfficeServices) {
                $service = $services | Where-Object { $_.Name -eq $name }
                if (-not $service) {
                    $failures += "$name is not installed"
                    continue
                }
                if ($service.Status -eq 'Running') { continue }

                try {
                    Start-Service -Name $name -ErrorAction Stop
                    $started += $name
                }
                catch {
                    $failures += "$name ($($_.Exception.Message))"
                }
            }

            if ($failures.Count -gt 0) {
                $hint = if (Test-IsAdministrator) { '' } else { ' Run this script from an elevated PowerShell (Run as Administrator).' }
                Add-Result 'OnlyOffice' 'Failed' ("Could not start: {0}.{1}" -f ($failures -join '; '), $hint)
            }
            elseif ($started.Count -eq 0) {
                # Services already running but the proxy is still warming up.
                $ready = Wait-Until { Test-Health $healthUrl } $TimeoutSeconds
                if ($ready) {
                    Add-Result 'OnlyOffice' 'Running' ("Services were already running; {0} answers true." -f $healthUrl)
                }
                else {
                    Add-Result 'OnlyOffice' 'Failed' ("Services are running but {0} did not answer true within {1}s." -f $healthUrl, $TimeoutSeconds)
                }
            }
            else {
                $ready = Wait-Until { Test-Health $healthUrl } $TimeoutSeconds
                if ($ready) {
                    Add-Result 'OnlyOffice' 'Started' ("Started {0}; {1} answers true on port {2}." -f ($started -join ', '), $healthUrl, $OnlyOfficePort)
                }
                else {
                    Add-Result 'OnlyOffice' 'Failed' ("Started {0}, but {1} did not answer true within {2}s. Check the 'Ds*' services and the Nginx log under {3}\Logs." -f `
                            ($started -join ', '), $healthUrl, $TimeoutSeconds, $OnlyOfficePath)
                }
            }
        }
    }
}

# ---------------------------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------------------------
Write-Host ''
$results | Format-Table -AutoSize -Wrap | Out-String | Write-Host

$failed = @($results | Where-Object { $_.State -eq 'Failed' })
if ($failed.Count -eq 0) {
    Write-Host 'Both Drive dependencies are up. Start the API with: dotnet run --project InstitutionalDiwan.Server' -ForegroundColor Green
    exit 0
}

Write-Host ("{0} component(s) need attention - see the rows above." -f $failed.Count) -ForegroundColor Red
exit 1
