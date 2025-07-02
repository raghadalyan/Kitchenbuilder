using SolidWorks.Interop.sldworks;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using SysEnv = System.Environment;

namespace Kitchenbuilder.Core
{
    public static class CheckVisibleCountertops
    {
        private static readonly string LogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\CheckVisibleCountertopsLog.txt";

        public static void ProcessVisibleCountertops(string jsonPath, SolidWorksSessionService session)
        {
            IModelDoc2 model = session.GetActiveModel();
            if (model == null)
            {
                Log("❌ SolidWorks model not available.");
                return;
            }

            if (!File.Exists(jsonPath))
            {
                Log($"❌ File not found: {jsonPath}");
                return;
            }

            string content = File.ReadAllText(jsonPath);
            JsonObject json = JsonNode.Parse(content).AsObject();
            bool modified = false;

            for (int i = 1; i <= 4; i++)
            {
                string wallKey = $"Wall{i}";
                if (!json.ContainsKey(wallKey)) continue;

                JsonObject wall = json[wallKey]?.AsObject();
                if (wall == null || !wall.ContainsKey("Bases")) continue;

                JsonObject bases = wall["Bases"]?.AsObject();
                if (bases == null) continue;

                foreach (var basePair in bases)
                {
                    string baseName = basePair.Key;
                    JsonObject baseObj = basePair.Value?.AsObject();
                    if (baseObj == null) continue;

                    bool isVisible = baseObj["Visible"]?.GetValue<bool>() ?? false;
                    if (!isVisible)
                    {
                        Log($"⛔ Skipping {baseName} - base is not visible.");
                        continue;
                    }

                    string sketchName = baseObj["SketchName"]?.GetValue<string>() ?? "";
                    if (string.IsNullOrWhiteSpace(sketchName))
                    {
                        Log($"⚠️ {baseName} has no SketchName, skipping.");
                        continue;
                    }

                    string ctName = $"Extrude_CT_{sketchName}";

                    // Show in SolidWorks
                    Log($"👁️ Showing countertop body: {ctName}");
                    Show_Bodies_In_Sld.ShowBody((ModelDoc2)model, ctName);

                    // Inject Countertop array into JSON
                    JsonArray ctArray = new JsonArray
            {
                new JsonObject
                {
                    ["Name"] = ctName,
                    ["R"] = 0,
                    ["L"] = 0
                }
            };

                    baseObj["Countertop"] = ctArray;
                    modified = true;

                    Log($"📝 Added Countertop field to {baseName} with {ctName}");
                }
            }

            // Save modified JSON
            if (modified)
            {
                File.WriteAllText(jsonPath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
                Log("💾 Updated JSON file with Countertop fields.");
            }
            else
            {
                Log("ℹ️ No changes made to JSON.");
            }
        }


        private static void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(LogPath, $"{timestamp} {message}{SysEnv.NewLine}");
        }
    }
}
