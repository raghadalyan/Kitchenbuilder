using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace Kitchenbuilder.Core
{
    public static class Move_Stations_Upper
    {
        private static readonly string LogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\upper\move_station_debug.txt";

        public static void MoveTo(ISldWorks app, IModelDoc2 model, string wallName)
        {
            try
            {
                string planeName = $"Plane_{wallName.Replace("Wall", "Wall_")}";
                Log($"➡ Attempting to move to: {planeName}");

                bool success = model.Extension.SelectByID2(
                    planeName, "PLANE", 0, 0, 0,
                    false, 0, null, 0);

                if (success)
                {
                    model.ShowNamedView2("*Normal To", (int)swStandardViews_e.swIsometricView);
                    Log($"✅ Successfully set Normal To: {planeName}");
                }
                else
                {
                    Log($"❌ Failed to select plane: {planeName}");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Exception in MoveTo: {ex.Message}");
            }
        }

        private static void Log(string message)
        {
            try
            {
                string folder = Path.GetDirectoryName(LogPath)!;
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
                File.AppendAllText(LogPath, line + System.Environment.NewLine);
            }
            catch (Exception logEx)
            {
                Console.WriteLine($"❌ Logging failed: {logEx.Message}");
            }
        }
    }
}
