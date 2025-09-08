# PowerShell script to create GitHub release
param(
    [string]$ProjectDir
)

Write-Host "Starting GitHub release creation..." -ForegroundColor Green

try {
    # Check if GitHub CLI is available
    $ghExists = Get-Command gh -ErrorAction SilentlyContinue
    if (-not $ghExists) {
        Write-Host "GitHub CLI (gh) not found. Please install it to use this feature." -ForegroundColor Yellow
        Write-Host "Download from: https://cli.github.com/" -ForegroundColor Cyan
        return
    }
    
    # Read Info.json to get mod info
    $infoPath = Join-Path $ProjectDir "Info.json"
    if (-not (Test-Path $infoPath)) {
        throw "Info.json not found at: $infoPath"
    }
    
    $info = Get-Content $infoPath | ConvertFrom-Json
    $id = $info.Id
    $ver = $info.Version
    
    Write-Host "Creating GitHub release for $id v$ver" -ForegroundColor Cyan
    
    # Construct the zip path based on the version from Info.json
    $ZipPath = Join-Path $ProjectDir "release\${id}_v${ver}.zip"
    Write-Host "Looking for zip file at: $ZipPath" -ForegroundColor Yellow
    
    # Check if zip file exists
    if (-not (Test-Path $ZipPath)) {
        throw "Zip file not found at: $ZipPath"
    }
    
    # Create GitHub release
    $tagName = "v$ver"
    $releaseName = "$id v$ver"
    $releaseNotes = "Automated release from Visual Studio post-build event."
    
    Write-Host "Creating release: $tagName" -ForegroundColor Yellow
    gh release create $tagName $ZipPath --title $releaseName --notes $releaseNotes
    
    Write-Host "GitHub release created successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Error creating GitHub release: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "You can manually create the release using the generated zip file." -ForegroundColor Yellow
}
