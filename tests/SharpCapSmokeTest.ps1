param([double] $ExposureSeconds = 0.1)
$ErrorActionPreference = 'Stop'
$camera = New-Object -ComObject 'ASCOM.OpenUvcColor.Camera'
try {
    $camera.Connected = $true
    $camera.StartExposure($ExposureSeconds, $true)
    $deadline = [DateTime]::UtcNow.AddSeconds(10 + $ExposureSeconds)
    while (-not $camera.ImageReady -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
    if (-not $camera.ImageReady) { throw 'Exposure did not complete.' }
    $image = $camera.ImageArray
    if ($image.Rank -ne 3 -or $image.GetLength(2) -ne 3) { throw "Expected [x,y,RGB] color ImageArray; got rank $($image.Rank)." }
    Write-Host "PASS: $($image.GetLength(0))x$($image.GetLength(1)) RGB ImageArray"
} finally {
    if ($null -ne $camera) { $camera.Connected = $false }
}
