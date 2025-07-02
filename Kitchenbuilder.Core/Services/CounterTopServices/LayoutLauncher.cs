using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
/// <summary>
/// LayoutLauncher handles launching and positioning SolidWorks alongside the MAUI app.
/// 
/// Functionality:
/// - Opens a specific .SLDPRT file using the SolidWorks API.
/// - Registers the active model in the shared session service (SwSession).
/// - Splits the screen:
///     - SolidWorks is moved to the left half of the screen (0–960).
///     - The MAUI app window is moved to the right half of the screen (960–1920).
/// - Logs all actions and errors to a debug file.
/// 
/// Usage:
/// Call LaunchAndSplitScreen(swSession, pathToSldprt) from the UI layer, passing:
/// - swSession: shared SolidWorks session handler
/// - pathToSldprt: full path to the SLDPRT file to be opened
/// 
/// Debug logs are written to: Kitchenbuilder\Output\split_screen_debug.txt
/// </summary>

namespace Kitchenbuilder.Core
{
    public static class LayoutLauncher
    {
        public static List<string> StationSketches { get; set; } = new();

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void LaunchAndSplitScreen(SolidWorksSessionService swSession, string sldprtPath)
        {
            if (!File.Exists(sldprtPath))
            {
                Log($"❌ SLDPRT file not found: {sldprtPath}");
                return;
            }

            // ✅ Get existing SolidWorks app
            ISldWorks swApp;

            try
            {
                swApp = swSession.GetApp(); // ⬅️ This avoids creating a new instance
            }
            catch
            {
                swApp = (ISldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
                swApp.Visible = true;
                swSession.SetApp(swApp); // ✅ Store for reuse
                Log("🆕 SolidWorks launched and stored in session.");
            }

            int errors = 0, warnings = 0;
            var model = swApp.OpenDoc6(sldprtPath,
                (int)swDocumentTypes_e.swDocPART,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                "", ref errors, ref warnings);

            if (model == null)
            {
                Log("❌ Failed to open the SLDPRT file.");
                return;
            }

            swSession.SetActiveModel(model);
            Log("✅ Active model registered in session.");

            IntPtr swHandle = (IntPtr)swApp.IFrameObject().GetHWnd();
            MoveWindow(swHandle, 0, 0, 960, 1080, true);
            Log("✅ Moved SolidWorks to left side.");

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
