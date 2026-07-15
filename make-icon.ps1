# Genererer icon.ico (256x256, PNG-basert) for Store Filer
Add-Type -AssemblyName System.Drawing

$size = 256
$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'
$g.InterpolationMode = 'HighQualityBicubic'
$g.Clear([System.Drawing.Color]::Transparent)

# Avrundet blå bakgrunn med gradient
$rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
$path = New-Object System.Drawing.Drawing2D.GraphicsPath
$r = 48
$path.AddArc(0, 0, $r, $r, 180, 90)
$path.AddArc($size - $r, 0, $r, $r, 270, 90)
$path.AddArc($size - $r, $size - $r, $r, $r, 0, 90)
$path.AddArc(0, $size - $r, $r, $r, 90, 90)
$path.CloseFigure()
$c1 = [System.Drawing.Color]::FromArgb(255, 0, 122, 204)
$c2 = [System.Drawing.Color]::FromArgb(255, 0, 78, 150)
$grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $c1, $c2, 60)
$g.FillPath($grad, $path)

# Tre "størrelses-søyler" (hint om store filer)
$white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(70, 255, 255, 255))
$g.FillRectangle($white, 150, 150, 22, 60)
$g.FillRectangle($white, 178, 120, 22, 90)
$g.FillRectangle($white, 206, 96, 22, 114)

# Forstørrelsesglass (hvit ring + håndtak)
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 22)
$pen.StartCap = 'Round'; $pen.EndCap = 'Round'
$g.DrawEllipse($pen, 44, 44, 108, 108)
$g.DrawLine($pen, 140, 140, 196, 196)

$g.Dispose()

# Lagre som PNG i minne
$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$png = $ms.ToArray()
$ms.Dispose(); $bmp.Dispose()

# Pakk PNG inn i en ICO-container
$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($out)
$bw.Write([UInt16]0)      # reserved
$bw.Write([UInt16]1)      # type = icon
$bw.Write([UInt16]1)      # antall bilder
$bw.Write([Byte]0)        # bredde 0 = 256
$bw.Write([Byte]0)        # høyde 0 = 256
$bw.Write([Byte]0)        # farger
$bw.Write([Byte]0)        # reservert
$bw.Write([UInt16]1)      # planes
$bw.Write([UInt16]32)     # bits per piksel
$bw.Write([UInt32]$png.Length)
$bw.Write([UInt32]22)     # offset til PNG-data
$bw.Write($png)
$bw.Flush()
[System.IO.File]::WriteAllBytes("$PSScriptRoot\icon.ico", $out.ToArray())
$out.Dispose()
Write-Output "icon.ico laget ($($png.Length) bytes PNG)"
