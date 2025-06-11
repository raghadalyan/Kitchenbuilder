using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace Kitchenbuilder.Core
{
    public static class ImplementOneWallInSld
    {
        private const string SourceFilePath = @"C:\Users\chouse\Downloads\Kitchenbuilder\KitchenParts\base\base with fridge.SLDPRT";
        private const string TempFolderPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\temp";
        private const string OutputFolderPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output";
        private const string LogFilePath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\debug.txt";

        private static readonly string WwwRootOutputFolder = Path.Combine(AppContext.BaseDirectory, "wwwroot", "Output");

        public static void CopyAndOpenFiles(Dictionary<int, string> suggestedDescriptions)
        {
            Directory.CreateDirectory(TempFolderPath);
            Directory.CreateDirectory(OutputFolderPath);
            File.WriteAllText(LogFilePath, $"Run started at {DateTime.Now}{System.Environment.NewLine}");

            SldWorks swApp = Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application")) as SldWorks;
            if (swApp == null)
            {
                LogMessage("❌ Failed to launch SolidWorks. Please ensure it is installed.");
                return;
            }

            swApp.Visible = true;

            foreach (var kvp in suggestedDescriptions.OrderBy(x => x.Key))
            {
                int optionNumber = kvp.Key;
                string description = kvp.Value;
                string destinationFileName = $"base_with_fridge_option_{optionNumber}.SLDPRT";
                string destinationFilePath = Path.Combine(TempFolderPath, destinationFileName);

                try
                {
                    File.Copy(SourceFilePath, destinationFilePath, overwrite: true);
                    LogMessage($"✅ File {destinationFileName} copied successfully.");

                    int errors = 0, warnings = 0;
                    ModelDoc2 swModel = swApp.OpenDoc6(
                        destinationFilePath,
                        (int)swDocumentTypes_e.swDocPART,
                        (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                        "",
                        ref errors,
                        ref warnings) as ModelDoc2;

                    if (swModel != null)
                    {
                        List<int> wallsToKeep = ExtractWallsFromDescription(description);
                        DeleteFeaturesForWalls(swModel, wallsToKeep);

                        string optionFolder = Path.Combine(WwwRootOutputFolder, $"Option{optionNumber}");
                        Directory.CreateDirectory(optionFolder);

                        IModelDocExtension swModelDocExt = swModel.Extension;

                        swModel.ShowNamedView2("*Top", (int)swStandardViews_e.swTopView);
                        swModel.ViewZoomtofit2();
                        SaveImage(swModelDocExt, optionFolder, "Top");

                        swModel.ShowNamedView2("*Front", (int)swStandardViews_e.swFrontView);
                        swModel.ViewZoomtofit2();
                        SaveImage(swModelDocExt, optionFolder, "Front");

                        swModel.ShowNamedView2("*Right", (int)swStandardViews_e.swRightView);
                        swModel.ViewZoomtofit2();
                        SaveImage(swModelDocExt, optionFolder, "Right");

                        int saveErrors = 0, saveWarnings = 0;
                        swModel.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref saveErrors, ref saveWarnings);
                        swApp.CloseDoc(destinationFileName);
                    }
                    else
                    {
                        LogMessage($"❌ Error opening file: {destinationFileName}");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"❌ Error copying/opening: {ex.Message}");
                }
            }
        }

        private static List<int> ExtractWallsFromDescription(string description)
        {
            return Regex.Matches(description, @"Wall\s*(\d)")
                .Select(m => int.Parse(m.Groups[1].Value))
                .Distinct()
                .ToList();
        }

        private static void DeleteFeaturesForWalls(ModelDoc2 swModel, List<int> wallsToKeep)
        {
            var wallFeatures = new Dictionary<int, string[]>
{
    { 2, new[] { "Left_base2", "fridge_base2", "Right_base2", "Fridge2", "master_wall2" } },
    { 3, new[] { "Left_base3", "fridge_base3", "Right_base3", "Fridge3", "master_wall3" } },
    { 4, new[] { "Left_base4", "fridge_base4", "Right_base4", "Fridge4", "master_wall4" } }
};



            // Determine which walls to delete
            var wallsToDelete = Enumerable.Range(1, 4).Except(wallsToKeep).OrderBy(w => w).ToList();

            LogMessage($"🗑️ Walls to delete: {string.Join(", ", wallsToDelete)}");

            var deletedFeatures = new HashSet<string>();

            foreach (var wall in wallsToDelete)
            {
                if (wallFeatures.TryGetValue(wall, out var features))
                {
                    foreach (var featureName in features)
                    {
                        string wallNumber = Regex.Match(featureName, @"\d+").Value;

                        // Delete Body-Move/CopyX before fridge features
                        if (featureName.Contains("fridge") || featureName.Contains("Fridge"))
                        {
                            string moveCopyName = $"Body-Move/Copy{wallNumber}";
                            DeleteFeature(swModel, moveCopyName, deletedFeatures);
                        }

                        DeleteFeature(swModel, $"Extrude_{featureName}", deletedFeatures);
                        DeleteFeature(swModel, featureName, deletedFeatures);

                        if (featureName.StartsWith("Left_base") || featureName.StartsWith("fridge_base") || featureName.StartsWith("Right_base"))
                        {
                            DeleteSketch(swModel, featureName, deletedFeatures);
                        }
                    }
                }
            }

            // Save the model after deletions
            int saveErrors = 0, saveWarnings = 0;
            swModel.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref saveErrors, ref saveWarnings);

            if (saveErrors == 0)
            {
                LogMessage("✅ Model saved successfully after deleting walls.");
            }
            else
            {
                LogMessage($"❌ Error saving model after deleting walls. (ErrorCode: {saveErrors})");
            }
        }

        private static void DeleteFeature(ModelDoc2 swModel, string featureName, HashSet<string> deletedFeatures)
        {
            if (deletedFeatures.Contains(featureName)) return;

            Feature feature = FindFeatureByName(swModel, featureName);
            if (feature != null)
            {
                if (feature.Select2(false, -1))
                {
                    if (swModel.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed))
                    {
                        LogMessage($"🗑️ Deleted feature: {featureName}");
                        deletedFeatures.Add(featureName);
                    }
                    else
                    {
                        LogMessage($"⚠️ Failed to delete feature: {featureName}");
                    }
                }
                else
                {
                    LogMessage($"⚠️ Failed to select feature: {featureName}");
                }
            }
            else
            {
                LogMessage($"⚠️ Feature not found: {featureName}");
            }
        }

        private static void DeleteSketch(ModelDoc2 swModel, string sketchName, HashSet<string> deletedFeatures)
        {
            if (deletedFeatures.Contains(sketchName)) return;

            bool selected = swModel.Extension.SelectByID2(sketchName, "SKETCH", 0, 0, 0, false, 0, null, 0);
            if (selected)
            {
                if (swModel.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed))
                {
                    LogMessage($"🗑️ Deleted sketch: {sketchName}");
                    deletedFeatures.Add(sketchName);
                }
                else
                {
                    LogMessage($"⚠️ Failed to delete sketch: {sketchName}");
                }
            }
            else
            {
                LogMessage($"⚠️ Sketch not found: {sketchName}");
            }
        }

        private static Feature FindFeatureByName(ModelDoc2 swModel, string featureName)
        {
            Feature feature = (Feature)swModel.FirstFeature();
            while (feature != null)
            {
                if (feature.Name == featureName)
                    return feature;
                feature = (Feature)feature.GetNextFeature();
            }
            return null;
        }



        private static void SaveImage(IModelDocExtension swModelDocExt, string folder, string imageName)
        {
            string imagePath = Path.Combine(folder, $"{imageName}.png");

            int saveErrors = 0, saveWarnings = 0;
            bool result = swModelDocExt.SaveAs3(
                imagePath,
                (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
                null, null, ref saveErrors, ref saveWarnings);

            if (result && File.Exists(imagePath))
            {
                LogMessage($"✅ Image saved: {imagePath}");
            }
            else
            {
                LogMessage($"❌ Error saving image: {imagePath} (ErrorCode: {saveErrors}, Warnings: {saveWarnings})");
            }
        }


        private static void LogMessage(string message)
        {
            Console.WriteLine(message);
            File.AppendAllText(LogFilePath, message + System.Environment.NewLine);
            File.AppendAllText(Path.Combine(OutputFolderPath, "delete.txt"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{System.Environment.NewLine}");
        }
    }
}
