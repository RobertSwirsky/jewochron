# Quick Icon Generator using System.Drawing
# Creates simple but effective icons with Star of David and text

Write-Host "Quick Jewochron Icon Generator" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This creates simple icons using emojis and text." -ForegroundColor Yellow
Write-Host "For better quality, use the SVG files with Inkscape." -ForegroundColor Yellow
Write-Host ""

# Create a simple HTML file that can be screenshot for icons
$htmlContent = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <style>
        body { margin: 0; padding: 0; }
        .icon-container {
            width: 1024px;
            height: 1024px;
            background: linear-gradient(135deg, #1A4D7A 0%, #0D2847 100%);
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
            border-radius: 180px;
        }
        .star {
            font-size: 400px;
            line-height: 1;
            margin-bottom: -50px;
        }
        .clock {
            font-size: 200px;
            line-height: 1;
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
        }
        .text {
            font-size: 80px;
            color: #FFD700;
            font-weight: bold;
            margin-top: 20px;
        }
    </style>
</head>
<body>
    <div class="icon-container">
        <div class="star">✡️</div>
        <div class="clock">🕐</div>
        <div class="text">Jewochron</div>
    </div>
</body>
</html>
"@

$htmlFile = "Assets\temp-icon.html"
$htmlContent | Out-File -FilePath $htmlFile -Encoding UTF8

Write-Host "Created HTML icon template: $htmlFile" -ForegroundColor Green
Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Yellow
Write-Host "1. Open $htmlFile in your web browser" -ForegroundColor White
Write-Host "2. Press F12 to open Developer Tools" -ForegroundColor White
Write-Host "3. Press Ctrl+Shift+P (Chrome/Edge) and type 'screenshot'" -ForegroundColor White
Write-Host "4. Select 'Capture node screenshot'" -ForegroundColor White
Write-Host "5. Click on the icon container" -ForegroundColor White
Write-Host "6. Save as PNG and use online resizer for different sizes" -ForegroundColor White
Write-Host ""
Write-Host "OR use this EASIER method:" -ForegroundColor Cyan
Write-Host "1. Open: https://www.appicon.co/" -ForegroundColor White
Write-Host "2. Upload the screenshot" -ForegroundColor White  
Write-Host "3. Download all sizes for Windows" -ForegroundColor White
Write-Host "4. Copy files to Assets folder" -ForegroundColor White
Write-Host ""

# Alternative: Create simple solid color icons as placeholders
Write-Host "Would you like to create simple colored placeholders? (Y/N)" -ForegroundColor Yellow
$response = Read-Host

if ($response -eq 'Y' -or $response -eq 'y') {
    Write-Host "Creating placeholder icons..." -ForegroundColor Cyan
    
    # We'll create very basic placeholder icons
    # This requires System.Drawing which is available on Windows
    
    Add-Type -AssemblyName System.Drawing
    
    $iconSizes = @{
        "Square44x44Logo.scale-200.png" = 88
        "Square44x44Logo.targetsize-24_altform-unplated.png" = 24
        "Square150x150Logo.scale-200.png" = 300
        "StoreLogo.png" = 50
    }
    
    foreach ($icon in $iconSizes.GetEnumerator()) {
        $size = $icon.Value
        $outputFile = "Assets\$($icon.Key)"
        
        $bitmap = New-Object System.Drawing.Bitmap($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        
        # Fill with gradient-ish background
        $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(26, 77, 122))
        $graphics.FillRectangle($brush, 0, 0, $size, $size)
        
        # Draw Star of David text
        $font = New-Object System.Drawing.Font("Segoe UI Emoji", ($size / 2), [System.Drawing.FontStyle]::Bold)
        $textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(218, 165, 32))
        $format = New-Object System.Drawing.StringFormat
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        
        $graphics.DrawString("✡", $font, $textBrush, ($size / 2), ($size / 2), $format)
        
        $bitmap.Save($outputFile, [System.Drawing.Imaging.ImageFormat]::Png)
        
        $graphics.Dispose()
        $bitmap.Dispose()
        
        Write-Host "Created: $($icon.Key)" -ForegroundColor Green
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
    $bitmap.Save("Assets\Wide310x150Logo.scale-200.png", [System.Drawing.Imaging.ImageFormat]::Png)
    
    $graphics.Dispose()
    $bitmap.Dispose()
    
    Write-Host "Created: Wide310x150Logo.scale-200.png" -ForegroundColor Green
    
    # Create splash screen
    $width = 1240
    $height = 600
    $bitmap = New-Object System.Drawing.Bitmap($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(26, 77, 122))
    $graphics.FillRectangle($brush, 0, 0, $width, $height)
    
    $font = New-Object System.Drawing.Font("Segoe UI", 120, [System.Drawing.FontStyle]::Bold)
    $graphics.DrawString("✡ Jewochron", $font, $textBrush, ($width / 2), ($height / 2), $format)
    $bitmap.Save("Assets\SplashScreen.scale-200.png", [System.Drawing.Imaging.ImageFormat]::Png)
    
    $graphics.Dispose()
    $bitmap.Dispose()
    
    Write-Host "Created: SplashScreen.scale-200.png" -ForegroundColor Green
    Write-Host ""
    Write-Host "✓ Placeholder icons created!" -ForegroundColor Green
    Write-Host "These are basic placeholders. For better icons, use the SVG files." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
