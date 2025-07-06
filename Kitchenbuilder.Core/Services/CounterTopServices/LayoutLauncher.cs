using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public static class LayoutLauncher
    {
        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void ArrangeWindows(ISldWorks swApp)
        {
            try
            {
                // Get main window handle of SolidWorks
                var swHandle = (IntPtr)swApp.IFrameObject().GetHWnd();

                // Get current screen size
                int screenWidth = GetSystemMetrics(0);
                int screenHeight = GetSystemMetrics(1);

                // Left half: MAUI (current process)
                var mauiHandle = Process.GetCurrentProcess().MainWindowHandle;
                MoveWindow(mauiHandle, 0, 0, screenWidth / 2, screenHeight, true);

                // Right half: SolidWorks
                MoveWindow(swHandle, screenWidth / 2, 0, screenWidth / 2, screenHeight, true);

                SetForegroundWindow(mauiHandle); // Bring MAUI to front
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to arrange windows: {ex.Message}");
            }
        }

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
    }
}
