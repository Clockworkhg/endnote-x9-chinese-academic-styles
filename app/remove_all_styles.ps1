$ErrorActionPreference = "Stop"

try {
    $sourceDirectory = Join-Path $PSScriptRoot "Styles"
    $documents = [Environment]::GetFolderPath("MyDocuments")
    $targetDirectory = Join-Path $documents "EndNote\Styles"
    $styles = @(Get-ChildItem -LiteralPath $sourceDirectory -Filter "*.ens" -File)
    $removed = 0
    foreach ($style in $styles) {
        $target = Join-Path $targetDirectory $style.Name
        if (Test-Path -LiteralPath $target -PathType Leaf) {
            Remove-Item -LiteralPath $target -Force
            $removed++
        }
    }
    Write-Host ("Removed styles: " + $removed) -ForegroundColor Green
    Write-Host "Timestamped backup folders were not removed."
    exit 0
}
catch {
    Write-Host ("REMOVAL FAILED: " + $_.Exception.Message) -ForegroundColor Red
    exit 1
}
