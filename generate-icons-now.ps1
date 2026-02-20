# Quick Icon Generator - Run Now
Write-Host "Creating Jewochron placeholder icons..." -ForegroundColor Cyan

Add-Type -AssemblyName System.Drawing

$iconSizes = @{
    "Assets\\Square44x44Logo.scale-200.png" = 88
    "Assets\\Square44x44Logo.targetsize-24_altform-unplated.png" = 24
    "Assets\\Square150x150Logo.scale-200.png" = 300
    "Assets\\StoreLogo.png" = 50
}

foreach ($icon in $iconSizes.GetEnumerator()) {
    $size = $icon.Value
    $outputFile = $icon.Key
    
    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    
    # Fill with blue background
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(26, 77, 122))
    $graphics.FillRectangle($brush, 0, 0, $size, $size)
    
    # Draw Star of David emoji
    $font = New-Object System.Drawing.Font("Segoe UI Emoji", ($size / 2), [System.Drawing.FontStyle]::Bold)
    $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(218, 165, 32))
    $format = New-Object System.Drawing.StringFormat
    $format.Alignment = [System.Drawing.StringAlignment]::Center
    $format.LineAlignment = [System.Drawing.StringAlignment]::Center
    
    $graphics.DrawString("✡", $font, $textBrush, ($size / 2), ($size / 2), $format)
    
    $bitmap.Save($outputFile, [System.Drawing.Imaging.ImageFormat]::Png)
    
    $graphics.Dispose()
    $bitmap.Dispose()
    
    Write-Host "✓ Created: $outputFile" -ForegroundColor Green
}

# Create wide logo
$width = 620
$height = 300
$bitmap = New-Object System.Drawing.Bitmap($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(26, 77, 122))
$graphics.FillRectangle($brush, 0, 0, $width, $height)

$font = New-Object System.Drawing.Font("Segoe UI", 80, [System.Drawing.FontStyle]::Bold)
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(218, 165, 32))
$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center

$graphics.DrawString("✡ Jewochron", $font, $textBrush, ($width / 2), ($height / 2), $format)
$bitmap.Save("Assets\\Wide310x150Logo.scale-200.png", [System.Drawing.Imaging.ImageFormat]::Png)

$graphics.Dispose()
$bitmap.Dispose()

Write-Host "✓ Created: Assets\\Wide310x150Logo.scale-200.png" -ForegroundColor Green

# Create splash screen
$width = 1240
$height = 600
$bitmap = New-Object System.Drawing.Bitmap($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(26, 77, 122))
$graphics.FillRectangle($brush, 0, 0, $width, $height)

$font = New-Object System.Drawing.Font("Segoe UI", 120, [System.Drawing.FontStyle]::Bold)
$graphics.DrawString("✡ Jewochron", $font, $textBrush, ($width / 2), ($height / 2), $format)
$bitmap.Save("Assets\\SplashScreen.scale-200.png", [System.Drawing.Imaging.ImageFormat]::Png)

$graphics.Dispose()
$bitmap.Dispose()

Write-Host "✓ Created: Assets\\SplashScreen.scale-200.png" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✓ All placeholder icons created!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Yellow
Write-Host "1. Close Visual Studio" -ForegroundColor White
Write-Host "2. Delete the 'bin' and 'obj' folders" -ForegroundColor White
Write-Host "3. Rebuild the project" -ForegroundColor White
Write-Host "4. Run the app - you should see your new icon!" -ForegroundColor White
Write-Host ""
