using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core.WallBuilders
{
    public static class TwoWallBuilder
    {
        public static void Run(Kitchen kitchen)
        {
            string basePath = KitchenConfig.Get().BasePath;
            string outputPath = Path.Combine(basePath, "Kitchenbuilder", "Output", "TwoWallBuilder_Log.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using var writer = new StreamWriter(outputPath, append: true);
            void Log(string message)
            {
                writer.WriteLine($"{DateTime.Now:HH:mm:ss} - {message}");
                Console.WriteLine(message);
            }

            var wall1 = kitchen.Walls[0];
            var wall2 = kitchen.Walls[1];
            double floorWidth = kitchen.Floor.Length;
            double floorLength = kitchen.Floor.Width;

            var progId = Type.GetTypeFromProgID("SldWorks.Application");
            if (progId == null)
            {
                Log("❌ SolidWorks not found via ProgID.");
                return;
            }

            var instance = Activator.CreateInstance(progId);
            if (instance is not SldWorks swApp)
            {
                Log("❌ Failed to launch SolidWorks.");
                return;
            }

            swApp.Visible = true;

            string sourcePath = Path.Combine(basePath, "Kitchenbuilder", "KitchenParts", "Walls", "Wall2.SLDPRT");
            string destFolder = Path.Combine(basePath, "Kitchenbuilder", "Output", "Kitchen");
            Directory.CreateDirectory(destFolder);
            string destPath = Path.Combine(destFolder, "Wall2_WithFloor.SLDPRT");

            if (!File.Exists(sourcePath))
            {
                Log($"❌ Source file not found: {sourcePath}");
                return;
            }

            Log($"📁 Copying file from: {sourcePath} to {destPath}");
            File.Copy(sourcePath, destPath, true);

            ModelDoc2 swModel = swApp.OpenDoc(destPath, (int)swDocumentTypes_e.swDocPART) as ModelDoc2;
            if (swModel == null)
            {
                Log("❌ Failed to open file in SolidWorks.");
                return;
            }

            double factor = 0.01; // mm to m

            void SetDim(string name, double value)
            {
                try
                {
                    var dim = (Dimension)swModel.Parameter(name);
                    if (dim != null)
                    {
                        dim.SystemValue = value;
                        Log($"✅ Updated {name} to {value * 100} mm");
                    }
                    else
                    {
                        Log($"⚠️ Parameter not found: {name}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"❌ Error updating {name}: {ex.Message}");
                }
            }

            Log("🔧 Updating dimensions...");
            SetDim("Width_Floor@Sketch2", floorLength * factor);
            SetDim("Length_Floor@Sketch2", floorWidth * factor);
            SetDim("D1@Wall1", wall1.Height * factor);
            SetDim("D1@Wall2", wall2.Height * factor);

            // Wall 1 windows
            if (wall1.HasWindows && wall1.Windows != null)
            {
                string[] sketchNames = { "Win1", "Win2", "Win3" };
                for (int i = 0; i < wall1.Windows.Count && i < 3; i++)
                {
                    var win = wall1.Windows[i];
                    int index = i + 1;
                    string sketch = sketchNames[i];

                    SetDim($"Window{index}Display@{sketch}", win.Width * factor);
                    SetDim($"Window{index}Length@{sketch}", win.Height * factor);
                    SetDim($"Window{index}BottomOffset@{sketch}", win.DistanceY * factor);
                    SetDim($"Window{index}RightOffset@{sketch}", win.DistanceX * factor);
                }
            }

            // Wall 1 doors
            if (wall1.HasDoors && wall1.Doors != null)
            {
                string[] sketchNames = { "D1", "D2" };
                for (int i = 0; i < wall1.Doors.Count && i < 2; i++)
                {
                    var door = wall1.Doors[i];
                    int index = i + 1;
                    string sketch = sketchNames[i];

                    SetDim($"Door{index}Display@{sketch}", door.Width * factor);
                    SetDim($"Door{index}Length@{sketch}", door.Height * factor);
                    SetDim($"Door{index}BottomOffset@{sketch}", door.DistanceY * factor);
                    SetDim($"Door{index}RightOffset@{sketch}", door.DistanceX * factor);
                }
            }

            // Wall 2 windows
            if (wall2.HasWindows && wall2.Windows != null)
            {
                string[] sketchNames = { "Win12", "Win22", "Win32" };
                for (int i = 0; i < wall2.Windows.Count && i < 3; i++)
                {
                    var win = wall2.Windows[i];
                    int index = i + 1;
                    string sketch = sketchNames[i];

                    SetDim($"Window{index}Display@{sketch}", win.Width * factor);
                    SetDim($"Window{index}Length@{sketch}", win.Height * factor);
                    SetDim($"Window{index}BottomOffset@{sketch}", win.DistanceY * factor);
                    SetDim($"Window{index}RightOffset@{sketch}", win.DistanceX * factor);
                }
            }

            // Wall 2 doors
            if (wall2.HasDoors && wall2.Doors != null)
            {
                string[] sketchNames = { "D12", "D22" };
                for (int i = 0; i < wall2.Doors.Count && i < 2; i++)
                {
                    var door = wall2.Doors[i];
                    int index = i + 1;
                    string sketch = sketchNames[i];

                    SetDim($"Door{index}Display@{sketch}", door.Width * factor);
                    SetDim($"Door{index}Length@{sketch}", door.Height * factor);
                    SetDim($"Door{index}BottomOffset@{sketch}", door.DistanceY * factor);
                    SetDim($"Door{index}RightOffset@{sketch}", door.DistanceX * factor);
                }
            }

            // Suppress unused window/door features
            SuppressWallFeatures("WindowSlot", "Win", wall1.Windows?.Count ?? 0, 3, swModel);
            SuppressWallFeatures("Door", "D", wall1.Doors?.Count ?? 0, 2, swModel);
            SuppressWallFeatures("WindowSlot", "Win", wall2.Windows?.Count ?? 0, 3, swModel, wall2Suffix: "2");
            SuppressWallFeatures("Door", "D", wall2.Doors?.Count ?? 0, 2, swModel, wall2Suffix: "2");

            swModel.EditRebuild3();
            swModel.Save();
            swApp.CloseDoc(destPath);

            Log($"✅ Wall file saved: {destPath}");
        }

        private static void SuppressWallFeatures(string featurePrefix, string sketchPrefix, int usedCount, int max, ModelDoc2 swModel, string wall2Suffix = "")
        {
            for (int i = usedCount; i < max; i++)
            {
                string featureName = $"{featurePrefix}{i + 1}{wall2Suffix}";
                string sketchName = $"{sketchPrefix}{i + 1}{wall2Suffix}";

                Feature f = FindFeatureByName(swModel, featureName);
                if (f != null) f.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);

                Feature s = FindFeatureByName(swModel, sketchName);
                if (s != null) s.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);
            }
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
