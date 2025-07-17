using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Kitchenbuilder.Core.Models;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public static class CabinetUpdater
    {
        public static bool UpdateCabinet(
            string sketchName,
            int optionNumber,
            int newWidth,
            int? newHeight,
            int drawerCount,
            bool hasDrawers,
            IModelDoc2 model,                     // ✅ new parameter
            out string errorMessage)
        {
            errorMessage = "";

            // ✅ Validation
            if (newWidth < 5)
            {
                errorMessage = "❌ Width must be at least 5 cm.";
                return false;
            }

            if (newHeight.HasValue && newHeight.Value < 5)
            {
                errorMessage = "❌ Height must be at least 5 cm.";
                return false;
            }

            // ✅ File path
            string jsonPath = Path.Combine(KitchenConfig.Get().BasePath, "Kitchenbuilder", "Kitchenbuilder", "JSON", $"Option{optionNumber}SLD_stations.json");

            if (!File.Exists(jsonPath))
            {
                errorMessage = $"❌ JSON file not found: {jsonPath}";
                return false;
            }

            // ✅ Load and deserialize
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            string json = File.ReadAllText(jsonPath);
            var stations = JsonSerializer.Deserialize<List<StationInfo>>(json, options);

            if (stations == null)
            {
                errorMessage = "❌ Failed to load JSON content.";
                return false;
            }

            // ✅ Update the cabinet
            foreach (var station in stations)
            {
                var cabinet = station.Cabinets?.FirstOrDefault(c => c.SketchName == sketchName);
                if (cabinet != null)
                {
                    // ✅ Width boundary check
                    if (cabinet.DistanceX + newWidth > station.StationEnd)
                    {
                        errorMessage = $"❌ Cabinet width exceeds station boundary. Max allowed width is {station.StationEnd - cabinet.DistanceX} cm.";
                        return false;
                    }

                    cabinet.Width = newWidth;

                    if (newHeight.HasValue)
                        cabinet.Height = newHeight.Value;

                    cabinet.HasDrawers = hasDrawers;

                    string suffix = sketchName.Replace("Sketch_Cabinet", "");
                    var drawers = new Drawers($"Drawers{suffix}");

                    if (hasDrawers)
                    {
                        double height = cabinet.Height;
                        double availableHeight = height - 4 - (drawerCount > 1 ? 2 * (drawerCount - 1) : 0);
                        double drawerHeight = drawerCount > 0 ? availableHeight / drawerCount : 0;

                        for (int d = 1; d <= 5; d++)
                        {
                            if (d <= drawerCount)
                            {
                                double dy = 2 + (drawerHeight + 2) * (drawerCount - d);
                                typeof(Drawers).GetProperty($"Width{d}")?.SetValue(drawers, drawerHeight);
                                typeof(Drawers).GetProperty($"DistanceY{d}")?.SetValue(drawers, dy);
                            }
                            else
                            {
                                typeof(Drawers).GetProperty($"Width{d}")?.SetValue(drawers, 0.0);
                                typeof(Drawers).GetProperty($"DistanceY{d}")?.SetValue(drawers, 0.0);
                            }
                        }

                        cabinet.HasDrawers = true;
                    }
                    else
                    {
                        // Default 1-door drawer only
                        for (int d = 1; d <= 5; d++)
                        {
                            if (d == 1)
                            {
                                typeof(Drawers).GetProperty("Width1")?.SetValue(drawers, cabinet.Height - 4);
                                typeof(Drawers).GetProperty("DistanceY1")?.SetValue(drawers, 2.0);
                            }
                            else
                            {
                                typeof(Drawers).GetProperty($"Width{d}")?.SetValue(drawers, 0.0);
                                typeof(Drawers).GetProperty($"DistanceY{d}")?.SetValue(drawers, 0.0);
                            }
                        }

                        cabinet.HasDrawers = false;
                    }

                    cabinet.Drawers = drawers;


                    // ✅ Save
                    File.WriteAllText(jsonPath, JsonSerializer.Serialize(stations, new JsonSerializerOptions { WriteIndented = true }));
                    if (model != null)
                        CabinetEditorDim.ApplyDimensions(model, cabinet);

                    return true;
                }

            }

            errorMessage = $"❌ Cabinet with SketchName '{sketchName}' not found.";
            return false;
        }
        public static bool DeleteCabinet(
    IModelDoc2 model,
    CabinetInfo cabinet,
    int optionNumber,
    out string errorMessage)
        {
            errorMessage = "";
            try
            {
                // 1. Hide the body
                string suffix = cabinet.SketchName.Replace("Sketch_Cabinet", ""); // ➡️ "2_4"
                string bodyName = $"Extrude_Drawers{suffix}";

                Hide_Bodies_In_Sld_IModel.HideMultipleBodies(model, new[] { bodyName });

                // 2. Update the JSON
                string jsonPath = Path.Combine(KitchenConfig.Get().BasePath, "Kitchenbuilder", "Kitchenbuilder", "JSON", $"Option{optionNumber}SLD_stations.json");
                if (!File.Exists(jsonPath))
                {
                    errorMessage = $"❌ File not found: {jsonPath}";
                    return false;
                }

                string json = File.ReadAllText(jsonPath);
                var stations = JsonSerializer.Deserialize<List<StationInfo>>(json)!;

                var station = stations.FirstOrDefault(s => s.Cabinets.Any(c => c.SketchName == cabinet.SketchName));
                if (station == null)
                {
                    errorMessage = $"❌ Station not found for cabinet {cabinet.SketchName}";
                    return false;
                }

                station.Cabinets.RemoveAll(c => c.SketchName == cabinet.SketchName);
                File.WriteAllText(jsonPath, JsonSerializer.Serialize(stations, new JsonSerializerOptions { WriteIndented = true }));

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"❌ Exception: {ex.Message}";
                return false;
            }
        }

    }


}
