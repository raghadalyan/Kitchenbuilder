using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Kitchenbuilder.Core
{
    public static class LayoutLauncher
    {
        public static List<string> StationSketches { get; set; } = new();

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void LaunchAndSplitScreen(SolidWorksSessionService swSession)
        {
            string folder = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\temp";
            string latestFile = Directory.GetFiles(folder, "temp_Option*.SLDPRT")
                                         .OrderByDescending(File.GetLastWriteTime)
                                         .FirstOrDefault();

            if (latestFile == null)
            {
                Log("❌ No SLDPRT file found.");
                return;
            }

            Log($"✅ Found latest SLDPRT file: {Path.GetFileName(latestFile)}");

            // Start SolidWorks via API
            var swApp = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
            if (swApp == null)
            {
                Log("❌ Failed to launch SolidWorks via API.");
                return;
            }

            swApp.Visible = true;

            int errors = 0, warnings = 0;
            var model = swApp.OpenDoc6(latestFile,
                (int)swDocumentTypes_e.swDocPART,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                "", ref errors, ref warnings);

            if (model == null)
            {
                Log("❌ Failed to open the SLDPRT file.");
                return;
            }

            // Register active model in the session
            swSession.SetActiveModel(model);
            Log("✅ Active model registered in session.");

            // Move SolidWorks to left
            IntPtr swHandle = (IntPtr)swApp.IFrameObject().GetHWnd();
            MoveWindow(swHandle, 0, 0, 960, 1080, true);
            Log("✅ Moved SolidWorks to left side.");

            // Move MAUI to right
            Process currentProc = Process.GetCurrentProcess();
            IntPtr currentHandle = currentProc.MainWindowHandle;
            for (int i = 0; i < 10 && currentHandle == IntPtr.Zero; i++)
            {
                Thread.Sleep(500);
                currentHandle = currentProc.MainWindowHandle;
            }

            if (currentHandle != IntPtr.Zero)
            {
                MoveWindow(currentHandle, 960, 0, 960, 1080, true);
                Log("✅ Moved Kitchenbuilder to right side.");
            }
            else
            {
                Log("❌ Failed to get Kitchenbuilder window handle.");
            }
        }

        private static void Log(string message)
        {
            string debugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\split_screen_debug.txt";
            string logLine = $"[{DateTime.Now:HH:mm:ss}] {message}";
            File.AppendAllText(debugPath, logLine + System.Environment.NewLine);
        }
    }
}
