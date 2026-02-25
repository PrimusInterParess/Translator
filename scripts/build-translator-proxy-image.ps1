[CmdletBinding()]
param(
  # Base image name without a tag. Examples:
  # - translator-proxy
  # - myregistry.example.com/translator-proxy
  [string] $Image = $env:TRANSLATOR_PROXY_IMAGE,

  # Optional registry prefix (will be prepended to -Image).
  # Example: myregistry.azurecr.io
  [string] $Registry = $env:DOCKER_REGISTRY,

  # If set, also pushes :latest and :sha-<gitsha> tags.
  [switch] $Push
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Normalize-RegistryPrefix([string] $value) {
  if ([string]::IsNullOrWhiteSpace($value)) { return $null }
  return $value.Trim().TrimEnd('/')
}

function Run([string] $exe, [string[]] $args) {
  Write-Host ("`n> {0} {1}" -f $exe, ($args -join ' '))
  & $exe @args
  if ($LASTEXITCODE -ne 0) { throw "$exe failed with exit code $LASTEXITCODE" }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$contextDir = Join-Path $repoRoot 'translator-proxy'
$dockerfile = Join-Path $contextDir 'Dockerfile'

if (-not (Test-Path -LiteralPath $dockerfile)) {
  throw "Dockerfile not found: $dockerfile"
}

if ([string]::IsNullOrWhiteSpace($Image)) {
  $Image = 'translator-proxy'
}

$Registry = Normalize-RegistryPrefix $Registry
if ($Registry) {
  $Image = "$Registry/$Image"
}

Run git @('rev-parse', '--is-inside-work-tree') | Out-Null
$sha = (Run git @('rev-parse', '--short=12', 'HEAD') | Select-Object -Last 1).Trim()
if ([string]::IsNullOrWhiteSpace($sha)) { throw "Could not determine git SHA." }

$tagLatest = "$Image:latest"
$tagSha = "$Image:sha-$sha"

Run docker @('build', '-f', $dockerfile, '-t', $tagLatest, '-t', $tagSha, $contextDir)

if ($Push) {
  Run docker @('push', $tagLatest)
  Run docker @('push', $tagSha)
}

