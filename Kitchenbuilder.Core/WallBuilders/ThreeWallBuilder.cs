using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core.WallBuilders
{
    public static class ThreeWallBuilder
    {
        public static void Run(Kitchen kitchen)
        {
            var wall1 = kitchen.Walls[0];
            var wall2 = kitchen.Walls[1];
            var wall3 = kitchen.Walls[2];
            double floorLength = kitchen.Floor.Width;
            double floorWidth = kitchen.Floor.Length;
            double base1 = 20;
            double base2 = 20;
            double base3 = 20;

            SldWorks swApp = Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application")) as SldWorks;
            if (swApp == null)
            {
                Console.WriteLine("❌ Could not start SolidWorks.");
                return;
            }

            swApp.Visible = true;

            string path = @"C:\Users\chouse\Downloads\Kitchenbuilder\KitchenParts\Walls\Wall3.SLDPRT";
            ModelDoc2 swModel = swApp.OpenDoc(path, (int)swDocumentTypes_e.swDocPART) as ModelDoc2;
            if (swModel == null)
            {
                Console.WriteLine("❌ Failed to open SolidWorks file.");
                return;
            }

            double factor = 0.01;

            ((Dimension)swModel.Parameter("Length_Floor@Sketch2")).SystemValue = floorLength * factor;
            ((Dimension)swModel.Parameter("Width_Floor@Sketch2")).SystemValue = floorWidth * factor;

            ((Dimension)swModel.Parameter("D1@Wall1")).SystemValue = wall1.Height * factor;
            ((Dimension)swModel.Parameter("D1@Wall2")).SystemValue = wall2.Height * factor;
            ((Dimension)swModel.Parameter("D1@Wall3")).SystemValue = wall3.Height * factor;

            ((Dimension)swModel.Parameter("Wall1Base@Sketch4")).SystemValue = base1 * factor;
            ((Dimension)swModel.Parameter("Wall2Base@Sketch5")).SystemValue = base2 * factor;
            ((Dimension)swModel.Parameter("Wall3Base@Sketch6")).SystemValue = base3 * factor;

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

            if (wall1.HasWindows && wall1.Windows != null) SetWindows(wall1.Windows.ToArray(), 8, "");
            if (wall1.HasDoors && wall1.Doors != null) SetDoors(wall1.Doors.ToArray(), 11, "");

            if (wall2.HasWindows && wall2.Windows != null) SetWindows(wall2.Windows.ToArray(), 13, "");
            if (wall2.HasDoors && wall2.Doors != null) SetDoors(wall2.Doors.ToArray(), 16, "");

            if (wall3.HasWindows && wall3.Windows != null) SetWindows(wall3.Windows.ToArray(), 19, "");
            if (wall3.HasDoors && wall3.Doors != null) SetDoors(wall3.Doors.ToArray(), 22, "");

            void SuppressFeaturesAndSketches(string featurePrefix, string sketchPrefix, int actualCount, int maxCount, int sketchOffset)
            {
                for (int i = actualCount; i < maxCount; i++)
                {
                    string featureName = $"{featurePrefix}{i + 1}";
                    string sketchName = $"Sketch{sketchOffset + i}";

                    Feature feature = FindFeatureByName(swModel, featureName);
                    if (feature != null)
                        feature.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);

                    Feature sketch = FindFeatureByName(swModel, sketchName);
                    if (sketch != null)
                        sketch.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);
                }
            }

            SuppressFeaturesAndSketches("Window", "Sketch", wall1.Windows?.Count ?? 0, 3, 8);
            SuppressFeaturesAndSketches("Door", "Sketch", wall1.Doors?.Count ?? 0, 2, 11);

            SuppressFeaturesAndSketches("Window", "Sketch", wall2.Windows?.Count ?? 0, 3, 13);
            SuppressFeaturesAndSketches("Door", "Sketch", wall2.Doors?.Count ?? 0, 2, 16);

            SuppressFeaturesAndSketches("Window", "Sketch", wall3.Windows?.Count ?? 0, 3, 19);
            SuppressFeaturesAndSketches("Door", "Sketch", wall3.Doors?.Count ?? 0, 2, 22);

            swModel.ForceRebuild3(true);

            string folder =  @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\temp";
            Directory.CreateDirectory(folder);
            string outputPath = Path.Combine(folder, "3Walls_WithFloor.SLDPRT");

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
