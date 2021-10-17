using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Microsoft.Shell;

namespace AcerUserSensing
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance("AcerUserSensingInstance"))
            {
                var application = new App();
                application.Init();
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
        }
        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            //TODO: handle command line arguments
            Process cur = Process.GetCurrentProcess();

            foreach (Process p in Process.GetProcessesByName(cur.ProcessName))
            {
                //set focus
                NativeMethods.SetForegroundWindow(p.MainWindowHandle);
                //restore window if it was minimized
                NativeMethods.ShowWindow(p.MainWindowHandle, WindowShowStyle.Restore);
            }

            return true;
        }

        public void Init()
        {
            this.InitializeComponent();
            this.ApplyMultiLanguageResource();
        }

		private void ApplyMultiLanguageResource()
		{
            //Change language
			//System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en");
		}

        private static class NativeMethods
        {
#if (DEBUG)
            [DllImport("kernel32.dll")]
            public static extern Boolean AllocConsole();
            [DllImport("kernel32.dll")]
            public static extern Boolean FreeConsole();
#endif
            [DllImport("user32.dll")]
            internal static extern bool SetForegroundWindow(IntPtr hWnd);

            [DllImport("user32.dll")]
            internal static extern bool ShowWindow(IntPtr hWnd,
                WindowShowStyle nCmdShow);
        }
        internal enum WindowShowStyle : uint
        {
            Hide = 0,
            ShowNormal = 1,
            ShowMinimized = 2,
            ShowMaximized = 3,
            Maximize = 3,
            ShowNormalNoActivate = 4,
            Show = 5,
            Minimize = 6,
            ShowMinNoActivate = 7,
            ShowNoActivate = 8,
            Restore = 9,
            ShowDefault = 10,
            ForceMinimized = 11
        }
    }
}
