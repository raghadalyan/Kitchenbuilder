using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace Kitchenbuilder.Core.Materials
{
    public static class EditMaterial
    {
        public static bool ApplyMaterialToBody(ISldWorks swApp, string bodyName, string materialName, string type)
        {
            string debugPath = Path.Combine(
                KitchenConfig.Get().BasePath,
                "Kitchenbuilder", "Output", "materialdeb.txt"
            );

            void Log(string message)
            {
                Console.WriteLine(message);
                File.AppendAllText(debugPath, $"{DateTime.Now:HH:mm:ss} - {message}\n");
            }

            try
            {
                Log("🔧 Starting material assignment...");

                if (swApp == null)
                {
                    Log("❌ SolidWorks application is null.");
                    return false;
                }

                // Get the active document
                ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;
                if (swModel == null)
                {
                    Log("❌ No active document in SolidWorks.");
                    return false;
                }

                Log("📄 Active document found.");

                // Build material path
                string materialPath = swApp.GetExecutablePath() + @"\lang\english\solidworks materials\solidworks materials.sldmat";
                Log($"📁 Material database path: {materialPath}");
                Log($"🎯 Attempting to select body: {bodyName}");

                // Select the body
                ModelDocExtension swExt = swModel.Extension;
                bool selected = swExt.SelectByID2(bodyName, "SOLIDBODY", 0, 0, 0, false, 0, null, 0);

                if (!selected)
                {
                    Log($"❌ Failed to select the body '{bodyName}'.");
                    return false;
                }

                Log("✅ Body selected successfully.");

                // Apply the material
                PartDoc swPart = (PartDoc)swModel;
                swPart.SetMaterialPropertyName2("", materialPath, materialName);
                Log($"🎨 Material '{materialName}' of type '{type}' applied.");

                // Rebuild and save
                swModel.ForceRebuild3(false);
                swModel.GraphicsRedraw2();
                swModel.Save();
                Log("💾 Document rebuilt and saved successfully.");

                return true;
            }
            catch (Exception ex)
            {
                Log($"❌ Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
    }
}