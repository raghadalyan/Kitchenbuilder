using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class SelectBody
    {
        private static readonly string LogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Debug\SelectBody_Debug.txt";

        public static void SelectByName(IModelDoc2 model, string bodyName)
        {
            Log($"🔍 Trying to select body: {bodyName}");

            if (model == null)
            {
                Log("❌ Model is null.");
                return;
            }

            // Exit sketch if open
            if (model.SketchManager?.ActiveSketch != null)
            {
                model.SketchManager.InsertSketch(true);
                Log("📤 Exited active sketch.");
            }

            // Cast to PartDoc to access GetBodies2
            if (model is not PartDoc part)
            {
                Log("❌ Model is not a PartDoc.");
                return;
            }

            object[] bodies = (object[])part.GetBodies2((int)swBodyType_e.swAllBodies, false);
            foreach (object b in bodies)
            {
                if (b is Body2 body && body.Name.Equals(bodyName, StringComparison.OrdinalIgnoreCase))
                {
                    model.ClearSelection2(true);
                    bool success = body.Select2(true, null); // Use null instead of -1
                    Log(success ? $"✅ Body selected: {bodyName}" : $"⚠️ Failed to select body: {bodyName}");
                    return;
                }
            }

            Log($"❌ Body not found: {bodyName}");
        }

        private static void Log(string message)
        {
            string timeStamped = $"[{DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}";
            string folder = Path.GetDirectoryName(LogPath)!;
            Directory.CreateDirectory(folder);
            File.AppendAllText(LogPath, timeStamped);
        }
    }
}
