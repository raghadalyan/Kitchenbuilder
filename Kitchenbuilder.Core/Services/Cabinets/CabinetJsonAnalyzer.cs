//using System.Text.Json.Nodes;

//namespace Kitchenbuilder.Core
//{
//    public static class CabinetJsonAnalyzer
//    {
//        public static string GenerateInitialPrompt(string jsonPath)
//        {
//            try
//            {
//                string json = File.ReadAllText(jsonPath);
//                JsonObject data = JsonNode.Parse(json)?.AsObject();
//                if (data == null)
//                    return "⚠️ Failed to read the kitchen layout.";

//                var lowerCabinetBases = new List<string>();

//                foreach (var wall in data)
//                {
//                    if (!wall.Key.StartsWith("Wall")) continue;

//                    var wallObj = wall.Value?.AsObject();
//                    if (wallObj == null || !wallObj.ContainsKey("Bases")) continue;

//                    var bases = wallObj["Bases"]?.AsObject();
//                    if (bases == null) continue;

//                    foreach (var basePair in bases)
//                    {
//                        var baseObj = basePair.Value?.AsObject();
//                        if (baseObj == null) continue;

//                        bool visible = baseObj["Visible"]?.GetValue<bool>() == true;
//                        bool hasCountertop = baseObj.ContainsKey("Countertop") && baseObj["Countertop"]?.ToString() != "null";

//                        if (visible && hasCountertop)
//                        {
//                            string sketch = baseObj["SketchName"]?.ToString() ?? "unknown";
//                            lowerCabinetBases.Add($"• {wall.Key} / {sketch}");
//                        }
//                    }
//                }

//                if (lowerCabinetBases.Count == 0)
//                    return "⚠️ No visible bases with countertops found.";

//                string baseList = string.Join("\n", lowerCabinetBases);
//                return $"🔧 We're now handling the **lower cabinets** under the countertop.\nThe following bases are relevant:\n{baseList}\n\n🧠 Please explain how you want the cabinets to look:\n- How many cabinets?\n- Are any of them drawers?\n- What sizes?";
//            }
//            catch (Exception ex)
//            {
//                return $"❌ Error analyzing cabinet JSON: {ex.Message}";
//            }
//        }
//    }
//}
