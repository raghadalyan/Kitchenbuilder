using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class EditPlaneOffset
    {
        private static readonly string debugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\EditPlaneDebug.txt";

        private static void Log(string message)
        {
            File.AppendAllText(debugPath, $"{DateTime.Now:HH:mm:ss} - {message}{System.Environment.NewLine}");
        }

        public static void UpdatePlaneOffset(ISldWorks swApp, string planeName, double newOffset)
        {
            try
            {
                var model = (ModelDoc2)swApp.ActiveDoc;
                if (model == null)
                {
                    Log("❌ No document open.");
                    return;
                }

                var ext = model.Extension;

                // Select the plane by name
                bool selected = ext.SelectByID2(planeName, "PLANE", 0, 0, 0, false, 0, null, 0);
                if (!selected)
                {
                    Log($"❌ Could not select plane '{planeName}'.");
                    return;
                }

                var selMgr = (SelectionMgr)model.SelectionManager;
                var feat = selMgr.GetSelectedObject6(1, -1) as Feature;
                if (feat == null)
                {
                    Log("❌ Could not get feature from selection.");
                    return;
                }

                var def = feat.GetDefinition() as RefPlaneFeatureData;
                if (def == null)
                {
                    Log("❌ Could not get RefPlaneFeatureData.");
                    return;
                }

                // Set new distance
                def.Distance = newOffset;

                // Re-apply the definition
                bool modified = feat.ModifyDefinition(def, model, null);
                Log(modified
                    ? $"✅ Offset of plane '{planeName}' updated to {newOffset}m (90cm)."
                    : $"❌ Failed to update the plane.");

                model.ClearSelection2(true);
            }
            catch (Exception ex)
            {
                Log($"❌ Exception: {ex.Message}");
            }
        }
    }
}
