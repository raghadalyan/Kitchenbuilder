using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core.WallBuilders
{
    public static class OneWallBuilder
    {
        public static void Run(Kitchen kitchen)
        {
            var wall = kitchen.Walls[0];
            double floorLength = kitchen.Floor.Width;
            double floorWidth = kitchen.Floor.Length;
            double height = wall.Height;
            double baseLength = 20;

            // ⬇️ إعداد SolidWorks
            SldWorks swApp = Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application")) as SldWorks;
            if (swApp == null)
            {
                Console.WriteLine("❌ Could not start SolidWorks.");
                return;
            }

            swApp.Visible = true;

            string templatePath = @"C:\Users\chouse\Downloads\Kitchenbuilder\KitchenParts\Walls\Wall1.SLDPRT";
            ModelDoc2 swModel = swApp.OpenDoc(templatePath, (int)swDocumentTypes_e.swDocPART) as ModelDoc2;
            if (swModel == null)
            {
                Console.WriteLine("❌ Failed to open SolidWorks file.");
                return;
            }

            double factor = 0.01;

            // ⬇️ تعديل أبعاد الحائط والأرضية
            ((Dimension)swModel.Parameter("Length_Floor@Sketch2")).SystemValue = floorLength * factor;
            ((Dimension)swModel.Parameter("Width_Floor@Sketch2")).SystemValue = floorWidth * factor;
            ((Dimension)swModel.Parameter("D1@Wall1")).SystemValue = height * factor;
            ((Dimension)swModel.Parameter("wallBase@Sketch4")).SystemValue = baseLength * factor;

            // ⬇️ النوافذ
            if (wall.HasWindows && wall.Windows != null)
            {
                for (int i = 0; i < wall.Windows.Count && i < 3; i++)
                {
                    var win = wall.Windows[i];
                    int sketch = (i == 0) ? 5 : (i == 1) ? 7 : 8;
                    string index = (i + 1).ToString();

                    ((Dimension)swModel.Parameter($"Window{index}Display@Sketch{sketch}")).SystemValue = win.Width * factor;
                    ((Dimension)swModel.Parameter($"Window{index}Length@Sketch{sketch}")).SystemValue = win.Height * factor;
                    ((Dimension)swModel.Parameter($"Window{index}BottomOffset@Sketch{sketch}")).SystemValue = win.DistanceY * factor;
                    ((Dimension)swModel.Parameter($"Window{index}RightOffset@Sketch{sketch}")).SystemValue = win.DistanceX * factor;
                }
            }

            // ⬇️ الأبواب
            if (wall.HasDoors && wall.Doors != null)
            {
                for (int i = 0; i < wall.Doors.Count && i < 2; i++)
                {
                    var door = wall.Doors[i];
                    int sketch = 9 + i;
                    string index = (i + 1).ToString();

                    ((Dimension)swModel.Parameter($"Door{index}Display@Sketch{sketch}")).SystemValue = door.Width * factor;

                    string lengthParam = (i == 0) ? $"Door{index}length@Sketch{sketch}" : $"D1@Sketch{sketch}";
                    ((Dimension)swModel.Parameter(lengthParam)).SystemValue = door.Height * factor;

                    ((Dimension)swModel.Parameter($"Door{index}BottomOffset@Sketch{sketch}")).SystemValue = door.DistanceY * factor;
                    ((Dimension)swModel.Parameter($"Door{index}RightOffset@Sketch{sketch}")).SystemValue = door.DistanceX * factor;
                }
            }

            // ⬇️ تعطيل النوافذ غير المستخدمة (باستخدام WindowSlot)
            int usedWindows = wall.Windows?.Count ?? 0;
            for (int i = usedWindows; i < 3; i++)
            {
                string featureName = $"WindowSlot{i + 1}";
                string sketchName = (i == 0) ? "Sketch5" : (i == 1) ? "Sketch7" : "Sketch8";

                Feature feature = FindFeatureByName(swModel, featureName);
                if (feature != null)
                    feature.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);

                Feature sketch = FindFeatureByName(swModel, sketchName);
                if (sketch != null)
                    sketch.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);
            }

            // ⬇️ تعطيل الأبواب غير المستخدمة (تبقى كما هي)
            int usedDoors = wall.Doors?.Count ?? 0;
            for (int i = usedDoors; i < 2; i++)
            {
                string featureName = $"Door{i + 1}";
                string sketchName = (i == 0) ? "Sketch9" : "Sketch10";

                Feature feature = FindFeatureByName(swModel, featureName);
                if (feature != null)
                    feature.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);

                Feature sketch = FindFeatureByName(swModel, sketchName);
                if (sketch != null)
                    sketch.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, 2, null);
            }

            swModel.ForceRebuild3(true);

            // ⬇️ إعداد المجلد واسم الملف
            string folder = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\temp";
            Directory.CreateDirectory(folder);
            string outputPath = Path.Combine(folder, "Wall1_WithFloor.SLDPRT");

            // ⬇️ الحفظ
            swModel.SaveAs3(outputPath,
                (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                (int)swSaveAsOptions_e.swSaveAsOptions_Copy);

            Console.WriteLine($"✅ File saved to: {outputPath}");
        }

        // ✅ دالة البحث عن ميزة بالاسم
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