using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core
{
    public static class CabinetJsonAnalyzer
    {
        public static string GenerateInitialPrompt(string jsonPath)
        {
            try
            {
                string json = File.ReadAllText(jsonPath);
                JsonObject data = JsonNode.Parse(json)?.AsObject();
                if (data == null)
                    return "⚠️ Failed to read the kitchen layout.";

                var prompt = "👋 Hi! I'm your Cabinets Builder Helper. Let's start building your lower kitchen cabinets together.\n";

                foreach (var wall in data)
                {
                    if (!wall.Key.StartsWith("Wall")) continue;

                    var bases = wall.Value?["Bases"]?.AsObject();
                    if (bases == null) continue;

                    foreach (var basePair in bases)
                    {
                        var baseObj = basePair.Value?.AsObject();
                        if (baseObj == null) continue;

                        bool visible = baseObj["Visible"]?.GetValue<bool>() == true;
                        bool hasCountertop = baseObj.ContainsKey("Countertop") && baseObj["Countertop"]?.ToString() != "null";
                        if (!visible || !hasCountertop) continue;

                        string sketch = baseObj["SketchName"]?.ToString() ?? "unknown";
                        double start = baseObj["Start"]?.GetValue<double>() ?? 0;
                        double end = baseObj["End"]?.GetValue<double>() ?? 0;
                        double width = end - start;

                        prompt += $"\n👉 Now handling `{sketch}` on {wall.Key} (from {start} to {end} cm, total width: {width} cm).\n";
                        prompt += $"❓ What cabinet layout would you like here?\nPlease suggest cabinet widths (standard sizes are 45, 50, 60 cm).\n";
                        prompt += $"Example: 45 + 60 + 60 + 60 = {width}? (Your total space is {width} cm)\n";
                        prompt += $"Let me know if any of these should be drawers!\n";
                    }
                }

                if (!prompt.Contains("handling"))
                    return "⚠️ No visible bases with countertops found.";

                return prompt;
            }
            catch (Exception ex)
            {
                return $"❌ Error analyzing cabinet JSON: {ex.Message}";
            }
        }
    }
}
