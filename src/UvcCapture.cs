using System;
using System.Threading;
using OpenCvSharp;

namespace FibonaxUvcAscom;

/// <summary>Small, replaceable UVC/MSMF capture boundary. No vendor SDK or binary is used.</summary>
internal sealed class UvcCapture : IDisposable
{
    private readonly VideoCapture capture;

    public UvcCapture(int deviceIndex)
    {
        capture = new VideoCapture(deviceIndex, VideoCaptureAPIs.MSMF);
        if (!capture.IsOpened())
            throw new InvalidOperationException($"Could not open UVC camera index {deviceIndex} through Media Foundation.");
    }

    public (int Width, int Height) Size
    {
        get
        {
            using var probe = ReadFrame();
            return (probe.Width, probe.Height);
        }
    }

    public int[,,] CaptureRgb(double durationSeconds, CancellationToken cancellationToken)
    {
        // UVC exposure values conventionally use log2(seconds). Drivers differ, so this is
        // best effort and the caller can still request a timed frame from any UVC webcam.
        var uvcExposure = -Math.Log(Math.Max(durationSeconds, 1.0 / 10000.0), 2.0);
        capture.Set(VideoCaptureProperties.Exposure, uvcExposure);

        var deadline = DateTime.UtcNow.AddSeconds(Math.Max(0.0, durationSeconds));
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Thread.Sleep(10);
        }

        using var frame = ReadFrame();
        if (frame.Type() != MatType.CV_8UC3)
            throw new InvalidOperationException($"UVC backend returned {frame.Type()}, not an 8-bit BGR colour frame.");

        // ASCOM color ImageArray: [x, y, plane], plane 0=R, 1=G, 2=B.
        var result = new int[frame.Width, frame.Height, 3];
        for (var y = 0; y < frame.Height; y++)
        for (var x = 0; x < frame.Width; x++)
        {
            var bgr = frame.At<Vec3b>(y, x);
            result[x, y, 0] = bgr.Item2;
            result[x, y, 1] = bgr.Item1;
            result[x, y, 2] = bgr.Item0;
        }
        return result;
    }

    private Mat ReadFrame()
    {
        var frame = new Mat();
        // Drop a few stale frames so an exposure follows the requested setting.
        for (var i = 0; i < 3; i++) capture.Grab();
        if (!capture.Read(frame) || frame.Empty())
        {
            frame.Dispose();
            throw new InvalidOperationException("No frame was received from the UVC camera.");
        }
        return frame;
    }

    public void Dispose() => capture.Dispose();
}
