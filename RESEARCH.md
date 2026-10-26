# Interoperability research record

## Acquisition and static inspection

The public release asset requested was downloaded on 2026-10-25 from the
FIBONAX-BETTER GitHub Releases page:

* `FIBONAX_ASCOM_Camera_Driver_Setup_v1.0.0.zip`
  SHA-256 `ac6bc7bdc19173e61e471843e29da277164f1442c8ed161be6b6b9d70c3428df`
* The archive contains `FIBONAX_ASCOM_Camera_Driver_Setup_v1.0.0.exe`
  (SHA-256 `0dc3b58086ce200da2588fc4bdd337001c337ba36bdcbe3af51ab1c838ce60b7`)
  and a vendor PDF manual.

`file` identifies the executable as `PE32 executable (GUI) Intel 80386`.
Static strings identify `Inno Setup Setup Data (6.7.0)`. This establishes that
the supplied EXE is a native 32-bit installer, **not** a directly inspectable
.NET driver assembly. It does not establish what language the installer payload
uses. The payload was not redistributed or used as source.

## REA result and boundary

The public `morluto/rea` project (MIT) was retrieved and its documentation
reviewed. REA supports static .NET metadata inspection directly, but native
analysis requires a configured provider such as Hopper, Ghidra, IDA, or Kuna.
REA 6.8.0's diagnostic was run: its native providers (Hopper, Ghidra, IDA and
Kuna) were all unavailable, and its Node requirement also missed the installed
patch version. No payload extractor was available either. Therefore no
decompilation, code recovery, binary patching, or claims about private FIBONAX
capture/exposure algorithms were made.

The only capture-related inference retained is an interoperability requirement:
the vendor's public release description says supported cameras are UVC and lists
SharpCap direct UVC and ASCOM modes. This project consequently uses the public
Windows UVC/Media Foundation path, not a recovered vendor protocol.

## Design decisions

* The ASCOM implementation uses the documented `ICameraV2` contract and reports
  `SensorType.Color`.
* `ImageArray` is a three-dimensional integer array `[x, y, RGB]`; this is the
  ASCOM colour form and is the direct correction for a grayscale-only array.
* OpenCV receives standard BGR frames from Media Foundation and explicitly maps
  them to ASCOM RGB planes.
* Exposure uses the widely used UVC log2(seconds) convention as a best-effort
  setting. Device-specific values must be verified on physical hardware.

## Reproducibility and licences

This project is MIT-licensed. Its build restores `ASCOM.DeviceInterfaces`
7.1.2 and OpenCvSharp packages from NuGet. Consult those packages' notices
before redistributing a compiled installer. REA is MIT licensed, but no REA code
is incorporated here. The official vendor ZIP and PDF remain local research
inputs only and are intentionally absent from this deliverable.

## Verification performed

The source restored and compiled cleanly with .NET SDK 8.0.427 using the
`net481` target and `PlatformTarget=x64`; the resulting assembly is a PE32+
x86-64 .NET DLL. That verifies the ASCOM interface and source-level build, but
does **not** verify camera capture, COM registration, SharpCap integration, or
Windows 11 ARM emulation. Run `tests/SharpCapSmokeTest.ps1` on the target system
before use.
