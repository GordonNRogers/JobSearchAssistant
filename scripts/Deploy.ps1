
#requires -RunAsAdministrator

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$Install
)

$ErrorActionPreference = "Stop"

$ServiceName = "JobSearchAssistant"
$ExecutableName = "JobSearchAssistant.Web.exe"
$InstallDir = Join-Path $env:ProgramFiles "JobSearchAssistant"
$DataDir = Join-Path $env:ProgramData "JobSearchAssistant"
$DatabasePath = Join-Path $DataDir "jobsearch.db"
$DatabaseBackupDir = Join-Path $DataDir "Backups"
$ServiceUrl = "http://127.0.0.1:5117/"
$ProjectFile = "JobSearchAssistant.Web\JobSearchAssistant.Web.csproj"
$SolutionFile = "JobSearchAssistant.sln"
$DatabaseToolProject = "JobSearchAssistant.DatabaseTool\JobSearchAssistant.DatabaseTool.csproj"
$DatabaseBackupPath = $null

$RepoRoot = Split-Path -Parent $PSScriptRoot
$DeployRoot = Join-Path $RepoRoot ".deploy"
$PublishDir = Join-Path $DeployRoot "publish"
$BackupDir = Join-Path $DeployRoot "backup"
$DatabaseToolDll = Join-Path $RepoRoot "JobSearchAssistant.DatabaseTool\bin\$Configuration\net11.0\JobSearchAssistant.DatabaseTool.dll"

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE."
    }
}

