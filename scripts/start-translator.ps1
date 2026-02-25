[CmdletBinding()]
param(
  [int] $DockerReadyTimeoutSeconds = 180,
  [string] $ProjectName = 'translator'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function New-LogFilePath {
  $dir = Join-Path $env:LOCALAPPDATA 'translator\logs'
  New-Item -ItemType Directory -Force -Path $dir | Out-Null
  return (Join-Path $dir 'startup.log')
}

$LogFile = New-LogFilePath

function Log([string] $Message) {
  $line = "[{0}] {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Message
  $line | Tee-Object -FilePath $LogFile -Append | Out-Null
}

try {
  $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
  $composeFile = Join-Path $repoRoot 'docker-compose.yml'

  Log "Starting translator stack. RepoRoot=$repoRoot"

  if (-not (Test-Path -LiteralPath $composeFile)) {
    throw "Compose file not found: $composeFile"
  }

  $docker = Get-Command docker -ErrorAction Stop
  Log "Docker CLI: $($docker.Source)"

  $deadline = (Get-Date).AddSeconds($DockerReadyTimeoutSeconds)
  while ($true) {
    try {
      & docker info | Out-Null
      break
    } catch {
      if ((Get-Date) -ge $deadline) {
        throw "Docker engine not ready after ${DockerReadyTimeoutSeconds}s. Make sure Docker Desktop starts on login."
      }
      Start-Sleep -Seconds 3
    }
  }

  Push-Location $repoRoot
  try {
    Log "Running: docker compose -p $ProjectName up -d"
    & docker compose -p $ProjectName -f $composeFile up -d
    if ($LASTEXITCODE -ne 0) { throw "docker compose up failed with exit code $LASTEXITCODE" }
    Log "Translator stack is up."
  } finally {
    Pop-Location
  }

  exit 0
} catch {
  Log ("ERROR: " + $_.Exception.Message)
  exit 1
}

