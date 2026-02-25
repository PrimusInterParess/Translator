[CmdletBinding()]
param(
  [string] $TaskName = 'Translator - start docker compose'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
  Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
  Write-Host "Unregistered scheduled task: $TaskName"
} else {
  Write-Host "Task not found: $TaskName"
}

