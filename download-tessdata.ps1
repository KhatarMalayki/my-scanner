# BlindScanner - Tessdata Download Script
# This script downloads OCR language files for Tesseract

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BlindScanner - Tessdata Downloader" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Determine tessdata path
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$tessdataPath = Join-Path $scriptDir "BlindScanner.ServiceHost\bin\Release\net6.0-windows\tessdata"

# Check if running from correct location
if (-not (Test-Path (Join-Path $scriptDir "BlindScanner.sln"))) {
    Write-Host "ERROR: Please run this script from the BlindScanner root directory!" -ForegroundColor Red
    Write-Host "Current directory: $scriptDir" -ForegroundColor Yellow
    pause
    exit 1
}

# Create tessdata directory
Write-Host "Creating tessdata directory..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $tessdataPath | Out-Null
Write-Host "Directory: $tessdataPath" -ForegroundColor Green
Write-Host ""

# Base URL for tessdata
$baseUrl = "https://github.com/tesseract-ocr/tessdata/raw/main"

# Available languages
$languages = @{
    "eng" = "English"
    "ind" = "Indonesian (Bahasa Indonesia)"
    "ara" = "Arabic"
    "chi_sim" = "Chinese Simplified"
    "chi_tra" = "Chinese Traditional"
    "fra" = "French"
    "deu" = "German"
    "hin" = "Hindi"
    "ita" = "Italian"
    "jpn" = "Japanese"
    "kor" = "Korean"
    "por" = "Portuguese"
    "rus" = "Russian"
    "spa" = "Spanish"
    "tha" = "Thai"
    "vie" = "Vietnamese"
}

# Display available languages
Write-Host "Available languages:" -ForegroundColor Cyan
Write-Host "-------------------" -ForegroundColor Cyan
$index = 1
$languageArray = @()
foreach ($lang in $languages.GetEnumerator() | Sort-Object Value) {
    Write-Host "$index. [$($lang.Key)] $($lang.Value)" -ForegroundColor White
    $languageArray += $lang.Key
    $index++
}
Write-Host ""
Write-Host "A. Download ALL languages (warning: ~200MB+)" -ForegroundColor Yellow
Write-Host "R. Recommended: eng + ind (English + Indonesian)" -ForegroundColor Green
Write-Host "Q. Quit" -ForegroundColor Gray
Write-Host ""

# Get user selection
$selection = Read-Host "Enter your choice (1-$($languages.Count), A, R, or Q)"

$languagesToDownload = @()

switch ($selection.ToUpper()) {
    "A" {
        $languagesToDownload = $languageArray
        Write-Host ""
        Write-Host "Downloading ALL languages..." -ForegroundColor Yellow
    }
    "R" {
        $languagesToDownload = @("eng", "ind")
        Write-Host ""
        Write-Host "Downloading recommended languages (English + Indonesian)..." -ForegroundColor Green
    }
    "Q" {
        Write-Host "Cancelled by user." -ForegroundColor Gray
        exit 0
    }
    default {
        if ($selection -match '^\d+$') {
            $idx = [int]$selection - 1
            if ($idx -ge 0 -and $idx -lt $languageArray.Count) {
                $languagesToDownload = @($languageArray[$idx])
                Write-Host ""
                Write-Host "Downloading: $($languages[$languageArray[$idx]])..." -ForegroundColor Green
            } else {
                Write-Host "Invalid selection!" -ForegroundColor Red
                pause
                exit 1
            }
        } else {
            Write-Host "Invalid selection!" -ForegroundColor Red
            pause
            exit 1
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

# Download selected languages
$successCount = 0
$failCount = 0

foreach ($lang in $languagesToDownload) {
    $fileName = "$lang.traineddata"
    $url = "$baseUrl/$fileName"
    $outputPath = Join-Path $tessdataPath $fileName

    # Check if file already exists
    if (Test-Path $outputPath) {
        $fileSize = (Get-Item $outputPath).Length / 1MB
        Write-Host "[SKIP] $fileName already exists ($("{0:N2}" -f $fileSize) MB)" -ForegroundColor Yellow
        $successCount++
        continue
    }

    Write-Host "[DOWN] Downloading $fileName..." -ForegroundColor Cyan

    try {
        # Download with progress
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $url -OutFile $outputPath -TimeoutSec 300

        $fileSize = (Get-Item $outputPath).Length / 1MB
        Write-Host "[OK]   $fileName downloaded successfully! ($("{0:N2}" -f $fileSize) MB)" -ForegroundColor Green
        $successCount++
    }
    catch {
        Write-Host "[FAIL] Failed to download $fileName" -ForegroundColor Red
        Write-Host "       Error: $($_.Exception.Message)" -ForegroundColor Red
        $failCount++
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Download Summary:" -ForegroundColor Cyan
Write-Host "  Success: $successCount" -ForegroundColor Green
Write-Host "  Failed:  $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Gray" })
Write-Host "  Total:   $($successCount + $failCount)" -ForegroundColor White
Write-Host ""

if ($successCount -gt 0) {
    Write-Host "Tessdata files saved to:" -ForegroundColor Green
    Write-Host "  $tessdataPath" -ForegroundColor White
    Write-Host ""
    Write-Host "You can now run BlindScanner!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. cd BlindScanner.ServiceHost\bin\Release\net6.0-windows" -ForegroundColor White
    Write-Host "  2. .\BlindScanner.ServiceHost.exe" -ForegroundColor White
    Write-Host "  3. Open http://localhost:8080 in your browser" -ForegroundColor White
}

Write-Host ""
pause
