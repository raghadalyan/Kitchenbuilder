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
            string outputPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\aya.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

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
            double base1 = 20;
            double base2 = 20;

            var progId = Type.GetTypeFromProgID("SldWorks.Application");
            if (progId == null)
            {
                Log("❌ لم يتم العثور على SolidWorks عبر ProgID.");
                return;
            }

            var instance = Activator.CreateInstance(progId);
            if (instance is not SldWorks swApp)
            {
                Log("❌ فشل في تشغيل SolidWorks.");
                return;
            }

            swApp.Visible = true;

            string sourcePath = @"C:\Users\chouse\Downloads\Kitchenbuilder\KitchenParts\Walls\Wall2.SLDPRT";
            string destFolder = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Kitchen";
            Directory.CreateDirectory(destFolder);
            string destPath = Path.Combine(destFolder, "Wall2_WithFloor.SLDPRT");

            if (!File.Exists(sourcePath))
            {
                Log($"❌ الملف المصدر غير موجود: {sourcePath}");
                return;
            }

            Log($"📁 نسخ الملف من: {sourcePath} إلى {destPath}");
            File.Copy(sourcePath, destPath, true);

            ModelDoc2 swModel = swApp.OpenDoc(destPath, (int)swDocumentTypes_e.swDocPART) as ModelDoc2;
            if (swModel == null)
            {
                Log("❌ فشل في فتح الملف في SolidWorks.");
                return;
            }

            double factor = 0.01;

            void SetDim(string name, double value)
            {
                try
                {
                    var dim = (Dimension)swModel.Parameter(name);
                    if (dim != null)
                    {
                        dim.SystemValue = value;
                        Log($"✅ تم تعديل {name} إلى {value * 100} mm");
                    }
                    else
                    {
                        Log($"⚠️ الباراميتر غير موجود: {name}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"❌ خطأ أثناء تعديل {name}: {ex.Message}");
                }
            }

            Log("🔧 بدأ تعديل الأبعاد...");
            
            SetDim("Width_Floor@Sketch2", floorLength * factor);
            SetDim("Length_Floor@Sketch2", floorWidth * factor);
            SetDim("D1@Wall1", wall1.Height * factor);
            SetDim("D1@Wall2", wall2.Height * factor);


            // ===== WALL 1 WINDOWS =====
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

            // ===== WALL 1 DOORS =====
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

            // ===== WALL 2 WINDOWS =====
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

            // ===== WALL 2 DOORS =====
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

            void SuppressWindowFeaturesWall1(int usedCount)
            {
                string[] features = { "WindowSlot1", "WindowSlot2", "WindowSlot3" };
                string[] sketches = { "Win1", "Win2", "Win3" };

                for (int i = usedCount; i < 3; i++)
                {
                    Feature f = FindFeatureByName(swModel, features[i]);
                    if (f != null) f.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);

                    Feature s = FindFeatureByName(swModel, sketches[i]);
                    if (s != null) s.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);
                }
            }

            void SuppressDoorFeaturesWall1(int usedCount)
            {
                string[] features = { "Door1", "Door2" };
                string[] sketches = { "D1", "D2" };

                for (int i = usedCount; i < 2; i++)
                {
                    Feature f = FindFeatureByName(swModel, features[i]);
                    if (f != null) f.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);

                    Feature s = FindFeatureByName(swModel, sketches[i]);
                    if (s != null) s.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);
                }
            }

            void SuppressWindowFeaturesWall2(int usedCount)
            {
                string[] features = { "WindowSlot12", "WindowSlot22", "WindowSlot32" };
                string[] sketches = { "Win12", "Win22", "Win32" };

                for (int i = usedCount; i < 3; i++)
                {
                    Feature f = FindFeatureByName(swModel, features[i]);
                    if (f != null) f.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);

                    Feature s = FindFeatureByName(swModel, sketches[i]);
                    if (s != null) s.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);
                }
            }

            void SuppressDoorFeaturesWall2(int usedCount)
            {
                string[] features = { "Door12", "Door22" };
                string[] sketches = { "D12", "D22" };

                for (int i = usedCount; i < 2; i++)
                {
                    Feature f = FindFeatureByName(swModel, features[i]);
                    if (f != null) f.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);

                    Feature s = FindFeatureByName(swModel, sketches[i]);
                    if (s != null) s.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);
                }
            }


            SuppressWindowFeaturesWall1(wall1.Windows?.Count ?? 0);
            SuppressDoorFeaturesWall1(wall1.Doors?.Count ?? 0);
            SuppressWindowFeaturesWall2(wall2.Windows?.Count ?? 0);
            SuppressDoorFeaturesWall2(wall2.Doors?.Count ?? 0);


            Log("💾 حفظ النموذج وإغلاقه...");
            swModel.EditRebuild3();
            swModel.Save();
            swApp.CloseDoc(destPath);

            Log($"✅ تم حفظ ملف الجدران بنجاح: {destPath}");
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
