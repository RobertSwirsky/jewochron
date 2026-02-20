# PowerShell Script to Generate App Icons from SVG
# This script uses Inkscape (free) or ImageMagick to convert SVG to PNG at various sizes

Write-Host "Jewochron Icon Generator" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
Write-Host ""

# Define required icon sizes
$iconSizes = @{
    "Square44x44Logo.scale-200.png" = 88
    "Square44x44Logo.targetsize-24_altform-unplated.png" = 24
    "Square150x150Logo.scale-200.png" = 300
    "Wide310x150Logo.scale-200.png" = @(620, 300)  # Width, Height
    "StoreLogo.png" = 50
}

$svgFile = "Assets\Jewochron-Icon-Jerusalem.svg"

# Check if SVG exists
if (-not (Test-Path $svgFile)) {
    Write-Host "ERROR: SVG file not found: $svgFile" -ForegroundColor Red
    Write-Host "Please make sure the SVG file exists." -ForegroundColor Yellow
    exit 1
}

Write-Host "Found SVG file: $svgFile" -ForegroundColor Green
Write-Host ""

# Option 1: Check for Inkscape (Best quality)
$inkscapePath = $null
$possibleInkscapePaths = @(
    "C:\Program Files\Inkscape\bin\inkscape.exe",
    "C:\Program Files (x86)\Inkscape\bin\inkscape.exe",
    "$env:LOCALAPPDATA\Programs\Inkscape\bin\inkscape.exe"
)

foreach ($path in $possibleInkscapePaths) {
    if (Test-Path $path) {
        $inkscapePath = $path
        break
    }
}

if ($inkscapePath) {
    Write-Host "Found Inkscape at: $inkscapePath" -ForegroundColor Green
    Write-Host "Generating PNG files..." -ForegroundColor Cyan
    Write-Host ""
    
    foreach ($icon in $iconSizes.GetEnumerator()) {
        $outputFile = "Assets\$($icon.Key)"
        
        if ($icon.Value -is [Array]) {
            # Wide logo - special handling
            $width = $icon.Value[0]
            $height = $icon.Value[1]
            Write-Host "Creating $($icon.Key) (${width}x${height})..." -ForegroundColor Yellow
            & $inkscapePath $svgFile --export-filename=$outputFile --export-width=$width --export-height=$height
        } else {
            # Square logo
            $size = $icon.Value
            Write-Host "Creating $($icon.Key) (${size}x${size})..." -ForegroundColor Yellow
            & $inkscapePath $svgFile --export-filename=$outputFile --export-width=$size --export-height=$size
        }
    }
    
    # Create splash screen
    Write-Host "Creating SplashScreen.scale-200.png (1240x600)..." -ForegroundColor Yellow
    & $inkscapePath $svgFile --export-filename="Assets\SplashScreen.scale-200.png" --export-width=1240 --export-height=600
    
    Write-Host ""
    Write-Host "✓ All icon files generated successfully!" -ForegroundColor Green
    
} else {
    Write-Host "Inkscape not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "OPTION 1 (Recommended): Install Inkscape" -ForegroundColor Yellow
    Write-Host "1. Download Inkscape from: https://inkscape.org/release/" -ForegroundColor White
    Write-Host "2. Install it" -ForegroundColor White
    Write-Host "3. Run this script again" -ForegroundColor White
    Write-Host ""
    Write-Host "OPTION 2: Use Online Converter" -ForegroundColor Yellow
    Write-Host "1. Go to https://cloudconvert.com/svg-to-png" -ForegroundColor White
    Write-Host "2. Upload: $svgFile" -ForegroundColor White
    Write-Host "3. Set dimensions for each icon size:" -ForegroundColor White
    foreach ($icon in $iconSizes.GetEnumerator()) {
        if ($icon.Value -is [Array]) {
            Write-Host "   - $($icon.Key): $($icon.Value[0])x$($icon.Value[1])" -ForegroundColor Cyan
        } else {
            Write-Host "   - $($icon.Key): $($icon.Value)x$($icon.Value)" -ForegroundColor Cyan
        }
    }
    Write-Host "   - SplashScreen.scale-200.png: 1240x600" -ForegroundColor Cyan
    Write-Host "4. Download and save to Assets folder" -ForegroundColor White
    Write-Host ""
    Write-Host "OPTION 3: Quick Emoji Icon (See below)" -ForegroundColor Yellow
    Write-Host "Run: .\Assets\Create-Quick-Icon.ps1" -ForegroundColor White
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
