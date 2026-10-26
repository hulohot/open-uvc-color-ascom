# Open UVC Color ASCOM Camera

An independently written, source-only ASCOM Camera V2 driver for ordinary UVC
colour cameras. It uses Windows Media Foundation through OpenCvSharp and returns
an RGB `ImageArray[x, y, plane]` (plane 0=red, 1=green, 2=blue), rather than a
grayscale array.

It is an interoperability experiment, not an official FIBONAX product. No
vendor binary, firmware, manual, proprietary source, or device-specific control
sequence is in this project.

## Requirements

* Windows 11 on ARM running x64 applications through Windows emulation.
* .NET SDK with .NET Framework 4.8.1 targeting pack and the .NET Framework 4.8.1 runtime.
* ASCOM Platform (for the ASCOM chooser and client integration).
* A UVC camera that Windows can open through Media Foundation.
* Administrator PowerShell only for COM/ASCOM registration.

This is deliberately an **x64** build. That is the practical Windows 11 ARM path
for SharpCap and OpenCvSharp's Windows native capture library. It is not a
native-ARM64 driver and it must be loaded by a 64-bit ASCOM client.

## Build and install

Open an elevated 64-bit PowerShell in this directory and run:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\install.ps1
```

The release ZIP is prebuilt, so it does not need the .NET SDK; the same command
registers its included DLL. A source checkout first builds the driver, then
registers it.

In the ASCOM camera chooser select **Open UVC Color Camera**, use **Setup**, and
choose the UVC camera index. Then select that camera in SharpCap's ASCOM camera
source. To remove the registration, run:

```powershell
.\install.ps1 -Uninstall
```

## Test on the target machine

After installation and with the desired UVC camera connected, run:

```powershell
.\tests\SharpCapSmokeTest.ps1 -ExposureSeconds 0.1
```

It checks that a capture completes and that the returned ASCOM array is rank 3
with three RGB planes. Finally, test in SharpCap: connect, take a 0.1-second
exposure, verify natural colour, then repeat at the camera's supported longer
exposures.

## Known limitations

* UVC exposure semantics vary by device and Media Foundation driver. The driver
  sends the common log2(seconds) exposure setting and waits for the requested
  duration; it cannot promise true sensor integration for every webcam.
* It does not advertise gain, cooling, binning, ROI/subframes, shutter, or
  sensor pitch because generic UVC does not standardize those properties well.
* Camera selection is an index, so it can change when USB devices are added or
  removed. Use Setup again if capture opens the wrong device.
* There is no hardware validation in this repository. The supplied smoke test
  must be run on a Windows 11 ARM machine with the target camera.