function Wait-ForService {
    param(
        [ValidateSet("Running", "Stopped")]
        [string]$Status,

        [int]$TimeoutSeconds = 30
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    do {
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

        if ($service -and $service.Status.ToString() -eq $Status) {
            return
        }

        Start-Sleep -Seconds 1
    } while ((Get-Date) -lt $deadline)

    throw "Service '$ServiceName' did not reach status '$Status' within $TimeoutSeconds seconds."
}

function Backup-Database {
    if (-not (Test-Path $DatabasePath -PathType Leaf)) {
        if ($Install) {
            Write-Host "No existing database to back up (first installation)."
            return
        }

        throw "Database not found: $DatabasePath. Refusing to deploy without a database backup."
    }

    New-Item -ItemType Directory -Force $DatabaseBackupDir | Out-Null

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss-fff"
    $backupPath = Join-Path $DatabaseBackupDir "jobsearch-$timestamp.db"

    Invoke-CheckedCommand "dotnet" @(
        "exec",
        $DatabaseToolDll,
        "backup",
        $DatabasePath,
        $backupPath
    )

    if (-not (Test-Path $backupPath -PathType Leaf)) {
        throw "Database backup tool reported success, but the backup file was not found: $backupPath"
    }

    $script:DatabaseBackupPath = $backupPath

    Write-Host "Database backup verified: $backupPath" -ForegroundColor Green
}

function Test-DeploymentHealth {
    $service = Get-Service -Name $ServiceName

    if ($service.Status -ne "Running") {
        throw "Service is not running."
    }

    $listener = Get-NetTCPConnection -LocalPort 5117 -State Listen -ErrorAction SilentlyContinue

    if (-not $listener -or
        @($listener | Where-Object { $_.LocalAddress -ne "127.0.0.1" }).Count -gt 0) {
        throw "Expected the application to listen only on 127.0.0.1:5117."
    }

    $response = Invoke-WebRequest -Uri $ServiceUrl -UseBasicParsing -TimeoutSec 15

    if ($response.StatusCode -ne 200) {
        throw "Health check returned HTTP $($response.StatusCode), expected 200."
    }
}

Push-Location $RepoRoot

try {
    Write-Host "Repository: $RepoRoot" -ForegroundColor Cyan

    if (-not (Test-Path $SolutionFile)) {
        throw "Solution file not found: $SolutionFile"
    }

    if (-not (Test-Path $ProjectFile)) {
        throw "Web project not found: $ProjectFile"
    }

    $existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

    if ($Install) {
        if ($existingService) {
            throw "Service '$ServiceName' already exists. Omit -Install for a normal deployment."
        }
    }
    elseif (-not $existingService) {
        throw "Service '$ServiceName' does not exist. Use -Install for first-time installation."
    }

    # Build and test before stopping the running application.
    Write-Host "`nBuilding solution..." -ForegroundColor Cyan
    Invoke-CheckedCommand "dotnet" @(
        "build", $SolutionFile,
        "--configuration", $Configuration
    )

    Write-Host "`nRunning tests..." -ForegroundColor Cyan
    Invoke-CheckedCommand "dotnet" @(
        "test", $SolutionFile,
        "--configuration", $Configuration,
        "--no-build"
    )

    Write-Host "`nBuilding database utility..." -ForegroundColor Cyan
    Invoke-CheckedCommand "dotnet" @(
        "build", $DatabaseToolProject,
        "--configuration", $Configuration
    )

    if (-not (Test-Path $DatabaseToolDll -PathType Leaf)) {
        throw "Database utility was not built: $DatabaseToolDll"
    }

    # Prepare a clean staging directory.
    Write-Host "`nPublishing application..." -ForegroundColor Cyan

    New-Item -ItemType Directory -Force $DeployRoot | Out-Null

    if (Test-Path $PublishDir) {
        Remove-Item $PublishDir -Recurse -Force
    }

    New-Item -ItemType Directory -Force $PublishDir | Out-Null

    Invoke-CheckedCommand "dotnet" @(
        "publish", $ProjectFile,
        "--configuration", $Configuration,
        "--output", $PublishDir
    )

    $PublishedExecutable = Join-Path $PublishDir $ExecutableName

    if (-not (Test-Path $PublishedExecutable)) {
        throw "Published executable not found: $PublishedExecutable"
    }

    # The publish directory must not be inside the installation directory.
    $installFullPath = [IO.Path]::GetFullPath($InstallDir)
    $publishFullPath = [IO.Path]::GetFullPath($PublishDir)

    if ($publishFullPath.StartsWith(
        $installFullPath,
        [StringComparison]::OrdinalIgnoreCase
    )) {
        throw "Publish directory must not be inside the installation directory."
    }

    # First-time installation setup.
    if ($Install) {
        Write-Host "`nPreparing first-time installation..." -ForegroundColor Cyan

        New-Item -ItemType Directory -Force $InstallDir | Out-Null
        New-Item -ItemType Directory -Force $DataDir | Out-Null

        # Do not overwrite or delete an existing database.
        Invoke-CheckedCommand "icacls.exe" @(
            $DataDir,
            "/grant",
            "NT AUTHORITY\LOCAL SERVICE:(OI)(CI)M"
        )

        $servicePath = "`"$InstallDir\$ExecutableName`""

        Invoke-CheckedCommand "sc.exe" @(
            "create", $ServiceName,
            "binPath=", $servicePath,
            "start=", "auto",
            "obj=", "NT AUTHORITY\LocalService"
        )
    }

    # Back up the existing application files before replacing them.
    $hadExistingInstall = Test-Path (Join-Path $InstallDir $ExecutableName)

    if (Test-Path $BackupDir) {
        Remove-Item $BackupDir -Recurse -Force
    }

    if ($hadExistingInstall) {
        New-Item -ItemType Directory -Force $BackupDir | Out-Null

        & robocopy.exe $InstallDir $BackupDir /MIR /R:2 /W:2 /NFL /NDL /NP
        if ($LASTEXITCODE -ge 8) {
            throw "Could not back up the existing installation. Robocopy exit code: $LASTEXITCODE"
        }
    }

    $serviceStopped = $false
    $deploymentSucceeded = $false

    try {
        Write-Host "`nStopping service..." -ForegroundColor Cyan

        Stop-Service -Name $ServiceName -Force
        Wait-ForService -Status "Stopped"
        $serviceStopped = $true

        Write-Host "`nBacking up database..." -ForegroundColor Cyan
        Backup-Database

        Write-Host "`nCopying published files..." -ForegroundColor Cyan

        & robocopy.exe $PublishDir $InstallDir /MIR /R:2 /W:2 /NFL /NDL /NP
        if ($LASTEXITCODE -ge 8) {
            throw "File deployment failed. Robocopy exit code: $LASTEXITCODE"
        }

        # Configure automatic restart after service failure.
        Invoke-CheckedCommand "sc.exe" @(
            "failure", $ServiceName,
            "reset=", "86400",
            "actions=", "restart/5000/restart/15000/restart/60000"
        )

        Write-Host "`nStarting service..." -ForegroundColor Cyan

        Start-Service -Name $ServiceName
        Wait-ForService -Status "Running"

        Write-Host "`nChecking application health..." -ForegroundColor Cyan
        Test-DeploymentHealth

        $deploymentSucceeded = $true
    }
    finally {
        if (-not $deploymentSucceeded -and $serviceStopped) {
            Write-Warning "Deployment failed. Attempting to restore the previous application and database."

            $rollbackSuccessful = $true

            if (-not $hadExistingInstall) {
                Write-Warning "No previous application installation exists. Automatic rollback is not possible."
                $rollbackSuccessful = $false
            }

            # Ensure the application is stopped before touching its database.
            try {
                $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

                if ($service -and $service.Status -ne "Stopped") {
                    Stop-Service -Name $ServiceName -Force
                    Wait-ForService -Status "Stopped"
                }
            }
            catch {
                Write-Warning "Could not stop the service for rollback: $_"
                $rollbackSuccessful = $false
            }

            # Restore the pre-deployment database before restarting the old application.
            if ($rollbackSuccessful -and $DatabaseBackupPath) {
                try {
                    Write-Host "`nRestoring database backup..." -ForegroundColor Cyan

                    Invoke-CheckedCommand "dotnet" @(
                        "exec",
                        $DatabaseToolDll,
                        "restore",
                        $DatabaseBackupPath,
                        $DatabasePath
                    )
                }
                catch {
                    Write-Warning "Database rollback failed: $_"
                    $rollbackSuccessful = $false
                }
            }

            # Restore the previous application binaries.
            if ($rollbackSuccessful -and $hadExistingInstall -and (Test-Path $BackupDir)) {
                try {
                    Write-Host "`nRestoring previous application files..." -ForegroundColor Cyan

                    & robocopy.exe $BackupDir $InstallDir /MIR /R:2 /W:2 /NFL /NDL /NP

                    if ($LASTEXITCODE -ge 8) {
                        throw "Robocopy exit code: $LASTEXITCODE"
                    }
                }
                catch {
                    Write-Warning "Application rollback failed: $_"
                    $rollbackSuccessful = $false
                }
            }

            if ($rollbackSuccessful) {
                try {
                    Start-Service -Name $ServiceName
                    Wait-ForService -Status "Running" -TimeoutSeconds 30
                    Test-DeploymentHealth

                    Write-Warning "Previous application and database are running again."
                }
                catch {
                    Write-Warning "Could not verify service recovery: $_"

                    try {
                        Stop-Service -Name $ServiceName -Force -ErrorAction Stop
                        Wait-ForService -Status "Stopped"
                    }
                    catch {
                        Write-Warning "Could not stop the unhealthy service: $_"
                    }
                }
            }
            else {
                Write-Warning "Rollback was incomplete. The service has been left stopped to avoid running an incompatible application/database combination."
            }
        }    
    }

    if (-not $deploymentSucceeded) {
        throw "Deployment did not complete successfully."
    }

    Write-Host "`nDeployment completed successfully." -ForegroundColor Green
    Write-Host "Application: $ServiceUrl"
    Write-Host "Database:    $DatabasePath"
}
finally {
    Pop-Location
}
