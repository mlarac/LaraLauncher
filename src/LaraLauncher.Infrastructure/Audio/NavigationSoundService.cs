using System.Runtime.InteropServices;
using LaraLauncher.Core.Interfaces;

namespace LaraLauncher.Infrastructure.Audio;

/// <summary>
/// Synthesizes soft, short Roku-inspired navigation tones in memory and plays them
/// asynchronously via Win32 winmm.dll (no external audio files or NuGet packages needed).
/// </summary>
public sealed class NavigationSoundService : INavigationSoundService
{
    private const uint SND_ASYNC = 0x0001;
    private const uint SND_NODEFAULT = 0x0002;
    private const uint SND_MEMORY = 0x0004;

    private readonly GCHandle _moveHandle;
    private readonly GCHandle _selectHandle;
    private readonly GCHandle _backHandle;
    private bool _disposed;

    public bool IsEnabled { get; set; } = true;

    public NavigationSoundService()
    {
        byte[] moveWav = GenerateToneWav(frequencyHz: 520, durationMs: 22, amplitude: 0.12);
        byte[] selectWav = GenerateTwoToneWav(freq1Hz: 620, freq2Hz: 840, durationMs: 42, amplitude: 0.14);
        byte[] backWav = GenerateToneWav(frequencyHz: 340, durationMs: 30, amplitude: 0.12);

        _moveHandle = GCHandle.Alloc(moveWav, GCHandleType.Pinned);
        _selectHandle = GCHandle.Alloc(selectWav, GCHandleType.Pinned);
        _backHandle = GCHandle.Alloc(backWav, GCHandleType.Pinned);
    }

    public void PlayMove() => PlayPinned(_moveHandle);
    public void PlaySelect() => PlayPinned(_selectHandle);
    public void PlayBack() => PlayPinned(_backHandle);

    private void PlayPinned(GCHandle handle)
    {
        if (_disposed || !IsEnabled || !handle.IsAllocated)
        {
            return;
        }

        try
        {
            PlaySound(handle.AddrOfPinnedObject(), nint.Zero, SND_MEMORY | SND_ASYNC | SND_NODEFAULT);
        }
        catch (Exception)
        {
            // Audio device may be absent or disabled; ignore gracefully.
        }
    }

    public static byte[] GenerateToneWav(double frequencyHz, int durationMs, double amplitude)
        => GenerateTwoToneWav(frequencyHz, frequencyHz, durationMs, amplitude);

    public static byte[] GenerateTwoToneWav(double freq1Hz, double freq2Hz, int durationMs, double amplitude)
    {
        const int sampleRate = 22050;
        const short bitsPerSample = 16;
        const short channels = 1;

        int sampleCount = Math.Max(1, sampleRate * durationMs / 1000);
        int dataSize = sampleCount * channels * (bitsPerSample / 8);

        using var ms = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(ms);

        // Standard RIFF/WAVE PCM header
        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1); // PCM
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * (bitsPerSample / 8));
        writer.Write((short)(channels * (bitsPerSample / 8)));
        writer.Write(bitsPerSample);
        writer.Write("data"u8);
        writer.Write(dataSize);

        int half = sampleCount / 2;
        for (int i = 0; i < sampleCount; i++)
        {
            double t = (double)i / sampleRate;
            double progress = (double)i / sampleCount;
            double freq = i < half ? freq1Hz : freq2Hz;

            // Smooth attack and exponential decay envelope for soft, non-harsh UI clicks
            double attack = Math.Min(1.0, i / 40.0);
            double decay = Math.Pow(1.0 - progress, 2.2);
            double sample = Math.Sin(2.0 * Math.PI * freq * t) * amplitude * attack * decay;

            short pcm = (short)Math.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
            writer.Write(pcm);
        }

        return ms.ToArray();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try { PlaySound(nint.Zero, nint.Zero, 0); } catch (Exception) { }

        if (_moveHandle.IsAllocated) _moveHandle.Free();
        if (_selectHandle.IsAllocated) _selectHandle.Free();
        if (_backHandle.IsAllocated) _backHandle.Free();
    }

    [DllImport("winmm.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PlaySound(nint pszSound, nint hmod, uint fdwSound);
}
