[CmdletBinding()]
param(
  [string] $TaskName = 'Translator - start docker compose',
  [int] $DelaySeconds = 20,

  # By default we register without requiring admin rights.
  # If you really need highest privileges, run PowerShell as Administrator and pass -RunLevel Highest.
  [ValidateSet('Limited', 'Highest')]
  [string] $RunLevel = 'Limited'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$userId = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$startScript = Join-Path $PSScriptRoot 'start-translator.ps1'

if (-not (Test-Path -LiteralPath $startScript)) {
  throw "Missing script: $startScript"
}

$powershellExe = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
if (-not (Test-Path -LiteralPath $powershellExe)) {
  $powershellExe = 'powershell.exe'
}

$args = @(
  '-NoLogo',
  '-NoProfile',
  '-ExecutionPolicy', 'Bypass',
  '-File', "`"$startScript`""
) -join ' '

$action = New-ScheduledTaskAction -Execute $powershellExe -Argument $args
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $userId
if ($DelaySeconds -gt 0) {
  $trigger.Delay = "PT${DelaySeconds}S"
}

$settings = New-ScheduledTaskSettingsSet `
  -StartWhenAvailable `
  -AllowStartIfOnBatteries `
  -DontStopIfGoingOnBatteries `
  -ExecutionTimeLimit (New-TimeSpan -Minutes 10) `
  -MultipleInstances IgnoreNew

$logonTypeEnum = [Microsoft.PowerShell.Cmdletization.GeneratedTypes.ScheduledTask.LogonTypeEnum]
$supportedLogonTypes = [enum]::GetNames($logonTypeEnum)

# Different Windows/PowerShell versions expose different ScheduledTask logon types.
# Prefer an interactive logon type for "AtLogOn" triggers.
if ($supportedLogonTypes -contains 'Interactive') {
  $logonType = 'Interactive'
} elseif ($supportedLogonTypes -contains 'InteractiveOrPassword') {
  $logonType = 'InteractiveOrPassword'
} else {
  $logonType = 'None'
}

$principal = New-ScheduledTaskPrincipal -UserId $userId -LogonType $logonType -RunLevel $RunLevel
$task = New-ScheduledTask -Action $action -Trigger $trigger -Settings $settings -Principal $principal

Register-ScheduledTask -TaskName $TaskName -InputObject $task -Force | Out-Null

Write-Host "Registered scheduled task: $TaskName"
Write-Host "Repo: $repoRoot"
Write-Host "Script: $startScript"

