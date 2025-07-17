using SolidWorks.Interop.sldworks;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core
{
    public static class CheckVisibleElements
    {
        private static readonly string LogPath = Path.Combine(KitchenConfig.Get().BasePath, "Kitchenbuilder", "Output", "CheckVisibleElementsLog.txt");

        public static void ProcessVisibleBases(string jsonPath, ModelDoc2 model)
        {
            if (!File.Exists(jsonPath))
            {
                Log($"❌ File not found: {jsonPath}");
                return;
            }

            if (model == null)
            {
                Log("❌ Model is null.");
                return;
            }

            string content = File.ReadAllText(jsonPath);
            JsonObject json = JsonNode.Parse(content).AsObject();

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
                    JsonObject baseObj = basePair.Value?.AsObject();
                    if (baseObj == null) continue;

                    bool isVisible = baseObj["Visible"]?.GetValue<bool>() ?? false;
                    string sketchName = baseObj["SketchName"]?.GetValue<string>();
                    string extrudeName = baseObj["ExtrudeName"]?.GetValue<string>();

                    if (!isVisible || string.IsNullOrEmpty(extrudeName)) continue;

                    // Special handling for fridge base
                    if (!string.IsNullOrEmpty(sketchName) && sketchName.StartsWith("fridge_base"))
                    {
                        string number = sketchName.Replace("fridge_base", "").Trim();

                        string[] fridgeBodies =
                        {
                            $"Extrude_fridge_base{number}[1]",
                            $"Extrude_fridge_base{number}[2]",
                            $"<Fridge{number}>-<Extrude-Thin1>",
                            $"<Fridge{number}>-<Cut-Extrude2[1]>",
                            $"<Fridge{number}>-<Cut-Extrude3>",
                            $"<Fridge{number}>-<Cut-Extrude2[2]>"
                        };

                        foreach (var bodyName in fridgeBodies)
                        {
                            Log($"👁️ Requesting to show fridge body: {bodyName}");
                            Show_Bodies_In_Sld.ShowBody(model, bodyName);
                        }
                    }
                    else
                    {
                        Log($"👁️ Requesting to show: {extrudeName}");
                        Show_Bodies_In_Sld.ShowBody(model, extrudeName);
                    }
                }
            }
        }

        private static void Log(string message)
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(LogPath, $"[{timestamp}] {message}{System.Environment.NewLine}");
        }
    }
}
