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
            string outputPath = @"C:\Users\chouse\Desktop\kitchen\aya.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using var writer = new StreamWriter(outputPath, append: true);
            void Log(string message)
            {
                writer.WriteLine($"{DateTime.Now:HH:mm:ss} - {message}");
                Console.WriteLine(message);
            }

            var wall1 = kitchen.Walls[0];
            var wall2 = kitchen.Walls[1];
            double floorLength = kitchen.Floor.Length;
            double floorWidth = kitchen.Floor.Width;
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
            string destFolder = @"C:\Users\chouse\Desktop\kitchen";
            Directory.CreateDirectory(destFolder);
            string destPath = Path.Combine(destFolder, "2Walls_WithFloor.SLDPRT");

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

            SetDim("Length_Floor@Sketch2", floorLength * factor);
            SetDim("Width_Floor@Sketch2", floorWidth * factor);
            SetDim("D1@Wall1", wall1.Height * factor);
            SetDim("D1@Wall2", wall2.Height * factor);
            SetDim("wallBase@Sketch3", base1 * factor);
            SetDim("wall2Base@Sketch4", base2 * factor);

            if (wall1.HasWindows && wall1.Windows != null)
            {
                for (int i = 0; i < wall1.Windows.Count && i < 3; i++)
                {
                    var win = wall1.Windows[i];
                    int sketch = 5 + i;
                    string index = (i + 1).ToString();

                    SetDim($"Window{index}Display@Sketch{sketch}", win.Width * factor);
                    SetDim($"Window{index}Length@Sketch{sketch}", win.Height * factor);
                    SetDim($"Window{index}BottomOffset@Sketch{sketch}", win.DistanceY * factor);
                    SetDim($"Window{index}RightOffset@Sketch{sketch}", win.DistanceX * factor);
                }
            }

            if (wall1.HasDoors && wall1.Doors != null)
            {
                for (int i = 0; i < wall1.Doors.Count && i < 2; i++)
                {
                    var door = wall1.Doors[i];
                    int sketch = 8 + i;
                    string index = (i + 1).ToString();

                    SetDim($"Door{index}Display@Sketch{sketch}", door.Width * factor);
                    SetDim($"Door{index}length@Sketch{sketch}", door.Height * factor);
                    SetDim($"Door{index}BottomOffset@Sketch{sketch}", door.DistanceY * factor);
                    SetDim($"Door{index}RightOffset@Sketch{sketch}", door.DistanceX * factor);
                }
            }

            if (wall2.HasWindows && wall2.Windows != null)
            {
                for (int i = 0; i < wall2.Windows.Count && i < 3; i++)
                {
                    var win = wall2.Windows[i];
                    int sketch = 10 + i;

                    SetDim($"Window1Display@Sketch{sketch}", win.Width * factor);
                    SetDim($"Window1Length@Sketch{sketch}", win.Height * factor);
                    SetDim($"Window1BottomOffset@Sketch{sketch}", win.DistanceY * factor);
                    SetDim($"Window1RightOffset@Sketch{sketch}", win.DistanceX * factor);
                }
            }

            if (wall2.HasDoors && wall2.Doors != null)
            {
                for (int i = 0; i < wall2.Doors.Count && i < 2; i++)
                {
                    var door = wall2.Doors[i];
                    int sketch = 13 + i;

                    SetDim($"Door1Display@Sketch{sketch}", door.Width * factor);
                    SetDim($"Door1length@Sketch{sketch}", door.Height * factor);
                    SetDim($"Door1BottomOffset@Sketch{sketch}", door.DistanceY * factor);
                    SetDim($"Door1RightOffset@Sketch{sketch}", door.DistanceX * factor);
                }
            }

            Log("💾 حفظ النموذج وإغلاقه...");
            swModel.EditRebuild3();
            swModel.Save();
            swApp.CloseDoc(destPath);

            Log($"✅ تم حفظ ملف الجدران بنجاح: {destPath}");
        }
    }
}
