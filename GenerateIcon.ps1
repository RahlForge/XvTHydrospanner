# Generate Icon for XvT Hydrospanner
# Creates a simple 32x32 icon with a wrench/tool theme

Add-Type -AssemblyName System.Drawing

Write-Host "Generating XvT Hydrospanner icon..." -ForegroundColor Cyan

# Create a 32x32 bitmap
$size = 32
$bitmap = New-Object System.Drawing.Bitmap($size, $size)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Enable anti-aliasing for smoother lines
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

# Fill background with transparent black
$graphics.Clear([System.Drawing.Color]::Transparent)

# Define colors - Gold and gray (Star Wars themed)
$goldColor = [System.Drawing.Color]::FromArgb(255, 255, 215, 0)  # FFD700
$darkGray = [System.Drawing.Color]::FromArgb(255, 60, 60, 60)
$lightGray = [System.Drawing.Color]::FromArgb(255, 180, 180, 180)

# Draw a stylized wrench/hydrospanner
# Handle of the wrench
$handleBrush = New-Object System.Drawing.SolidBrush($darkGray)
$graphics.FillRectangle($handleBrush, 6, 18, 4, 12)

# Wrench head (hexagon-ish shape for open-end wrench)
$headBrush = New-Object System.Drawing.SolidBrush($goldColor)
$graphics.FillPolygon($headBrush, @(
    [System.Drawing.Point]::new(4, 16),
    [System.Drawing.Point]::new(12, 16),
    [System.Drawing.Point]::new(14, 12),
    [System.Drawing.Point]::new(12, 8),
    [System.Drawing.Point]::new(4, 8),
    [System.Drawing.Point]::new(2, 12)
))

# Add highlight to make it look 3D
$highlightBrush = New-Object System.Drawing.SolidBrush($lightGray)
$graphics.FillPolygon($highlightBrush, @(
    [System.Drawing.Point]::new(5, 15),
    [System.Drawing.Point]::new(11, 15),
    [System.Drawing.Point]::new(12, 13),
    [System.Drawing.Point]::new(11, 11),
    [System.Drawing.Point]::new(5, 11),
    [System.Drawing.Point]::new(4, 13)
))

# Draw a gear/cog to the right (representing modification/mechanics)
$gearBrush = New-Object System.Drawing.SolidBrush($goldColor)
$centerX = 22
$centerY = 12

# Draw gear teeth (simplified 6-tooth gear)
for ($i = 0; $i -lt 6; $i++) {
    $angle = ($i * 60) * [Math]::PI / 180
    $x1 = $centerX + [Math]::Cos($angle) * 6
    $y1 = $centerY + [Math]::Sin($angle) * 6
    $x2 = $centerX + [Math]::Cos($angle) * 8
    $y2 = $centerY + [Math]::Sin($angle) * 8
    
    $graphics.FillRectangle($gearBrush, $x2 - 1.5, $y2 - 1.5, 3, 3)
}

# Draw center circle of gear
$graphics.FillEllipse($gearBrush, $centerX - 4, $centerY - 4, 8, 8)
$graphics.FillEllipse($handleBrush, $centerX - 2, $centerY - 2, 4, 4)

# Add small stars (3 stars for Star Wars theme)
$starBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(200, 255, 255, 255))
$graphics.FillPolygon($starBrush, @(
    [System.Drawing.Point]::new(16, 26),
    [System.Drawing.Point]::new(17, 28),
    [System.Drawing.Point]::new(15, 28)
))
$graphics.FillPolygon($starBrush, @(
    [System.Drawing.Point]::new(24, 22),
    [System.Drawing.Point]::new(25, 24),
    [System.Drawing.Point]::new(23, 24)
))
$graphics.FillPolygon($starBrush, @(
    [System.Drawing.Point]::new(28, 28),
    [System.Drawing.Point]::new(29, 30),
    [System.Drawing.Point]::new(27, 30)
))

# Cleanup
$graphics.Dispose()

# Save as ICO file
$iconPath = Join-Path $PSScriptRoot "XvTHydrospanner\Resources\hydrospanner.ico"
$resourcesDir = Join-Path $PSScriptRoot "XvTHydrospanner\Resources"

# Create Resources directory if it doesn't exist
if (-not (Test-Path $resourcesDir)) {
    New-Item -ItemType Directory -Path $resourcesDir -Force | Out-Null
    Write-Host "Created Resources directory" -ForegroundColor Green
}

# Convert to ICO format
try {
    # Create icon from bitmap
    $iconStream = New-Object System.IO.MemoryStream
    $bitmap.Save($iconStream, [System.Drawing.Imaging.ImageFormat]::Png)
    $iconStream.Position = 0
    
    # ICO file header
    $iconBytes = New-Object System.Collections.Generic.List[byte]
    
    # ICONDIR header
    $iconBytes.Add(0); $iconBytes.Add(0)  # Reserved (must be 0)
    $iconBytes.Add(1); $iconBytes.Add(0)  # Type (1 = icon)
    $iconBytes.Add(1); $iconBytes.Add(0)  # Number of images
    
    # ICONDIRENTRY
    $iconBytes.Add($size)  # Width
    $iconBytes.Add($size)  # Height
    $iconBytes.Add(0)      # Color palette
    $iconBytes.Add(0)      # Reserved
    $iconBytes.Add(1); $iconBytes.Add(0)  # Color planes
    $iconBytes.Add(32); $iconBytes.Add(0) # Bits per pixel
    
    # Image size
    $pngBytes = $iconStream.ToArray()
    $imageSize = $pngBytes.Length
    $iconBytes.Add([byte]($imageSize -band 0xFF))
    $iconBytes.Add([byte](($imageSize -shr 8) -band 0xFF))
    $iconBytes.Add([byte](($imageSize -shr 16) -band 0xFF))
    $iconBytes.Add([byte](($imageSize -shr 24) -band 0xFF))
    
    # Image offset (22 bytes for header + entry)
    $iconBytes.Add(22); $iconBytes.Add(0); $iconBytes.Add(0); $iconBytes.Add(0)
    
    # Add PNG data
    $iconBytes.AddRange($pngBytes)
    
    # Write to file
    [System.IO.File]::WriteAllBytes($iconPath, $iconBytes.ToArray())
    
    Write-Host "Icon created successfully!" -ForegroundColor Green
    Write-Host "Location: $iconPath" -ForegroundColor Yellow
    
    $iconStream.Dispose()
}
catch {
    Write-Host "Error creating icon: $_" -ForegroundColor Red
    Write-Host "Saving as PNG instead..." -ForegroundColor Yellow
    $pngPath = Join-Path $resourcesDir "hydrospanner.png"
    $bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Host "PNG saved to: $pngPath" -ForegroundColor Yellow
}

$bitmap.Dispose()

Write-Host ""
Write-Host "Icon generation complete!" -ForegroundColor Cyan
