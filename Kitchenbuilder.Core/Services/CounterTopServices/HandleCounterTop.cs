using System.Text.Json;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core
{
    public class CountertopStation
    {
        public string SketchName { get; set; }
        public double? Left { get; set; }
        public double? Right { get; set; }

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
        public static void AddCountertopFields(string jsonPath)
        {
            if (!File.Exists(jsonPath)) return;

            var json = File.ReadAllText(jsonPath);
            var doc = JsonNode.Parse(json)?.AsObject();
            if (doc == null) return;

            foreach (var wallKey in new[] { "Wall1", "Wall2", "Wall3", "Wall4" })
            {
                if (!doc.ContainsKey(wallKey)) continue;
                var wall = doc[wallKey]?.AsObject();
                if (wall == null || !wall.ContainsKey("Bases")) continue;

                var bases = wall["Bases"]?.AsObject();
                if (bases == null) continue;

                foreach (var baseItem in bases)
                {
                    var baseObj = baseItem.Value?.AsObject();
                    if (baseObj == null) continue;

                    var visible = baseObj["Visible"]?.GetValue<bool>() ?? false;
                    var sketchName = baseObj["SketchName"]?.GetValue<string>() ?? "";

                    if (visible && !string.IsNullOrWhiteSpace(sketchName) && !sketchName.StartsWith("fridge_base"))
                    {
                        if (!baseObj.ContainsKey("Countertop") || baseObj["Countertop"] is null)
                        {
                            baseObj["Countertop"] = new JsonObject
                            {
                                ["Name"] = $"Extrude_CT_{sketchName}",
                                ["L"] = 0,
                                ["R"] = 0
                            };
                        }
                    }

                }
            }

            File.WriteAllText(jsonPath, doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
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
        public static void UpdateJsonCountertopDistances(string baseName, string currentSketch, double? left, double? right, Action<string> Log, bool removeObject = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseName) || string.IsNullOrWhiteSpace(currentSketch))
                    return;

                string jsonPath = Path.Combine(@"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON", $"{baseName}SLD.json");
                if (!File.Exists(jsonPath))
                {
                    Log("❌ JSON file not found when trying to update distances.");
                    return;
                }

                string jsonText = File.ReadAllText(jsonPath);
                using JsonDocument doc = JsonDocument.Parse(jsonText);
                var root = doc.RootElement.Clone();

                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

                writer.WriteStartObject();

                foreach (var wallProp in root.EnumerateObject())
                {
                    writer.WritePropertyName(wallProp.Name);
                    writer.WriteStartObject();

                    foreach (var section in wallProp.Value.EnumerateObject())
                    {
                        if (section.Name != "Bases")
                        {
                            section.WriteTo(writer);
                            continue;
                        }

                        writer.WritePropertyName("Bases");
                        writer.WriteStartObject();

                        foreach (var baseProp in section.Value.EnumerateObject())
                        {
                            writer.WritePropertyName(baseProp.Name);
                            writer.WriteStartObject();

                            string sketchName = "";
                            foreach (var field in baseProp.Value.EnumerateObject())
                            {
                                if (field.Name == "SketchName")
                                    sketchName = field.Value.GetString() ?? "";

                                if (field.Name != "Countertop")
                                    field.WriteTo(writer);
                            }

                            if (sketchName == currentSketch)
                            {
                                writer.WritePropertyName("Countertop");
                                if (removeObject)
                                {
                                    writer.WriteNullValue();
                                    Log($"🗑️ Removed Countertop object for {currentSketch}");
                                }
                                else
                                {
                                    writer.WriteStartObject();
                                    writer.WriteString("Name", $"Extrude_CT_{currentSketch}");
                                    writer.WriteNumber("L", Math.Abs(left ?? 0));
                                    writer.WriteNumber("R", Math.Abs(right ?? 0));
                                    writer.WriteEndObject();
                                    Log($"✅ Updated L/R values for {currentSketch}");
                                }
                            }
                            else
                            {
                                // Preserve existing countertop data
                                if (baseProp.Value.TryGetProperty("Countertop", out var ct))
                                {
                                    writer.WritePropertyName("Countertop");
                                    ct.WriteTo(writer);
                                }
                            }


                            writer.WriteEndObject(); // base
                        }

                        writer.WriteEndObject(); // Bases
                    }

                    writer.WriteEndObject(); // wall
                }

                writer.WriteEndObject(); // root
                writer.Flush();

                File.WriteAllBytes(jsonPath, stream.ToArray());
            }
            catch (Exception ex)
            {
                Log($"❌ Failed to update JSON: {ex.Message}");
            }
        }
        public static void DeleteSketchByName(IModelDoc2 modelDoc, string fullSketchName, Action<string> log)
        {
            if (modelDoc is not PartDoc partDoc)
            {
                log("❌ ModelDoc is not a PartDoc.");
                return;
            }
            // ✅ Exit sketch edit mode if active
            modelDoc.SketchManager.InsertSketch(true);
            log("↩️ Exited sketch mode if active.");


            // 1. Delete the extrusion feature
            string extrudeName = $"Extrude_{fullSketchName}"; // don't double-prefix
            var extrudeObj = partDoc.FeatureByName(extrudeName);
            if (extrudeObj is IFeature extrudeFeature)
            {
                bool selected = extrudeFeature.Select2(false, 0);
                if (selected)
                {
                    modelDoc.EditDelete();
                    log($"🗑️ Deleted extrusion feature {extrudeName}");
                }
                else
                {
                    log($"❌ Could not select extrusion feature {extrudeName}");
                }
            }
            else
            {
                log($"⚠️ Extrusion feature {extrudeName} not found");
            }

            // 2. Delete the sketch itself
            var sketchObj = partDoc.FeatureByName(fullSketchName); // no prefix
            if (sketchObj is IFeature sketchFeature)
            {
                bool selected = sketchFeature.Select2(false, 0);
                if (selected)
                {
                    modelDoc.EditDelete();
                    log($"🗑️ Deleted sketch {fullSketchName}");
                }
                else
                {
                    log($"❌ Could not select sketch {fullSketchName}");
                }
            }
            else
            {
                log($"⚠️ Sketch {fullSketchName} not found");
            }
        }

    }


}

