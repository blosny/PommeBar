using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace PommeBar;

public static class SpotifyAutomation
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    private const byte VK_MENU = 0x12;    // Alt
    private const byte VK_SHIFT = 0x10;   // Shift
    private const byte VK_B = 0x42;       // 'B'
    private const uint KEYEVENTF_KEYUP = 0x0002;

    public static async Task<bool> ToggleFavoriteAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var processes = Process.GetProcessesByName("Spotify");
                if (processes.Length == 0) return false;

                var root = AutomationElement.RootElement;
                if (root == null) return false;

                var procIds = processes.Select(p => p.Id).ToHashSet();
                
                var topWindows = root.FindAll(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)
                );

                AutomationElement? spotifyWindow = null;
                foreach (AutomationElement win in topWindows)
                {
                    try
                    {
                        if (procIds.Contains(win.Current.ProcessId))
                        {
                            spotifyWindow = win;
                            break;
                        }
                    }
                    catch { }
                }

                if (spotifyWindow != null)
                {
                    // Search for like / save / favorite button
                    var buttons = spotifyWindow.FindAll(
                        TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)
                    );

                    foreach (AutomationElement btn in buttons)
                    {
                        try
                        {
                            string name = (btn.Current.Name ?? "").ToLowerInvariant();
                            string autoId = (btn.Current.AutomationId ?? "").ToLowerInvariant();

                            if (name.Contains("save to your library") ||
                                name.Contains("remove from your library") ||
                                name.Contains("add to liked songs") ||
                                name.Contains("remove from liked songs") ||
                                name.Contains("beğenilen") ||
                                name.Contains("beğen") ||
                                name.Contains("like") ||
                                autoId.Contains("like") ||
                                autoId.Contains("heart"))
                            {
                                if (btn.TryGetCurrentPattern(InvokePattern.Pattern, out object invokePattern))
                                {
                                    ((InvokePattern)invokePattern).Invoke();
                                    return true;
                                }
                            }
                        }
                        catch { }
                    }
                }

                // Fallback: If UI Automation didn't catch CEF button, send Spotify's native shortcut Alt+Shift+B
                var mainSpotify = processes.FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
                if (mainSpotify != null)
                {
                    SetForegroundWindow(mainSpotify.MainWindowHandle);
                    keybd_event(VK_MENU, 0, 0, 0);
                    keybd_event(VK_SHIFT, 0, 0, 0);
                    keybd_event(VK_B, 0, 0, 0);
                    keybd_event(VK_B, 0, KEYEVENTF_KEYUP, 0);
                    keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, 0);
                    keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, 0);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SpotifyAutomation error: {ex.Message}");
                return false;
            }
        });
    }
}
