using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class EditPlaneOffset
    {
        private static readonly string DebugPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Output", "EditPlaneOffsetDebug.txt"
        );

        private static void Log(string msg)
        {
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {msg}{System.Environment.NewLine}");
        }

        public static void SetOffset(IModelDoc2 model, string planeName, double offsetCm)
        {
            try
            {
                Log($"📐 Editing offset of plane '{planeName}' to {offsetCm} cm");

                PartDoc part = model as PartDoc;
                if (part == null)
                {
                    Log("❌ Model is not a PartDoc.");
                    return;
                }

                Feature feat = (Feature)part.FeatureByName(planeName);
                if (feat == null)
                {
                    Log($"❌ Plane '{planeName}' not found.");
                    return;
                }

                object def = feat.GetDefinition();
                if (def is not IRefPlaneFeatureData refPlaneData)
                {
                    Log($"❌ '{planeName}' is not a reference plane.");
                    return;
                }

                // Convert cm to meters
                double offsetMeters = offsetCm / 100.0;

                // Access selections (required)
                refPlaneData.AccessSelections(model, null);

                // Set offset
                refPlaneData.Distance = offsetMeters;

                // Save back
                bool success = feat.ModifyDefinition(refPlaneData, model, null);
                model.EditRebuild3();

                if (success)
                    Log($"✅ Offset updated to {offsetCm} cm (={offsetMeters} m)");
                else
                    Log("❌ ModifyDefinition failed.");
            }
            catch (Exception ex)
            {
                Log($"❌ Exception: {ex.Message}");
            }
        }
    }
}
