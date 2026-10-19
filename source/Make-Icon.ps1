param([string]$Source = "$PSScriptRoot\Ncnc.App\Assets\NcNc.png", [string]$Destination = "$PSScriptRoot\Ncnc.App\Assets\NcNc.ico")
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$original = [Drawing.Image]::FromFile($Source)
try {
    $sizes = @(16,24,32,48,64,128,256)
    $images = @()
    foreach ($size in $sizes) {
        $bitmap = [Drawing.Bitmap]::new($size,$size,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $graphics = [Drawing.Graphics]::FromImage($bitmap)
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.DrawImage($original,0,0,$size,$size)
        $memory = [IO.MemoryStream]::new()
        $bitmap.Save($memory,[Drawing.Imaging.ImageFormat]::Png)
        $images += ,$memory.ToArray()
        $memory.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
    }
    $stream = [IO.File]::Create($Destination)
    $writer = [IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
        $offset = 6 + 16 * $sizes.Count
        for ($i=0;$i -lt $sizes.Count;$i++) {
            $dimension = [byte]($sizes[$i] % 256)
            $writer.Write($dimension); $writer.Write($dimension); $writer.Write([byte]0); $writer.Write([byte]0)
            $writer.Write([uint16]1); $writer.Write([uint16]32)
            $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
            $offset += $images[$i].Length
        }
        foreach ($bytes in $images) { $writer.Write([byte[]]$bytes) }
    } finally { $writer.Dispose(); $stream.Dispose() }
} finally { $original.Dispose() }
