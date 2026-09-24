using System;
using System.Runtime.InteropServices;

namespace PommeBar;

public static class VolumeController
{
    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorComObject { }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr ppDevices);
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppDevice);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        int RegisterControlChangeNotify(IntPtr pNotify);
        int UnregisterControlChangeNotify(IntPtr pNotify);
        int GetChannelCount(out uint pnChannelCount);
        int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
        int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
        int GetMasterVolumeLevel(out float pfLevelDB);
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
        int GetMute(out bool pbMute);
    }

    private static IAudioEndpointVolume? GetVolumeService()
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            enumerator.GetDefaultAudioEndpoint(0, 1, out IMMDevice dev);
            var iid = typeof(IAudioEndpointVolume).GUID;
            dev.Activate(ref iid, 1, IntPtr.Zero, out object volumeObj);
            return (IAudioEndpointVolume)volumeObj;
        }
        catch
        {
            return null;
        }
    }

    public static float GetVolume()
    {
        try
        {
            var vol = GetVolumeService();
            if (vol != null)
            {
                vol.GetMasterVolumeLevelScalar(out float level);
                return level;
            }
        }
        catch { }
        return 0.5f;
    }

    public static void SetVolume(float level)
    {
        try
        {
            var vol = GetVolumeService();
            if (vol != null)
            {
                Guid empty = Guid.Empty;
                vol.SetMasterVolumeLevelScalar(Math.Clamp(level, 0f, 1f), ref empty);
            }
        }
        catch { }
    }

    public static void ChangeVolume(float delta)
    {
        float current = GetVolume();
        SetVolume(current + delta);
    }
}
