# Sync Matt Pocock skills into .cursor/skills/<category>/<name>/ using skills-lock.json.
# Preserves upstream folder taxonomy instead of the flat .agents/skills/ layout.

param(
    [string]$LockFile = (Join-Path $PSScriptRoot '..\skills-lock.json'),
    [string]$DestRoot = (Join-Path $PSScriptRoot '..\.cursor\skills')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path $LockFile)) {
    throw "Lock file not found: $LockFile"
}

$lock = Get-Content $LockFile -Raw | ConvertFrom-Json
$skills = $lock.skills
if (-not $skills) {
    throw "No skills entries in $LockFile"
}

$tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("translator-skills-" + [guid]::NewGuid().ToString('n'))
New-Item -ItemType Directory -Path $tmp -Force | Out-Null

try {
    $sources = @{}
    foreach ($prop in $skills.PSObject.Properties) {
        $entry = $prop.Value
        $source = "$($entry.source)@$($entry.computedHash.Substring(0, 12))"
        if (-not $sources.ContainsKey($source)) {
            $sources[$source] = @{
                Repo = $entry.source
                Hash = $entry.computedHash
                Skills = @()
            }
        }
        $sources[$source].Skills += [pscustomobject]@{
            Name = $prop.Name
            SkillPath = $entry.skillPath
        }
    }

    foreach ($sourceKey in $sources.Keys) {
        $bundle = $sources[$sourceKey]
        $repoDir = Join-Path $tmp $bundle.Repo.Replace('/', '-')
        if (-not (Test-Path $repoDir)) {
            Write-Host "Cloning $($bundle.Repo) ..."
            git clone --depth 1 "https://github.com/$($bundle.Repo).git" $repoDir | Out-Null
        }

        foreach ($skill in $bundle.Skills) {
            if ($skill.SkillPath -notmatch '^skills/(.+)/SKILL\.md$') {
                throw "Unexpected skillPath for $($skill.Name): $($skill.SkillPath)"
            }
            $relativeDir = $Matches[1]
            $srcDir = Join-Path $repoDir $relativeDir
            $dstDir = Join-Path $DestRoot $relativeDir

            if (-not (Test-Path $srcDir)) {
                throw "Skill folder missing in upstream repo: $srcDir"
            }

            if (Test-Path $dstDir) {
                Remove-Item $dstDir -Recurse -Force
            }
            New-Item -ItemType Directory -Path (Split-Path $dstDir -Parent) -Force | Out-Null
            Copy-Item $srcDir $dstDir -Recurse -Force
            Write-Host "Installed $($skill.Name) -> .cursor/skills/$relativeDir"
        }
    }
}
finally {
    if (Test-Path $tmp) {
        Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "Done. Skills are under .cursor/skills/"
