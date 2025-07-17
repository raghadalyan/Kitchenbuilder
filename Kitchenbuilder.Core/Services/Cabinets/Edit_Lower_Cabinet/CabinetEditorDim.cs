using SolidWorks.Interop.sldworks;
using System;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core
{
    public static class CabinetEditorDim
    {
        public static void ApplyDimensions(IModelDoc2 model, CabinetInfo cabinet)
        {
            if (model == null || cabinet == null)
            {
                Log("❌ Model or cabinet is null.");
                return;
            }

            string sketchName = cabinet.SketchName;

            string widthDim = $"Length@{sketchName}";
            string heightDim = $"Width@{sketchName}";

            Log($"🔧 Editing cabinet: {sketchName}");
            Log($"➡️ Setting {widthDim} = {cabinet.Width} cm");
            Log($"➡️ Setting {heightDim} = {cabinet.Height} cm");

            EditSketchDim_IModel.SetDimension(model, widthDim, cabinet.Width);
            EditSketchDim_IModel.SetDimension(model, heightDim, cabinet.Height);

            // ✅ Apply drawers if available
            if (cabinet.Drawers != null)
            {
                string drawerSketch = cabinet.Drawers.SketchName?.Trim();

                if (string.IsNullOrWhiteSpace(drawerSketch))
                {
                    Log($"❌ Drawers sketch name is missing for cabinet {sketchName}.");
                    return;
                }

                Log($"📦 Editing drawers: {drawerSketch}");


                double fallbackY = 0.1;

                for (int i = 1; i <= 5; i++)
                {
                    string widthProp = $"Width{i}";
                    string distYProp = $"DistanceY{i}";

                    double width = (double)(typeof(Drawers).GetProperty(widthProp)?.GetValue(cabinet.Drawers) ?? 0.0);
                    double distanceY = (double)(typeof(Drawers).GetProperty(distYProp)?.GetValue(cabinet.Drawers) ?? 0.0);

                    if (width <= 0.0)
                        width = 0.001;

                    if (distanceY <= 0.0)
                    {
                        distanceY = fallbackY;
                        fallbackY += 0.2;
                    }

                    string widthDimName = $"{widthProp}@{drawerSketch}";
                    string distYDimName = $"{distYProp}@{drawerSketch}";

                    Log($"➡️ Setting {widthDimName} = {width} cm");
                    EditSketchDim_IModel.SetDimension(model, widthDimName, width);

                    Log($"➡️ Setting {distYDimName} = {distanceY} cm");
                    EditSketchDim_IModel.SetDimension(model, distYDimName, distanceY);
                }



            }
        }

        private static void Log(string message)
        {
            string logPath = Path.Combine(KitchenConfig.Get().BasePath, "Kitchenbuilder", "Output", "CabinetEditorDim.txt");
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }
    }
}
