using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class Hide_Bodies_In_Sld
    {
        private static readonly string LogPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Output", "HideBodiesLog.txt"
        );

        public static void HideBodyByName(ModelDoc2 model, string bodyName)
        {
            try
            {
                Log($"🔍 Trying to hide body: {bodyName}");

                if (model == null)
                {
                    Log("❌ Model is null.");
                    return;
                }

                var part = model as PartDoc;
                if (part == null)
                {
                    Log("❌ Model is not a PartDoc.");
                    return;
                }

                object[] bodies = (object[])part.GetBodies2((int)swBodyType_e.swAllBodies, false);
                if (bodies == null)
                {
                    Log("❌ No bodies found in the part.");
                    return;
                }

                foreach (var bodyObj in bodies)
                {
                    var body = bodyObj as Body2;
                    if (body != null && body.Name == bodyName)
                    {
                        body.HideBody(true);
                        model.EditRebuild3(); // Refresh the model
                        Log($"✅ Body '{bodyName}' successfully hidden.");
                        return;
                    }
                }

                Log($"❌ Body '{bodyName}' not found.");
            }
            catch (Exception ex)
            {
                Log($"❌ Exception: {ex.Message}");
            }
        }

        private static void Log(string message)
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
    }
}
