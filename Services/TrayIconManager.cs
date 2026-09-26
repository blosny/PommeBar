using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PommeBar;

public class TrayIconManager : IDisposable
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const int NIM_ADD = 0x00000000;
    private const int NIM_MODIFY = 0x00000001;
    private const int NIM_DELETE = 0x00000002;

    private const int NIF_MESSAGE = 0x00000001;
    private const int NIF_ICON = 0x00000002;
    private const int NIF_TIP = 0x00000004;

    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 101;

    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONUP = 0x0205;

    private readonly Window _targetWindow;
    private IntPtr _hwnd;
    private HwndSource? _hwndSource;
    private bool _isAdded = false;

    public event Action? OnLeftClick;
    public event Action? OnRightClick;

    public TrayIconManager(Window window)
    {
        _targetWindow = window;
    }

    public void Initialize()
    {
        var helper = new WindowInteropHelper(_targetWindow);
        _hwnd = helper.Handle;

        if (_hwnd == IntPtr.Zero)
        {
            _targetWindow.SourceInitialized += (s, e) =>
            {
                _hwnd = helper.Handle;
                SetupHook();
                AddTrayIcon();
            };
        }
        else
        {
            SetupHook();
            AddTrayIcon();
        }
    }

    private void SetupHook()
    {
        _hwndSource = HwndSource.FromHwnd(_hwnd);
        _hwndSource?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_TRAYICON)
        {
            int mouseMsg = lParam.ToInt32();
            if (mouseMsg == WM_LBUTTONUP)
            {
                OnLeftClick?.Invoke();
                handled = true;
            }
            else if (mouseMsg == WM_RBUTTONUP)
            {
                SetForegroundWindow(_hwnd);
                OnRightClick?.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern int ExtractIconEx(string lpszFile, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, int nIcons);

    private void AddTrayIcon()
    {
        if (_hwnd == IntPtr.Zero) return;

        try
        {
            IntPtr iconHandle = IntPtr.Zero;
            string? exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath) && System.IO.File.Exists(exePath))
            {
                ExtractIconEx(exePath, 0, out _, out iconHandle, 1);
            }
            if (iconHandle == IntPtr.Zero)
            {
                iconHandle = LoadIcon(IntPtr.Zero, (IntPtr)32512); // IDI_APPLICATION
            }

            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = _hwnd,
                uID = 1001,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                hIcon = iconHandle,
                szTip = "PommeBar • Media Controller"
            };

            _isAdded = Shell_NotifyIcon(NIM_ADD, ref nid);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to add tray icon: {ex.Message}");
        }
    }

    public void UpdateTooltip(string tooltip)
    {
        if (!_isAdded || _hwnd == IntPtr.Zero) return;
        try
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                hWnd = _hwnd,
                uID = 1001,
                uFlags = NIF_TIP,
                szTip = tooltip.Length > 120 ? tooltip.Substring(0, 120) : tooltip
            };
            Shell_NotifyIcon(NIM_MODIFY, ref nid);
        }
        catch { }
    }

    public void Dispose()
    {
        if (_isAdded && _hwnd != IntPtr.Zero)
        {
            try
            {
                var nid = new NOTIFYICONDATA
                {
                    cbSize = Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                    hWnd = _hwnd,
                    uID = 1001
                };
                Shell_NotifyIcon(NIM_DELETE, ref nid);
            }
            catch { }
            _isAdded = false;
        }

        if (_hwndSource != null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }
    }
}
