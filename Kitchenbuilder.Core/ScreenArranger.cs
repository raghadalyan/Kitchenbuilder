using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Kitchenbuilder.Core
{
    public static class ScreenArranger
    {
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        public static void ArrangeScreen(List<string> filePaths)
        {
            int projectCount = filePaths.Count;
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            IntPtr consoleWindow = GetConsoleWindow();

            if (projectCount == 3)
            {
                MoveWindow(consoleWindow, 0, (int)(screenHeight * 0.75), screenWidth, (int)(screenHeight * 0.25), true);

                for (int i = 0; i < projectCount; i++)
                {
                    ArrangeProjectWindow(filePaths[i], i * (int)(screenWidth * 0.25), 0, (int)(screenWidth * 0.25), (int)(screenHeight * 0.75));
                }
            }
            else if (projectCount == 2)
            {
                // FIXED: System takes bottom 50%, each project splits top half horizontally
                MoveWindow(consoleWindow, 0, (int)(screenHeight * 0.5), screenWidth, (int)(screenHeight * 0.5), true);

                for (int i = 0; i < projectCount; i++)
                {
                    ArrangeProjectWindow(filePaths[i], i * (int)(screenWidth * 0.5), 0, (int)(screenWidth * 0.5), (int)(screenHeight * 0.5));
                }
            }
            else if (projectCount == 1)
            {
                MoveWindow(consoleWindow, 0, 0, (int)(screenWidth * 0.5), screenHeight, true);

                ArrangeProjectWindow(filePaths[0], (int)(screenWidth * 0.5), 0, (int)(screenWidth * 0.5), screenHeight);
            }
        }

        private static void ArrangeProjectWindow(string filePath, int x, int y, int width, int height)
        {
            string fileName = Path.GetFileName(filePath);
            IntPtr hWnd = FindWindow(null, fileName);
            if (hWnd != IntPtr.Zero)
            {
                MoveWindow(hWnd, x, y, width, height, true);
                Console.WriteLine($"📐 חלון {fileName} מוקם בהצלחה.");
            }
            else
            {
                Console.WriteLine($"⚠️ לא נמצא חלון עבור {fileName}.");
            }
        }
    }
}
