
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

        public static void ApplyDimensionsFromJson(string jsonPath, ISldWorks swApp)
        {
            Log("🚀 Starting CountertopDimensionApplier...");

            if (!File.Exists(jsonPath))
            {
                Log($"❌ JSON file not found: {jsonPath}");
                return;
            }

            JsonDocument doc = JsonDocument.Parse(File.ReadAllText(jsonPath));

            if (!doc.RootElement.TryGetProperty("Wall1", out var wall) ||
                !wall.TryGetProperty("Bases", out var bases))
            {
                Log("❌ 'Wall1.Bases' not found in JSON.");
                return;
            }

            IModelDoc2 model = swApp.ActiveDoc as IModelDoc2;
            if (model == null)
            {
                Log("❌ No active SolidWorks document.");
                return;
            }

            foreach (var baseItem in bases.EnumerateObject())
            {
                var baseValue = baseItem.Value;

                if (!baseValue.TryGetProperty("Countertop", out var countertopArray) ||
                    countertopArray.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var ct in countertopArray.EnumerateArray())
                {
                    if (!ct.TryGetProperty("Name", out var nameProp) ||
                        !ct.TryGetProperty("L", out var lProp) ||
                        !ct.TryGetProperty("R", out var rProp))
                    {
                        continue;
                    }

                    string name = nameProp.GetString();
                    double L = lProp.GetDouble();
                    double R = rProp.GetDouble();

                    string sketchName = name.Replace("Extrude_", "");
                    string dimL = $"L@{sketchName}";
                    string dimR = $"R@{sketchName}";

                    Log($"🧩 Trying to set {dimL} = {-L}, {dimR} = {-R}");

                    Dimension dim1 = model.Parameter(dimL) as Dimension;
                    Dimension dim2 = model.Parameter(dimR) as Dimension;

                    bool successL = false, successR = false;

                    if (dim1 != null)
                    {
                        dim1.SystemValue = L / 100.0;  // ✅ عكس الإشارة
                        successL = true;
                    }
                    if (dim2 != null)
                    {
                        dim2.SystemValue = R / 100.0;  // ✅ عكس الإشارة
                        successR = true;
                    }

                    Log(successL ? $"✅ Set {dimL} to {-L}" : $"❌ Failed to set {dimL}");
                    Log(successR ? $"✅ Set {dimR} to {-R}" : $"❌ Failed to set {dimR}");

                }
            }

            model.EditRebuild3();
            Log("✅ Done applying all countertop dimensions.");
        }
    }
}
