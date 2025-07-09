using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;  // ✅ Add this line
using System;
using System.IO;


namespace Kitchenbuilder.Core
{
    public static class Hide_Bodies_In_Sld_IModel
    {
        private static readonly string LogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\island\HideBodiesLog.txt";

        public static void HideMultipleBodies(IModelDoc2 model, string[] bodyNames)
        {
            try
            {
                if (model is not PartDoc part)
                {
                    Log("❌ Not a part document.");
                    return;
                }

                object[] bodies = (object[])part.GetBodies2((int)swBodyType_e.swAllBodies, true);

                foreach (object bodyObj in bodies)
                {
                    if (bodyObj is IBody2 body)
                    {
                        string name = body.Name;
                        if (Array.Exists(bodyNames, n => n == name))
                        {
                            body.HideBody(true); // hide body
                            Log($"🙈 Hid body: {name}");
                        }
                    }
                }

                model.EditRebuild3();
                Log("🔄 Rebuilt model after hiding.");
            }
            catch (Exception ex)
            {
                Log($"❌ Exception: {ex.Message}");
            }
        }

        private static void Log(string msg)
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            }
            catch { }
        }
    }
}
