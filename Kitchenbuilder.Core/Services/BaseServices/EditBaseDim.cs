using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Kitchenbuilder.Core
{
    public static class EditBaseDim
    {
        private const string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\edit base debug.txt";

        public static Dictionary<int, (double start, double end, double length)> EditDimensions(
            string filePath,
            Dictionary<int, string> suggestedDescriptions)
        {
            File.AppendAllText(DebugPath, $"\n🛠️ Starting EditBaseDim at {DateTime.Now}\n");

            var wallData = new Dictionary<int, (double start, double end, double length)>();

            if (suggestedDescriptions == null || suggestedDescriptions.Count == 0)
            {
                File.AppendAllText(DebugPath, "❌ No suggested descriptions provided.\n");
                return wallData;
            }

            string description = string.Join("\n", suggestedDescriptions.Values);
            File.AppendAllText(DebugPath, $"Combined Description:\n{description}\n");

            var matches = Regex.Matches(description, @"Wall\s+(\d):\s*(\d+)-(\d+)");
            if (matches.Count == 0)
            {
                File.AppendAllText(DebugPath, "❌ No wall data found in the description.\n");
                return wallData;
            }

            foreach (Match match in matches)
            {
                int wallNum = int.Parse(match.Groups[1].Value);
                double start = double.Parse(match.Groups[2].Value);
                double end = double.Parse(match.Groups[3].Value);
                double length = end - start;

                if (!wallData.ContainsKey(wallNum))
                {
                    wallData[wallNum] = (start, end, length);
                    File.AppendAllText(DebugPath, $"✅ Found Wall {wallNum} from {start} to {end} → {length} cm\n");
                }
            }

            File.AppendAllText(DebugPath, $"📌 Number of unique walls to handle: {wallData.Count}\n");

            // === Start SolidWorks
            var swApp = Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application")) as SldWorks;
            if (swApp == null)
            {
                File.AppendAllText(DebugPath, "❌ Could not launch SolidWorks.\n");
                return wallData;
            }

            int errors = 0, warnings = 0;
            var swModel = swApp.OpenDoc6(
                filePath,
                (int)swDocumentTypes_e.swDocPART,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                "",
                ref errors,
                ref warnings) as ModelDoc2;

            if (swModel == null)
            {
                File.AppendAllText(DebugPath, $"❌ Could not open model: {filePath}\n");
                return wallData;
            }

            DeleteUnwantedFeaturesAndSketches(swModel);

            if (wallData.TryGetValue(1, out var wall1Info))
            {
                string fridgeSide = DetectFridgeSide(description);
                EditWall1(swModel, wall1Info, fridgeSide);
            }
            else
            {
                File.AppendAllText(DebugPath, "⚠️ Wall 1 not found, so no fridge dimensions were updated.\n");
            }

            swModel.EditRebuild3();
            swModel.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref errors, ref warnings);
            swApp.CloseDoc(filePath);

            return wallData;
        }

        private static void DeleteUnwantedFeaturesAndSketches(ModelDoc2 swModel)
        {
            var ext = swModel.Extension;

            string[] featuresToDelete = { "Extrude_Left_base1", "Extrude_Right_base1" };
            foreach (var featName in featuresToDelete)
            {
                if (ext.SelectByID2(featName, "BODYFEATURE", 0, 0, 0, false, 0, null, 0))
                {
                    swModel.EditDelete();
                    File.AppendAllText(DebugPath, $"🗑️ Deleted feature: {featName}\n");
                }
                else
                {
                    File.AppendAllText(DebugPath, $"⚠️ Could not find feature: {featName}\n");
                }
            }

            string[] sketchesToDelete = { "Left_base1", "Right_base1" };
            foreach (var sketchName in sketchesToDelete)
            {
                if (ext.SelectByID2(sketchName, "SKETCH", 0, 0, 0, false, 0, null, 0))
                {
                    swModel.EditDelete();
                    File.AppendAllText(DebugPath, $"🗑️ Deleted sketch: {sketchName}\n");
                }
                else
                {
                    File.AppendAllText(DebugPath, $"⚠️ Could not find sketch: {sketchName}\n");
                }
            }
        }

        private static string DetectFridgeSide(string description)
        {
            if (description.Contains("fridge placement: right"))
                return "right";
            if (description.Contains("fridge placement: left"))
                return "left";
            return "unknown";
        }

        private static void EditWall1(ModelDoc2 swModel, (double start, double end, double length) wall1Info, string fridgeSide)
        {
            double start = wall1Info.start;
            double end = wall1Info.end;
            double wallWidth = end - start;

            // ✅ Edit width@master_wall1
            var wallWidthDim = swModel.Parameter("width@master_wall1") as Dimension;
            if (wallWidthDim != null)
            {
                wallWidthDim.SystemValue = wallWidth / 100.0;
                File.AppendAllText(DebugPath, $"✏️ Set width@master_wall1 = {wallWidth} cm\n");
            }
            else
            {
                File.AppendAllText(DebugPath, "❌ Could not find dimension: width@master_wall1\n");
            }

            // ✅ Edit DistanceX@fridge_base1
            var dimX = swModel.Parameter("DistanceX@fridge_base1") as Dimension;
            if (dimX != null)
            {
                dimX.SystemValue = start / 100.0;
                File.AppendAllText(DebugPath, $"✏️ Set DistanceX@fridge_base1 = {start} cm\n");
            }
            else
            {
                File.AppendAllText(DebugPath, "❌ Could not find dimension: DistanceX@fridge_base1\n");
            }

            // ✅ Prepare base lengths
            double adjustedLength = end - 85;
            double leftBase = fridgeSide == "right" ? adjustedLength : 2;
            double rightBase = fridgeSide == "left" ? adjustedLength : 2;

            // ✅ Call the external function
            EditFridgeRelatedDimensions.Edit(swModel, wallNumber: 1, leftBaseLength: leftBase, rightBaseLength: rightBase);
        }
    }
}
