# PowerShell script to create release package
param(
    [string]$ProjectDir,
    [string]$OutputPath,
    [string]$Configuration = "Release"
)

Write-Host "Starting release package creation..." -ForegroundColor Green
Write-Host "ProjectDir: $ProjectDir" -ForegroundColor Cyan
Write-Host "OutputPath: $OutputPath" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan

try {
    # Read Info.json to get mod ID and version
    $infoPath = Join-Path $ProjectDir "Info.json"
    Write-Host "Looking for Info.json at: $infoPath" -ForegroundColor Yellow
    
    if (-not (Test-Path $infoPath)) {
        throw "Info.json not found at: $infoPath"
    }
    
    $info = Get-Content $infoPath | ConvertFrom-Json
    $id = $info.Id
    $ver = $info.Version
    
    Write-Host "Mod ID: $id" -ForegroundColor Cyan
    Write-Host "Version: $ver" -ForegroundColor Cyan
    
    # Create release directory structure
    $releaseDir = Join-Path $ProjectDir "release"
    $modDir = Join-Path $releaseDir $id
    
    Write-Host "Creating release directory: $modDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Force -Path $modDir | Out-Null
    
    # Handle relative vs absolute output path
    if ([System.IO.Path]::IsPathRooted($OutputPath)) {
        $fullOutputPath = $OutputPath
    } else {
        $fullOutputPath = Join-Path $ProjectDir $OutputPath
    }
    
    Write-Host "Full output path: $fullOutputPath" -ForegroundColor Yellow
    
    # Copy DLL
    $dllSource = Join-Path $fullOutputPath "$id.dll"
    $dllDest = Join-Path $modDir "$id.dll"
    
    Write-Host "Looking for DLL at: $dllSource" -ForegroundColor Yellow
    
    if (-not (Test-Path $dllSource)) {
        # List files in output directory for debugging
        Write-Host "DLL not found. Files in output directory:" -ForegroundColor Red
        if (Test-Path $fullOutputPath) {
            Get-ChildItem $fullOutputPath | ForEach-Object { Write-Host "  $($_.Name)" -ForegroundColor Gray }
        } else {
            Write-Host "Output directory does not exist: $fullOutputPath" -ForegroundColor Red
        }
        throw "DLL not found at: $dllSource"
    }
    
    Write-Host "Copying DLL: $dllSource -> $dllDest" -ForegroundColor Yellow
    Copy-Item -Force $dllSource $dllDest
    
    # Copy Info.json
    $infoDest = Join-Path $modDir "Info.json"
    Write-Host "Copying Info.json: $infoPath -> $infoDest" -ForegroundColor Yellow
    Copy-Item -Force $infoPath $infoDest
    
    # Create zip archive
    $zipPath = Join-Path $releaseDir "${id}_v${ver}.zip"
    Write-Host "Creating zip archive: $zipPath" -ForegroundColor Yellow
    
    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }
    
    Compress-Archive -Path $modDir -DestinationPath $zipPath -Force
    
    Write-Host "Release package created successfully!" -ForegroundColor Green
    Write-Host "Package location: $zipPath" -ForegroundColor Cyan
    
    exit 0
}
catch {
    Write-Host "Error creating release package: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
    
    exit 1
}
