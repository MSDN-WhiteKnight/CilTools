/* CIL Tools 
 * Copyright (c) 2021, MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CilView.Common
{
    public static class Audio
    {
        static ISimpleAudioVolume volume = null;
        static bool firstrun = true;

        internal static ISimpleAudioVolume GetVolumeObject(int pid)
        {
            int hr=0;
            IMMDeviceEnumerator deviceEnumerator = null;
            IMMDevice speakers = null;
            IAudioSessionManager2 mgr = null;
            IAudioSessionEnumerator sessionEnumerator=null;
            ISimpleAudioVolume volumeControl = null;
            IAudioSessionControl ctl = null;
            IAudioSessionControl2 ctl2 = null;

            try
            {
                // get default render device
                deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                hr = deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);
                if (hr != 0) Marshal.ThrowExceptionForHR(hr);

                // activate the session manager. we need the enumerator
                Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
                object o;
                hr = speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
                if (hr != 0) Marshal.ThrowExceptionForHR(hr);
                mgr = (IAudioSessionManager2)o;

                // enumerate sessions for on this device
                hr = mgr.GetSessionEnumerator(out sessionEnumerator);
                if (hr != 0) Marshal.ThrowExceptionForHR(hr);

                int count;
                hr = sessionEnumerator.GetCount(out count);
                if (hr != 0) Marshal.ThrowExceptionForHR(hr);

                uint val;

                //get session for the specified process
                for (int i = 0; i < count; i++)
                {
                    hr = sessionEnumerator.GetSession(i, out ctl);
                    if (hr != 0) continue;

                    ctl2 = (IAudioSessionControl2)ctl;
                    val = 0;
                    ctl2.GetProcessId(out val);

                    if (val == pid)
                    {
                        volumeControl = ctl as ISimpleAudioVolume;
                        break;
                    }

                    SafeRelease(ref ctl);
                    SafeRelease(ref ctl2);
                }
            }
            finally
            {
                SafeRelease(sessionEnumerator);
                SafeRelease(mgr);
                SafeRelease(speakers);
                SafeRelease(deviceEnumerator);
                SafeRelease(ctl);
                SafeRelease(ctl2);
            }

            return volumeControl;
        }

        internal static void SafeRelease<T>(ref T obj) where T:class
        {
            if (obj != null)
            {
                Marshal.ReleaseComObject(obj);
                obj = null;
            }
        }

        internal static void SafeRelease(object obj)
        {
            if (obj != null)
            {
                Marshal.ReleaseComObject(obj);
            }
        }

        /// <summary>
        /// Gets volume control for process's default WASAPI session
        /// </summary>
        internal static ISimpleAudioVolume GetDefaultVolumeObject()
        {
            IMMDeviceEnumerator deviceEnumerator = null;
            IMMDevice device = null;
            ISimpleAudioVolume volumeControl = null;
            IAudioSessionManager mgr = null;
            object o;
            int hr = 0;

            try
            {
                // get default render device
                deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
                deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out device);

                // activate the session manager
                Guid IID_IAudioSessionManager = typeof(IAudioSessionManager).GUID;
                hr=device.Activate(ref IID_IAudioSessionManager, 0, IntPtr.Zero, out o);
                if (hr != 0) Marshal.ThrowExceptionForHR(hr);
                mgr = (IAudioSessionManager)o;

                // get volume control for default session
                Guid guidNull = new Guid();
                o = null;
                hr=mgr.GetSimpleAudioVolume(ref guidNull, 0, out o);

                volumeControl = (ISimpleAudioVolume)o;
            }
            finally
            {
                SafeRelease(mgr);
                SafeRelease(device);
                SafeRelease(deviceEnumerator);
            }

            return volumeControl;
        }

        internal static ISimpleAudioVolume GetVolumeObject()
        {
            if (volume != null) return volume;

            ISimpleAudioVolume vol = GetDefaultVolumeObject();

            if (vol == null)
            {
                int id = 0;

                using (Process proc = Process.GetCurrentProcess())
                {
                    id = proc.Id;
                }

                vol = GetVolumeObject(id);
            }

            volume = vol;
            return vol;
        }

        public static void SetMute(bool val)
        {
            try
            {
                ISimpleAudioVolume vol = GetVolumeObject();

                if (vol != null)
                {
                    Guid guid = new Guid();
                    vol.SetMute(val, ref guid);
                    firstrun = false;
                }
            }
            catch (Exception ex)
            {
                //log error only on first run
                if (firstrun)
                {
                    ErrorHandler.Current.Error(ex, "Audio.SetMute", silent:true);
                    firstrun = false;
                }
            }//end try
        }
    }

    [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioSessionControl2
    {
        [PreserveSig]
        int GetState(out object state);
        [PreserveSig]
        int GetDisplayName(out IntPtr name);
        [PreserveSig]
        int SetDisplayName(string value, Guid EventContext);
        [PreserveSig]
        int GetIconPath(out IntPtr Path);
        [PreserveSig]
        int SetIconPath(string Value, Guid EventContext);
        [PreserveSig]
        int GetGroupingParam(out Guid GroupingParam);
        [PreserveSig]
        int SetGroupingParam(Guid Override, Guid Eventcontext);
        [PreserveSig]
        int RegisterAudioSessionNotification(object NewNotifications);
        [PreserveSig]
        int UnregisterAudioSessionNotification(object NewNotifications);

        [PreserveSig]
        int GetSessionIdentifier(out IntPtr retVal);
        [PreserveSig]
        int GetSessionInstanceIdentifier(out IntPtr retVal);
        [PreserveSig]
        int GetProcessId(out UInt32 retvVal);
        [PreserveSig]
        int IsSystemSoundsSession();
        [PreserveSig]
        int SetDuckingPreference(bool optOut);
    }

    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ISimpleAudioVolume
    {
        [PreserveSig]
        int SetMasterVolume(float fLevel, ref Guid EventContext);

        [PreserveSig]
        int GetMasterVolume(out float pfLevel);

        [PreserveSig]
        int SetMute(bool bMute, ref Guid EventContext);

        [PreserveSig]
        int GetMute(out bool pbMute);
    }

    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionEnumerator
    {
        [PreserveSig]
        int GetCount(out int SessionCount);

        [PreserveSig]
        int GetSession(int SessionCount, out IAudioSessionControl Session);
    }

    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionControl
    {
        int NotImpl1();

        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        // the rest is not implemented
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, 
            [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface
            );

        // the rest is not implemented
    }

    [Guid("BFA971F1-4D5E-40BB-935E-967039BFBEE4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionManager
    {
        [PreserveSig]
        int GetAudioSessionControl(
            ref Guid AudioSessionGuid,
            uint StreamFlags,
            [MarshalAs(UnmanagedType.IUnknown)] out object SessionControl //IAudioSessionControl
            );

        [PreserveSig]
        int GetSimpleAudioVolume(
            ref Guid AudioSessionGuid,
            uint StreamFlags,
            [MarshalAs(UnmanagedType.IUnknown)] out object AudioVolume //ISimpleAudioVolume
            );
    }

    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionManager2
    {
        int NotImpl1();
        int NotImpl2();

        [PreserveSig]
        int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

        // the rest is not implemented
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    internal enum EDataFlow
    {
        eRender,
        eCapture,
        eAll,
        EDataFlow_enum_count
    }

    internal enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications,
        ERole_enum_count
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        int NotImpl1();

        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);

        // the rest is not implemented
    }
}
