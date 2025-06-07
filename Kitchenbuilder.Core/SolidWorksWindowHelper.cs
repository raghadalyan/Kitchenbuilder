using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Kitchenbuilder.Core
{
    public static class SolidWorksWindowHelper
    {
        private const int SW_RESTORE = 9;
        private const int SW_SHOWMAXIMIZED = 3;
        private const int SWP_SHOWWINDOW = 0x0040;

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private static readonly IntPtr HWND_TOP = IntPtr.Zero;

        public static void ArrangeBothWindows()
        {
            var thisProcess = Process.GetCurrentProcess();
            IntPtr thisHandle = thisProcess.MainWindowHandle;

            Process[] solidWorksProcesses = Process.GetProcessesByName("SLDWORKS");
            if (solidWorksProcesses.Length == 0)
            {
                Console.WriteLine("SolidWorks is not running.");
                return;
            }

            IntPtr solidWorksHandle = solidWorksProcesses[0].MainWindowHandle;

            ShowWindow(thisHandle, SW_RESTORE);
            ShowWindow(solidWorksHandle, SW_RESTORE);

            // Use Win32 API to get screen size
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            int halfWidth = screenWidth / 2;

            SetWindowPos(thisHandle, HWND_TOP, 0, 0, halfWidth, screenHeight, SWP_SHOWWINDOW);
            SetWindowPos(solidWorksHandle, HWND_TOP, halfWidth, 0, halfWidth, screenHeight, SWP_SHOWWINDOW);

            SetForegroundWindow(thisHandle);
            SetForegroundWindow(solidWorksHandle);
        }
    }
}
