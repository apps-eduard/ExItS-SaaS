<#
.SYNOPSIS
  Pre-commit safety report for ExItS-SaaS (manual; does not stage or commit).

.DESCRIPTION
  Reports untracked files, staged files, and flags staged paths that look like
  secrets or generated artifacts. Exit code 1 when risky staged paths are found.
  Does not modify the index or working tree.

.EXAMPLE
  powershell -File scripts/git/pre-commit-check.ps1
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    $root = git rev-parse --show-toplevel 2>$null
    if (-not $root) {
        throw 'Not inside a Git repository.'
    }
    return $root.Trim()
}

$repoRoot = Get-RepoRoot
Push-Location $repoRoot
try {
    Write-Host '=== ExItS pre-commit check (report only) ==='
    Write-Host "Repository: $repoRoot"
    Write-Host ''

    $untracked = @(git status --porcelain --untracked-files=all |
        Where-Object { $_ -match '^\?\?' } |
        ForEach-Object { $_.Substring(3).Trim() })

    Write-Host '--- Untracked files ---'
    if ($untracked.Count -eq 0) {
        Write-Host '(none)'
    }
    else {
        $untracked | ForEach-Object { Write-Host "  ?? $_" }
        Write-Host ''
        Write-Host 'Review each ?? path before git add. Never silently discard new files.'
    }
    Write-Host ''

    $stagedRaw = @(git diff --cached --name-only --diff-filter=ACMR)
    Write-Host '--- Staged files ---'
    if ($stagedRaw.Count -eq 0) {
        Write-Host '(none)'
    }
    else {
        $stagedRaw | ForEach-Object { Write-Host "  $_" }
    }
    Write-Host ''

    $secretOrGeneratedPatterns = @(
        '(^|/)\.env(\.|$)',
        '\.pem$',
        '\.key$',
        '\.pfx$',
        '\.p12$',
        '\.apk$',
        '\.aab$',
        '\.nupkg$',
        '(^|/)bin(/|$)',
        '(^|/)obj(/|$)',
        '(^|/)node_modules(/|$)',
        '(^|/)TestResults(/|$)',
        '(^|/)coverage(/|$)',
        '(^|/)artifacts(/|$)',
        '\.user$',
        '\.suo$',
        '(^|/)\.vs(/|$)',
        '(^|/)\.idea(/|$)',
        '\.dump(\.enc)?$',
        'credentials\.json$',
        'client_secret',
        'appsettings\..*\.local\.json$'
    )

    $risky = @()
    foreach ($path in $stagedRaw) {
        foreach ($pattern in $secretOrGeneratedPatterns) {
            if ($path -match $pattern) {
                $risky += $path
                break
            }
        }
    }

    Write-Host '--- Staged secrets / generated artifacts ---'
    if ($risky.Count -eq 0) {
        Write-Host '(none detected by heuristic patterns)'
        $exitCode = 0
    }
    else {
        $risky | ForEach-Object { Write-Host "  RISK: $_" }
        Write-Host ''
        Write-Host 'Unstage these before committing unless intentionally reviewed and allowed.'
        $exitCode = 1
    }

    Write-Host ''
    Write-Host 'This script does not stage or commit files.'
    exit $exitCode
}
finally {
    Pop-Location
}
