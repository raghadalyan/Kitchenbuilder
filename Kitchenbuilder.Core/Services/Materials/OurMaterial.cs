using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace Kitchenbuilder.Core.Materials
{
    public static class OurMaterial
    {
        public static bool ApplyCustomMaterial(ISldWorks swApp, string bodyName, string materialName)
        {
            string debugPath = Path.Combine(
                KitchenConfig.Get().BasePath,
                "Kitchenbuilder", "Output", "ourmaterial_debug.txt"
            );

            void Log(string message)
            {
                Console.WriteLine(message);
                File.AppendAllText(debugPath, $"{DateTime.Now:HH:mm:ss} - {message}\n");
            }

            try
            {
                Log("🔧 Starting custom material assignment...");

                if (swApp == null)
                {
                    Log("❌ SolidWorks application is null.");
                    return false;
                }

                ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;
                if (swModel == null)
                {
                    Log("❌ No active document in SolidWorks.");
                    return false;
                }

                Log("📄 Active document found.");

                string materialPath = Path.Combine(
                    KitchenConfig.Get().BasePath,
                    "Kitchenbuilder", "marbletiles", "materials-aya.sldmat"
                );
                Log($"📁 Custom material database path: {materialPath}");
                Log($"🎯 Attempting to select body: {bodyName}");

                ModelDocExtension swExt = swModel.Extension;
                bool selected = swExt.SelectByID2(bodyName, "SOLIDBODY", 0, 0, 0, false, 0, null, 0);

                if (!selected)
                {
                    Log($"❌ Failed to select the body '{bodyName}'.");
                    return false;
                }

                Log("✅ Body selected successfully.");

                PartDoc swPart = (PartDoc)swModel;
                swPart.SetMaterialPropertyName2("", materialPath, materialName);
                Log($"🎨 Custom material '{materialName}' applied from materials-aya.");

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
