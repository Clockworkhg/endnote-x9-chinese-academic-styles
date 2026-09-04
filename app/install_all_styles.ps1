$ErrorActionPreference = "Stop"

try {
    $sourceDirectory = Join-Path $PSScriptRoot "Styles"
    if (-not (Test-Path -LiteralPath $sourceDirectory -PathType Container)) {
        throw "Styles directory is missing."
    }
    $styles = @(Get-ChildItem -LiteralPath $sourceDirectory -Filter "*.ens" -File)
    if ($styles.Count -eq 0) {
        throw "No ENS files were found."
    }
    $documents = [Environment]::GetFolderPath("MyDocuments")
    if ([string]::IsNullOrWhiteSpace($documents)) {
        throw "Windows did not return the Documents folder path."
    }
    $targetDirectory = Join-Path $documents "EndNote\Styles"
    $backupDirectory = Join-Path $targetDirectory ("CN-Academic-Backup-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
    New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
    $backupCreated = $false
    foreach ($style in $styles) {
        $target = Join-Path $targetDirectory $style.Name
        if (Test-Path -LiteralPath $target -PathType Leaf) {
            if (-not $backupCreated) {
                New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
                $backupCreated = $true
            }
            Copy-Item -LiteralPath $target -Destination (Join-Path $backupDirectory $style.Name) -Force
        }
        Copy-Item -LiteralPath $style.FullName -Destination $target -Force
    }
    Write-Host ""
    Write-Host "INSTALLATION SUCCEEDED." -ForegroundColor Green
    Write-Host ("Installed styles: " + $styles.Count)
    Write-Host ("Installed to: " + $targetDirectory)
    if ($backupCreated) { Write-Host ("Previous files backed up to: " + $backupDirectory) }
    Write-Host "Restart Word/WPS and select a style beginning with: Chinese Academic (displayed as 中文学术)."
    exit 0
}
catch {
    Write-Host ""
    Write-Host ("INSTALLATION FAILED: " + $_.Exception.Message) -ForegroundColor Red
    Write-Host "Please send a screenshot of this complete window."
    exit 1
}
