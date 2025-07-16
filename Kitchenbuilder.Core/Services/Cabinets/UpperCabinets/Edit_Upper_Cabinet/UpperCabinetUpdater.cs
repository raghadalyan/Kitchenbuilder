using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Kitchenbuilder.Core.Models;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public static class UpperCabinetUpdater
    {
        public static bool UpdateCabinet(
           string sketchName,
           double newWidth,
           double? newHeight,
           double? newDepth,
           double? distanceX,
           double? distanceY,
           IModelDoc2 model,
           int wallNumber,
           out string errorMessage)
        {
            errorMessage = "";

            try
            {
                // 1. Update the JSON file first
                string jsonPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Kitchenbuilder\JSON\UpperCabinets.json";
                if (!File.Exists(jsonPath))
                {
                    errorMessage = "❌ JSON file not found.";
                    return false;
                }

                var json = File.ReadAllText(jsonPath);
                var data = JsonSerializer.Deserialize<Dictionary<string, WallCabinetWrapper>>(json);
                string wallKey = $"Wall{wallNumber}";

                if (data == null || !data.TryGetValue(wallKey, out var wrapper))
                {
                    errorMessage = "❌ Wall data not found in JSON.";
                    return false;
                }

                var cabinet = wrapper.Cabinets.FirstOrDefault(c => c.SketchName == sketchName);
                if (cabinet == null)
                {
                    errorMessage = "❌ Cabinet not found.";
                    return false;
                }

                // 2. Apply changes in SolidWorks
                if (!string.IsNullOrEmpty(sketchName))
                {
                    EditSketchDim_IModel.SetDimension(model, $"Length@{sketchName}", newWidth);
                    if (newHeight.HasValue)
                        EditSketchDim_IModel.SetDimension(model, $"Width@{sketchName}", newHeight.Value);
                    if (newDepth.HasValue && cabinet.ExtrudeName != null)
                        EditExtrusionDim_IModel.EditExtrude(model, cabinet.ExtrudeName, newDepth.Value);
                    if (distanceX.HasValue)
                        EditSketchDim_IModel.SetDimension(model, $"DistanceX@{sketchName}", distanceX.Value);
                    if (distanceY.HasValue)
                        EditSketchDim_IModel.SetDimension(model, $"DistanceY@{sketchName}", distanceY.Value);
                }

                // 3. Update memory
                cabinet.Width = (int)newWidth;
                if (newHeight.HasValue) cabinet.Height = (int)newHeight.Value;
                if (newDepth.HasValue) cabinet.Depth = (int)newDepth.Value;
                if (distanceX.HasValue) cabinet.DistanceX = (int)distanceX.Value;
                if (distanceY.HasValue) cabinet.DistanceY = (int)distanceY.Value;

                string updatedJson = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(jsonPath, updatedJson);

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "❌ " + ex.Message;
                return false;
            }
        }



        public static bool DeleteCabinet(IModelDoc2 model, CabinetInfo? cabinet, Space? space, out string errorMessage)
        {
            errorMessage = "";
            try
            {
                string? bodyName = cabinet != null
                    ? $"Extrude_Drawers{cabinet.SketchName?.Replace("Sketch_Cabinet", "")}"
                    : space?.Type?.Trim() switch
                    {
                        "Microwave" => "Microwave1",
                        "Oven" => "Oven1",
                        "Range Hood" => "Range Hood1",
                        "DishWasher" => "DishWasher1",
                        _ => null
                    };

                if (string.IsNullOrEmpty(bodyName))
                {
                    errorMessage = "❌ Could not determine body name.";
                    return false;
                }

                Hide_Bodies_In_Sld_IModel.HideMultipleBodies(model, new[] { bodyName });

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }


    }
}
