using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace Kitchenbuilder.Core
{
    public class CountertopDimensionApplier
    {
        private static readonly string debugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\countertop_debug.txt";

        private static void Log(string message)
        {
            File.AppendAllText(debugPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
        }

        // ✅ النسخة المعدلة لتطبيق الأبعاد فقط على السكيتش الحالي المفتوح
        public static void ApplyDimensionsToCurrentSketch(ISldWorks swApp, double left, double right)
        {
            IModelDoc2 model = swApp?.ActiveDoc as IModelDoc2;
            if (model == null)
            {
                Log("❌ No active SolidWorks document.");
                return;
            }

            SketchManager sketchMgr = model.SketchManager;
            object activeSketchObj = sketchMgr?.ActiveSketch;

            if (activeSketchObj == null)
            {
                Log("❌ No active sketch found.");
                return;
            }

            Feature sketchFeature = activeSketchObj as Feature;
            if (sketchFeature == null)
            {
                Log("❌ Could not retrieve sketch feature.");
                return;
            }

            string sketchName = sketchFeature.Name;
            string dimL = $"L@{sketchName}";
            string dimR = $"R@{sketchName}";

            // 🧠 محاولة معرفة الحيطة من اسم السكيتش
            string wallNumber = sketchName.Split('_').FirstOrDefault(); // مثلًا 1 من 1_2
            string wallName = wallNumber != null ? $"Wall{wallNumber}" : "";

            // 👈 حسب الحيطة نقرر إشارة البعد R
            double finalLeft = -left;
            double finalRight;
            switch (wallName)
            {
                case "Wall1":
                case "Wall3":
                    finalRight = right;
                    break;
                case "Wall2":
                case "Wall4":
                    finalRight = -right;
                    break;
                default:
                    finalRight = right;
                    break;
            }

            Log($"🧩 Applying to sketch '{sketchName}' on {wallName} → L = {finalLeft}, R = {finalRight}");

            Dimension dim1 = model.Parameter(dimL) as Dimension;
            Dimension dim2 = model.Parameter(dimR) as Dimension;

            bool successL = false, successR = false;

            if (dim1 != null)
            {
                dim1.SystemValue = finalLeft / 100.0;
                successL = true;
            }

            if (dim2 != null)
            {
                dim2.SystemValue = finalRight / 100.0;
                successR = true;
            }

            Log(successL ? $"✅ Set {dimL} to {finalLeft}" : $"❌ Failed to set {dimL}");
            Log(successR ? $"✅ Set {dimR} to {finalRight}" : $"❌ Failed to set {dimR}");

            model.EditRebuild3();
            Log("✅ Finished applying dimensions to current sketch.");
        }



        // ✅ النسخة المعدلة لتطبيق الأبعاد من JSON على سكيتش معين فقط
        // ✅ النسخة المعدلة لتطبيق الأبعاد من JSON على سكيتش معين فقط
        public static void ApplyDimensionsFromJson(string jsonPath, ISldWorks swApp, string sketchName)
        {
            Log("🚀 Starting CountertopDimensionApplier...");

            if (!File.Exists(jsonPath))
            {
                Log($"❌ JSON file not found: {jsonPath}");
                return;
            }

            IModelDoc2 model = swApp.ActiveDoc as IModelDoc2;
            if (model == null)
            {
                Log("❌ No active SolidWorks document.");
                return;
            }

            string expectedExtrudeName = $"Extrude_CT_{sketchName}";

            JsonDocument doc = JsonDocument.Parse(File.ReadAllText(jsonPath));

            foreach (var wallName in new[] { "Wall1", "Wall2", "Wall3", "Wall4" })
            {
                if (!doc.RootElement.TryGetProperty(wallName, out var wall) ||
                    !wall.TryGetProperty("Bases", out var bases))
                    continue;

                foreach (var baseItem in bases.EnumerateObject())
                {
                    var baseValue = baseItem.Value;

                    if (!baseValue.TryGetProperty("Countertop", out var countertopArray) ||
                        countertopArray.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var ct in countertopArray.EnumerateArray())
                    {
                        if (!ct.TryGetProperty("Name", out var nameProp) ||
                            !ct.TryGetProperty("L", out var lProp) ||
                            !ct.TryGetProperty("R", out var rProp))
                            continue;

                        string extrudeName = nameProp.GetString();
                        Log($"🔍 Comparing JSON Name = {extrudeName} with expected = {expectedExtrudeName}");

                        if (!string.Equals(extrudeName, expectedExtrudeName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        double L = lProp.GetDouble();
                        double R = rProp.GetDouble();

                        string dimL = $"L@CT_{sketchName}";
                        string dimR = $"R@CT_{sketchName}";

                        // ✅ حسب اسم الحيطة نغير الإشارة
                        double finalL = -L;
                        double finalR;

                        switch (wallName)
                        {
                            case "Wall1":
                            case "Wall3":
                                finalR = R;
                                break;
                            case "Wall2":
                            case "Wall4":
                                finalR = -R;
                                break;
                            default:
                                finalR = R;
                                break;
                        }


                        Log($"🏠 Sketch '{sketchName}' belongs to wall: {wallName}");
                        Log($"🧩 Trying to set {dimL} = {finalL}, {dimR} = {finalR}");

                        Dimension dim1 = model.Parameter(dimL) as Dimension;
                        Dimension dim2 = model.Parameter(dimR) as Dimension;

                        bool successL = false, successR = false;

                        if (dim1 != null)
                        {
                            dim1.SystemValue = finalL / 100.0;
                            successL = true;
                        }
                        if (dim2 != null)
                        {
                            dim2.SystemValue = finalR / 100.0;
                            successR = true;
                        }

                        Log(successL ? $"✅ Set {dimL} to {finalL}" : $"❌ Failed to set {dimL}");
                        Log(successR ? $"✅ Set {dimR} to {finalR}" : $"❌ Failed to set {dimR}");

                        model.EditRebuild3();
                        Log("✅ Finished applying dimensions to sketch.");
                        return;
                    }
                }
            }

            Log("⚠️ No matching sketch found in any wall.");
        }



    }
}
