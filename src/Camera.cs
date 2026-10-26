using System;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using ASCOM.DeviceInterface;

namespace FibonaxUvcAscom;

/// <summary>
/// Independently written ASCOM Camera V2 implementation for standard UVC devices.
/// It deliberately does not identify itself as, or depend upon, a vendor driver.
/// </summary>
[ComVisible(true)]
[Guid("A3CC8374-52F2-4EFA-8F15-A88BF7B39B19")]
[ProgId(DriverProgId)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class Camera : ICameraV2
{
    public const string DriverProgId = "ASCOM.OpenUvcColor.Camera";
    private const string ProfilePath = @"Software\\OpenUvcColorAscom";
    private readonly object gate = new();
    private UvcCapture? capture;
    private CancellationTokenSource? exposureCancellation;
    private Task? exposureTask;
    private int[,,]? image;
    private CameraStates state = CameraStates.cameraIdle;
    private int cameraIndex;
    private int width, height;
    private double lastDuration;
    private string lastStart = "";

    public Camera() => cameraIndex = LoadCameraIndex();

    [ComRegisterFunction]
    public static void Register(Type _) => WriteAscomProfile();
    [ComUnregisterFunction]
    public static void Unregister(Type _) => Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\\ASCOM\\Camera Drivers\\" + DriverProgId, false);

    private static void WriteAscomProfile()
    {
        using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\\ASCOM\\Camera Drivers\\" + DriverProgId);
        key?.SetValue("Driver", DriverProgId);
        key?.SetValue("Description", "Open UVC Color Camera (independent compatibility driver)");
    }

    public bool Connected
    {
        get { lock (gate) return capture != null; }
        set
        {
            lock (gate)
            {
                if (value == (capture != null)) return;
                if (value)
                {
                    capture = new UvcCapture(cameraIndex);
                    (width, height) = capture.Size;
                }
                else Disconnect();
            }
        }
    }

    public string Description => "Independent standard-UVC color camera bridge (Media Foundation/OpenCV).";
    public string DriverInfo => "Open UVC Color ASCOM Camera 0.1. No FIBONAX code, SDK, firmware, or installer is included.";
    public string DriverVersion => "0.1";
    public short InterfaceVersion => 2;
    public string Name => "Open UVC Color Camera";
    public ArrayList SupportedActions => new() { "GetCaptureDiagnostics" };

    public void SetupDialog()
    {
        if (Connected) { MessageBox.Show("Disconnect the camera before changing its UVC index.", Name); return; }
        using var form = new SetupForm(cameraIndex);
        if (form.ShowDialog() == DialogResult.OK) { cameraIndex = form.DeviceIndex; SaveCameraIndex(cameraIndex); }
    }

    public string Action(string actionName, string actionParameters)
    {
        if (string.Equals(actionName, "GetCaptureDiagnostics", StringComparison.OrdinalIgnoreCase))
            return $"backend=MSMF; cameraIndex={cameraIndex}; connected={Connected}; size={width}x{height}; colour=RGB; state={state}";
        throw new NotSupportedException($"Action '{actionName}' is not supported.");
    }
    public void CommandBlind(string command, bool raw = false) => throw new NotSupportedException();
    public bool CommandBool(string command, bool raw = false) => throw new NotSupportedException();
    public string CommandString(string command, bool raw = false) => throw new NotSupportedException();

    public void AbortExposure() { lock (gate) { exposureCancellation?.Cancel(); state = CameraStates.cameraIdle; } }
    public short BinX { get => 1; set { if (value != 1) throw new ArgumentOutOfRangeException(nameof(value), "UVC binning is unavailable."); } }
    public short BinY { get => 1; set { if (value != 1) throw new ArgumentOutOfRangeException(nameof(value), "UVC binning is unavailable."); } }
    public CameraStates CameraState { get { lock (gate) return state; } }
    public int CameraXSize { get { RequireConnection(); return width; } }
    public int CameraYSize { get { RequireConnection(); return height; } }
    public bool CanAbortExposure => true;
    public bool CanAsymmetricBin => false;
    public bool CanGetCoolerPower => false;
    public bool CanPulseGuide => false;
    public bool CanSetCCDTemperature => false;
    public bool CanStopExposure => true;
    public double CCDTemperature => throw new NotSupportedException();
    public bool CoolerOn { get => false; set => throw new NotSupportedException(); }
    public double CoolerPower => throw new NotSupportedException();
    public double ElectronsPerADU => 1.0;
    public double FullWellCapacity => 255;
    public bool HasShutter => false;
    public double HeatSinkTemperature => throw new NotSupportedException();
    public object ImageArray { get { lock (gate) return image ?? throw new InvalidOperationException("No completed exposure."); } }
    public object ImageArrayVariant => ImageArray;
    public bool ImageReady { get { lock (gate) return image != null && exposureTask?.IsCompleted != false; } }
    public bool IsPulseGuiding => false;
    public double LastExposureDuration => lastDuration;
    public string LastExposureStartTime => lastStart;
    public int MaxADU => 255;
    public short MaxBinX => 1;
    public short MaxBinY => 1;
    public int NumX { get { RequireConnection(); return width; } set { RequireFullFrame(value, width, nameof(NumX)); } }
    public int NumY { get { RequireConnection(); return height; } set { RequireFullFrame(value, height, nameof(NumY)); } }
    public double PixelSizeX => 0; // UVC does not expose sensor pitch reliably.
    public double PixelSizeY => 0;
    public void PulseGuide(GuideDirections direction, int duration) => throw new NotSupportedException();
    public double SetCCDTemperature { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public int StartX { get => 0; set { if (value != 0) throw new ArgumentOutOfRangeException(nameof(value)); } }
    public int StartY { get => 0; set { if (value != 0) throw new ArgumentOutOfRangeException(nameof(value)); } }
    public short BayerOffsetX => 0;
    public short BayerOffsetY => 0;
    public bool CanFastReadout => false;
    public double ExposureMax => 60.0;
    public double ExposureMin => 0.0001;
    public double ExposureResolution => 0.0001;
    public bool FastReadout { get => false; set => throw new NotSupportedException(); }
    public short Gain { get => 0; set => throw new NotSupportedException("Generic UVC gain is intentionally not advertised until capability probing is implemented."); }
    public short GainMax => 0;
    public short GainMin => 0;
    public ArrayList Gains => new();
    public short PercentCompleted => exposureTask is { IsCompleted: false } ? (short)50 : (short)100;
    public short ReadoutMode { get => 0; set { if (value != 0) throw new ArgumentOutOfRangeException(nameof(value)); } }
    public ArrayList ReadoutModes => new() { "RGB 8-bit UVC" };
    public string SensorName => "UVC colour sensor (model reported by Windows)";
    public SensorType SensorType => SensorType.Color;

    public void StartExposure(double duration, bool light)
    {
        RequireConnection();
        if (duration < ExposureMin || duration > ExposureMax) throw new ArgumentOutOfRangeException(nameof(duration));
        lock (gate)
        {
            if (exposureTask is { IsCompleted: false }) throw new InvalidOperationException("An exposure is already in progress.");
            image = null; lastDuration = duration; lastStart = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
            exposureCancellation = new CancellationTokenSource(); state = CameraStates.cameraExposing;
            var token = exposureCancellation.Token;
            exposureTask = Task.Run(() =>
            {
                try
                {
                    var captured = capture!.CaptureRgb(duration, token);
                    lock (gate) { image = captured; state = CameraStates.cameraIdle; }
                }
                catch (OperationCanceledException) { lock (gate) state = CameraStates.cameraIdle; }
                catch { lock (gate) state = CameraStates.cameraError; throw; }
            }, token);
        }
    }
    public void StopExposure() => AbortExposure();

    public void Dispose() { lock (gate) Disconnect(); GC.SuppressFinalize(this); }
    private void Disconnect() { exposureCancellation?.Cancel(); capture?.Dispose(); capture = null; image = null; state = CameraStates.cameraIdle; }
    private void RequireConnection() { if (!Connected) throw new InvalidOperationException("Camera is not connected."); }
    private static void RequireFullFrame(int value, int size, string name) { if (value != size) throw new ArgumentOutOfRangeException(name, "UVC subframes are not implemented."); }
    private static int LoadCameraIndex() { using var key = Registry.CurrentUser.OpenSubKey(ProfilePath); return key?.GetValue("CameraIndex", 0) as int? ?? 0; }
    private static void SaveCameraIndex(int value) { using var key = Registry.CurrentUser.CreateSubKey(ProfilePath); key.SetValue("CameraIndex", value); }
}
