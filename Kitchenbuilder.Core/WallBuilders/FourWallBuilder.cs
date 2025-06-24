using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core.WallBuilders
{
    public static class FourWallBuilder
    {
        public static void Run(Kitchen kitchen)
        {
            var wall1 = kitchen.Walls[0];
            var wall2 = kitchen.Walls[1];
            var wall3 = kitchen.Walls[2];
            var wall4 = kitchen.Walls[3];
            double floorLength = kitchen.Floor.Width;
            double floorWidth = kitchen.Floor.Length;
            double base1 = 20, base2 = 20, base3 = 20, base4 = 20;

            SldWorks swApp = Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application")) as SldWorks;
            if (swApp == null)
            {
                Console.WriteLine("❌ Could not start SolidWorks.");
                return;
            }

            swApp.Visible = true;

            string path = @"C:\Users\chouse\Downloads\Kitchenbuilder\KitchenParts\Walls\Wall4.SLDPRT";
            ModelDoc2 swModel = swApp.OpenDoc(path, (int)swDocumentTypes_e.swDocPART) as ModelDoc2;
            if (swModel == null)
            {
                Console.WriteLine("❌ Failed to open SolidWorks file.");
                return;
            }

            double factor = 0.01;

            ((Dimension)swModel.Parameter("Length_Floor@Sketch1")).SystemValue = floorLength * factor;
            ((Dimension)swModel.Parameter("Width_Floor@Sketch1")).SystemValue = floorWidth * factor;

            ((Dimension)swModel.Parameter("D1@Wall1")).SystemValue = wall1.Height * factor;
            ((Dimension)swModel.Parameter("D1@Wall2")).SystemValue = wall2.Height * factor;
            ((Dimension)swModel.Parameter("D1@Wall3")).SystemValue = wall3.Height * factor;
            ((Dimension)swModel.Parameter("D1@Wall4")).SystemValue = wall4.Height * factor;

            ((Dimension)swModel.Parameter("Wall1Base@Sketch2")).SystemValue = base1 * factor;
            ((Dimension)swModel.Parameter("Wall2Base@Sketch3")).SystemValue = base2 * factor;
            ((Dimension)swModel.Parameter("Wall3Base@Sketch4")).SystemValue = base3 * factor;
            ((Dimension)swModel.Parameter("Wall4Base@Sketch5")).SystemValue = base4 * factor;

            void SetWindows(Window[] windows, int sketchStart, string prefix)
            {
                for (int i = 0; i < windows.Length && i < 3; i++)
                {
                    var w = windows[i];
                    int sketch = sketchStart + i;
                    ((Dimension)swModel.Parameter($"{prefix}Window{i + 1}Display@Sketch{sketch}")).SystemValue = w.Width * factor;
                    ((Dimension)swModel.Parameter($"{prefix}Window{i + 1}Length@Sketch{sketch}")).SystemValue = w.Height * factor;
                    ((Dimension)swModel.Parameter($"{prefix}Window{i + 1}BottomOffset@Sketch{sketch}")).SystemValue = w.DistanceY * factor;
                    ((Dimension)swModel.Parameter($"{prefix}Window{i + 1}RightOffset@Sketch{sketch}")).SystemValue = w.DistanceX * factor;
                }
            }

            void SetDoors(Door[] doors, int sketchStart, string prefix)
            {
                for (int i = 0; i < doors.Length && i < 2; i++)
                {
                    var d = doors[i];
                    int sketch = sketchStart + i;
                    ((Dimension)swModel.Parameter($"{prefix}Door{i + 1}Display@Sketch{sketch}")).SystemValue = d.Width * factor;
                    ((Dimension)swModel.Parameter($"{prefix}Door{i + 1}length@Sketch{sketch}")).SystemValue = d.Height * factor;
                    ((Dimension)swModel.Parameter($"{prefix}Door{i + 1}BottomOffset@Sketch{sketch}")).SystemValue = d.DistanceY * factor;
                    ((Dimension)swModel.Parameter($"{prefix}Door{i + 1}RightOffset@Sketch{sketch}")).SystemValue = d.DistanceX * factor;
                }
            }

            if (wall1.HasWindows && wall1.Windows != null) SetWindows(wall1.Windows.ToArray(), 6, "");
            if (wall1.HasDoors && wall1.Doors != null) SetDoors(wall1.Doors.ToArray(), 9, "");

            if (wall2.HasWindows && wall2.Windows != null) SetWindows(wall2.Windows.ToArray(), 12, "");
            if (wall2.HasDoors && wall2.Doors != null) SetDoors(wall2.Doors.ToArray(), 15, "");

            if (wall3.HasWindows && wall3.Windows != null) SetWindows(wall3.Windows.ToArray(), 17, "");
            if (wall3.HasDoors && wall3.Doors != null) SetDoors(wall3.Doors.ToArray(), 20, "");

            if (wall4.HasWindows && wall4.Windows != null) SetWindows(wall4.Windows.ToArray(), 22, "");
            if (wall4.HasDoors && wall4.Doors != null) SetDoors(wall4.Doors.ToArray(), 25, "");

            void SuppressUnused(string prefix, int actualWindows, int actualDoors)
            {
                for (int i = actualWindows; i < 3; i++)
                {
                    string featureName = $"{prefix}Window{i + 1}";
                    Feature feature = FindFeatureByName(swModel, featureName);
                    if (feature != null)
                        feature.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);
                }

                for (int i = actualDoors; i < 2; i++)
                {
                    string featureName = $"{prefix}Door{i + 1}";
                    Feature feature = FindFeatureByName(swModel, featureName);
                    if (feature != null)
                        feature.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);
                }
            }

            SuppressUnused("", wall1.Windows?.Count ?? 0, wall1.Doors?.Count ?? 0);
            SuppressUnused("", wall2.Windows?.Count ?? 0, wall2.Doors?.Count ?? 0);
            SuppressUnused("", wall3.Windows?.Count ?? 0, wall3.Doors?.Count ?? 0);
            SuppressUnused("", wall4.Windows?.Count ?? 0, wall4.Doors?.Count ?? 0);

            swModel.ForceRebuild3(true);

            string folder = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\temp";
            Directory.CreateDirectory(folder);
            string outputPath = Path.Combine(folder, "4Walls_WithFloor.SLDPRT");

            swModel.SaveAs3(outputPath,
                (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                (int)swSaveAsOptions_e.swSaveAsOptions_Copy);

            Console.WriteLine($"✅ File saved to: {outputPath}");
        }

        private static Feature FindFeatureByName(ModelDoc2 model, string featureName)
        {
            Feature feature = model.FirstFeature() as Feature;
            while (feature != null)
            {
                if (feature.Name == featureName)
                    return feature;
                feature = feature.GetNextFeature() as Feature;
            }
            return null;
        }
    }
}
