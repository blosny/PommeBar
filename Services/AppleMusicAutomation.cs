using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace PommeBar;

public static class AppleMusicAutomation
{
    public static async Task<bool> ToggleFavoriteAsync()
    {
        return await Task.Run(() =>
        {
            try
            {
                var processes = Process.GetProcessesByName("AppleMusic");
                if (processes.Length == 0)
                {
                    Debug.WriteLine("AppleMusic process not found.");
                    return false;
                }

                var root = AutomationElement.RootElement;
                if (root == null) return false;

                var procIds = processes.Select(p => p.Id).ToHashSet();
                
                var topWindows = root.FindAll(
                    TreeScope.Children,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)
                );

                AutomationElement? appleMusicWindow = null;
                foreach (AutomationElement win in topWindows)
                {
                    try
                    {
                        if (procIds.Contains(win.Current.ProcessId))
                        {
                            appleMusicWindow = win;
                            break;
                        }
                    }
                    catch { }
                }

                if (appleMusicWindow == null)
                {
                    foreach (var pid in procIds)
                    {
                        try
                        {
                            var cond = new PropertyCondition(AutomationElement.ProcessIdProperty, pid);
                            var found = root.FindFirst(TreeScope.Children, cond);
                            if (found != null)
                            {
                                appleMusicWindow = found;
                                break;
                            }
                        }
                        catch { }
                    }
                }

                if (appleMusicWindow == null)
                {
                    Debug.WriteLine("Apple Music window not found in UI Automation tree.");
                    return false;
                }

                var buttonCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
                var buttons = appleMusicWindow.FindAll(TreeScope.Descendants, buttonCondition);

                AutomationElement? favoriteButton = null;

                foreach (AutomationElement btn in buttons)
                {
                    try
                    {
                        string name = btn.Current.Name ?? "";
                        string autoId = btn.Current.AutomationId ?? "";
                        string helpText = btn.Current.HelpText ?? "";

                        if (autoId.Contains("Favorite", StringComparison.OrdinalIgnoreCase) ||
                            autoId.Contains("Love", StringComparison.OrdinalIgnoreCase) ||
                            autoId.Contains("Heart", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Favori", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Favorite", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Beğen", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Love", StringComparison.OrdinalIgnoreCase) ||
                            helpText.Contains("Favori", StringComparison.OrdinalIgnoreCase) ||
                            helpText.Contains("Favorite", StringComparison.OrdinalIgnoreCase))
                        {
                            favoriteButton = btn;
                            break;
                        }
                    }
                    catch { }
                }

                if (favoriteButton != null)
                {
                    if (favoriteButton.TryGetCurrentPattern(InvokePattern.Pattern, out var invokeObj) &&
                        invokeObj is InvokePattern invokePattern)
                    {
                        invokePattern.Invoke();
                        return true;
                    }

                    if (favoriteButton.TryGetCurrentPattern(TogglePattern.Pattern, out var toggleObj) &&
                        toggleObj is TogglePattern togglePattern)
                    {
                        togglePattern.Toggle();
                        return true;
                    }
                }

                Debug.WriteLine("Favorite button not found or could not be invoked.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error toggling favorite: {ex.Message}");
                return false;
            }
        });
    }

    public static async Task<bool?> GetFavoriteStatusAsync()
    {
        return await Task.Run<bool?>(() =>
        {
            try
            {
                var processes = Process.GetProcessesByName("AppleMusic");
                if (processes.Length == 0) return null;

                var appleMusicWindow = AutomationElement.FromHandle(processes[0].MainWindowHandle);
                if (appleMusicWindow == null) return null;

                var buttonCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
                var buttons = appleMusicWindow.FindAll(TreeScope.Descendants, buttonCondition);

                foreach (AutomationElement btn in buttons)
                {
                    try
                    {
                        string name = btn.Current.Name ?? "";
                        string autoId = btn.Current.AutomationId ?? "";

                        if (autoId.Contains("Favorite", StringComparison.OrdinalIgnoreCase) ||
                            autoId.Contains("Love", StringComparison.OrdinalIgnoreCase) ||
                            autoId.Contains("Heart", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Favori", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Favorite", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Beğen", StringComparison.OrdinalIgnoreCase) ||
                            name.Contains("Love", StringComparison.OrdinalIgnoreCase))
                        {
                            if (btn.TryGetCurrentPattern(TogglePattern.Pattern, out var toggleObj) &&
                                toggleObj is TogglePattern togglePattern)
                            {
                                return togglePattern.Current.ToggleState == ToggleState.On;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        });
    }
}
