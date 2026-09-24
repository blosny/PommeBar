using System;
using System.IO;
using System.Windows;

namespace PommeBar;

public partial class App : Application
{
    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                File.AppendAllText(@"C:\projects\pomme-bar\crash.log", $"[AppDomain Unhandled] {DateTime.Now}: {e.ExceptionObject}\n");
            }
            catch { }
        };

        this.DispatcherUnhandledException += (s, e) =>
        {
            try
            {
                File.AppendAllText(@"C:\projects\pomme-bar\crash.log", $"[Dispatcher Unhandled] {DateTime.Now}: {e.Exception}\n");
            }
            catch { }
        };
    }
}
