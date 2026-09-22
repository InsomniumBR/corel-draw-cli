$repoRoot = $PSScriptRoot
$marker = Join-Path $repoRoot ".installed"

if (Test-Path $marker) {
    Write-Host "Tools already installed. Skipping."
    exit 0
}

Push-Location $repoRoot
try {
    Write-Host "Installing .NET tools..."
    dotnet tool restore

    Write-Host "Installing Husky Git hooks..."
    dotnet husky install
}
finally {
    Pop-Location
}

Write-Host "Creating marker file..."
New-Item -ItemType File -Path $marker -Force | Out-Null

Write-Host "Tools installation completed."
