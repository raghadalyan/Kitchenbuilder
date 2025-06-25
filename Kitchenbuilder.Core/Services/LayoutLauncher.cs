using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Kitchenbuilder.Core
{
    public static class LayoutLauncher
    {
        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        public static void LaunchAndSplitScreen()
        {
            string folder = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\temp";
            string solidWorksPath = @"C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\SLDWORKS.exe";
            string latestFile = Directory.GetFiles(folder, "temp_Option*.SLDPRT")
                                         .OrderByDescending(File.GetLastWriteTime)
                                         .FirstOrDefault();

            if (latestFile == null)
            {
                Log("❌ No SLDPRT file found.");
                return;
            }

            Log($"✅ Found latest SLDPRT file: {Path.GetFileName(latestFile)}");

            // Launch SolidWorks
            var swProc = Process.Start(solidWorksPath, $"\"{latestFile}\"");
            Thread.Sleep(5000);

            if (swProc != null)
            {
                IntPtr swHandle = swProc.MainWindowHandle;
                if (swHandle == IntPtr.Zero)
                    swHandle = WaitForMainWindow(swProc);

                if (swHandle != IntPtr.Zero)
                {
                    SetForegroundWindow(swHandle);
                    MoveWindow(swHandle, 0, 0, 960, 1080, true); // Left half
                    Log("✅ Moved SolidWorks to left side.");
                }
                else
                {
                    Log("❌ Failed to get SolidWorks window handle.");
                }
            }

            // Move current MAUI app to right side
            Process currentProc = Process.GetCurrentProcess();
            IntPtr currentHandle = currentProc.MainWindowHandle;

            for (int i = 0; i < 10 && currentHandle == IntPtr.Zero; i++)
            {
                Thread.Sleep(500);
                currentHandle = currentProc.MainWindowHandle;
            }

            if (currentHandle != IntPtr.Zero)
            {
                SetForegroundWindow(currentHandle);
                MoveWindow(currentHandle, 960, 0, 960, 1080, true); // Right half
                Log("✅ Moved Kitchenbuilder to right side.");
            }
            else
            {
                Log("❌ Failed to get Kitchenbuilder window handle.");
            }
        }

        private static IntPtr WaitForMainWindow(Process proc)
        {
            IntPtr handle = IntPtr.Zero;
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(1000);
                handle = proc.MainWindowHandle;
                if (handle != IntPtr.Zero)
                    break;
            }
            return handle;
        }

        private static void Log(string message)
        {
            string debugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\split_screen_debug.txt";
            string logLine = $"[{DateTime.Now:HH:mm:ss}] {message}";
            File.AppendAllText(debugPath, logLine + Environment.NewLine);
        }
    }
}
