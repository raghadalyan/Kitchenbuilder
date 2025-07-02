using System.Text.Json;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public class CountertopStation
    {
        public string SketchName { get; set; }
    }

    public static class HandleCounterTop
    {
        public static List<CountertopStation> ExtractCountertopStations(string jsonPath)
        {
            var stations = new List<CountertopStation>();
            if (!File.Exists(jsonPath)) return stations;

            var json = File.ReadAllText(jsonPath);
            var doc = JsonDocument.Parse(json);

            foreach (var wallName in new[] { "Wall1", "Wall2", "Wall3", "Wall4" })
            {
                if (!doc.RootElement.TryGetProperty(wallName, out var wall)) continue;
                if (!wall.TryGetProperty("Bases", out var bases)) continue;

                foreach (var baseItem in bases.EnumerateObject())
                {
                    var baseValue = baseItem.Value;
                    if (!baseValue.TryGetProperty("Visible", out var vis) || !vis.GetBoolean()) continue;
                    if (!baseValue.TryGetProperty("SketchName", out var sketchJson)) continue;

                    string sketchName = sketchJson.GetString();
                    if (string.IsNullOrWhiteSpace(sketchName)) continue;
                    if (sketchName.StartsWith("fridge_base")) continue; // skip fridge

                    stations.Add(new CountertopStation { SketchName = sketchName });
                }
            }

            return stations;
        }
        public static void EditSketchByName(IModelDoc2 modelDoc, string sketchName, Action<string> log)
        {
            if (modelDoc is PartDoc partDoc)
            {
                var featureObj = partDoc.FeatureByName(sketchName);
                if (featureObj is IFeature feature)
                {
                    bool success = feature.Select2(false, 0);
                    if (success)
                    {
                        modelDoc.EditSketch();
                        log($"✏️ Editing sketch {sketchName}");
                    }
                    else
                    {
                        log($"❌ Could not select sketch {sketchName}");
                    }
                }
                else
                {
                    log($"❌ Feature {sketchName} not found.");
                }
            }
            else
            {
                log("❌ ModelDoc is not a PartDoc.");
            }
        }
    }
}

