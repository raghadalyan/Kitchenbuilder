using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class CreatePlaneWithInsertRef
    {
        private static readonly string debugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\CreatePlaneDebug.txt";

        private static void Log(string message)
        {
            File.AppendAllText(debugPath, $"{DateTime.Now:HH:mm:ss} - {message}{System.Environment.NewLine}");
        }

        public static void CreateOffsetPlane(ISldWorks swApp)
        {
            try
            {
                ModelDoc2 swModel = (ModelDoc2)swApp.ActiveDoc;

                if (swModel == null)
                {
                    Log("❌ No document open.");
                    return;
                }

                // Select the Top Plane
                bool selected = swModel.Extension.SelectByID2("Top Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
                if (!selected)
                {
                    Log("❌ Could not select Top Plane.");
                    return;
                }

                IFeatureManager swFeatMgr = swModel.FeatureManager;

                RefPlane newPlane = (RefPlane)swFeatMgr.InsertRefPlane(
                    (int)swRefPlaneReferenceConstraints_e.swRefPlaneReferenceConstraint_Distance,
                    0.5,    // 60 cm offset
                    0,      // Flip direction
                    0, 0, 0
                );

                if (newPlane != null)
                {
                    Feature feat = (Feature)newPlane;
                    feat.Name = "Plane_Microwave";
                    Log("✅ Plane created successfully. Name set to 'Plane_Microwave'");
                }
                else
                {
                    Log("❌ Failed to create offset plane.");
                }

                swModel.ClearSelection2(true);
            }
            catch (Exception ex)
            {
                Log($"❌ Exception: {ex.Message}");
            }
        }
    }
}
