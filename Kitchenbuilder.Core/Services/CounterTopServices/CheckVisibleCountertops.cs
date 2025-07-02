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
                    if (!isVisible) continue;

                    bool isCountertopVisible = baseObj["IsCountertopVisible"]?.GetValue<bool>() ?? true;
                    if (!isCountertopVisible)
                    {
                        Log("⚠️ Countertop is marked as not visible, skipping.");
                        continue;
                    }

                    if (!baseObj.ContainsKey("Countertop")) continue;

                    var countertopArray = baseObj["Countertop"]?.AsArray();
                    if (countertopArray == null) continue;

                    foreach (var item in countertopArray)
                    {
                        string name = item?["Name"]?.GetValue<string>() ?? "";
                        if (string.IsNullOrWhiteSpace(name)) continue;

                        if (name.ToLower().Contains("fridge"))
                        {
                            Log($"🚫 Skipping fridge-related Countertop: {name}");
                            continue;
                        }

                        Log($"👁️ Requesting to show countertop body: {name}");
                        Show_Bodies_In_Sld.ShowBody((ModelDoc2)model, name);
                    }
                }
            }
        }

        private static void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(LogPath, $"{timestamp} {message}{SysEnv.NewLine}");
        }
    }
}
