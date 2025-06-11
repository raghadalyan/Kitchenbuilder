using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace Kitchenbuilder.Core
{
    public static class DeleteWall
    {
        private static readonly string LogFilePath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\debug.txt";

        public static void DeleteWallByNumber(string filePath, int wallNumber)
        {
            LogMessage($"🔍 Starting to delete Wall {wallNumber} in file: {filePath}");

            SldWorks swApp = Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application")) as SldWorks;
            if (swApp == null)
            {
                LogMessage("❌ Failed to launch SolidWorks. Please ensure it is installed.");
                return;
            }

            swApp.Visible = true;

            int errors = 0, warnings = 0;
            ModelDoc2 swModel = swApp.OpenDoc6(
                filePath,
                (int)swDocumentTypes_e.swDocPART,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                "",
                ref errors,
                ref warnings) as ModelDoc2;

            if (swModel == null)
            {
                LogMessage($"❌ Failed to open file: {filePath}");
                return;
            }

            var deletedFeatures = new HashSet<string>();
            DeleteWallFeatures(swModel, wallNumber, deletedFeatures);

            // Save the model after deletions
            int saveErrors = 0, saveWarnings = 0;
            swModel.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref saveErrors, ref saveWarnings);

            if (saveErrors == 0)
            {
                LogMessage($"✅ Model saved successfully after deleting Wall {wallNumber}.");
            }
            else
            {
                LogMessage($"❌ Error saving model after deleting Wall {wallNumber}. (ErrorCode: {saveErrors})");
            }

            swApp.CloseDoc(filePath);
        }

        private static void DeleteWallFeatures(ModelDoc2 swModel, int wallNumber, HashSet<string> deletedFeatures)
        {
            var featureNames = new[]
            {
                $"Left_base{wallNumber}",
                $"fridge_base{wallNumber}",
                $"Right_base{wallNumber}",
                $"Fridge{wallNumber}",
                $"master_wall{wallNumber}"
            };

            foreach (var featureName in featureNames)
            {
                // Delete Body-Move/CopyX before fridge features
                if (featureName.Contains("fridge") || featureName.Contains("Fridge"))
                {
                    string moveCopyName = $"Body-Move/Copy{wallNumber}";
                    DeleteFeature(swModel, moveCopyName, deletedFeatures);
                }

                // Delete extrude features
                string extrudeFeatureName = $"Extrude_{featureName}";
                DeleteFeature(swModel, extrudeFeatureName, deletedFeatures);

                // Delete sketches
                DeleteSketch(swModel, featureName, deletedFeatures);

                // Delete inserted fridge part
                if (featureName.StartsWith("Fridge"))
                {
                    DeleteInsertedPart(swModel, featureName, deletedFeatures);
                }
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

        private static void DeleteInsertedPart(ModelDoc2 swModel, string partName, HashSet<string> deletedFeatures)
        {
            Feature feature = (Feature)swModel.FirstFeature();
            bool found = false;

            while (feature != null)
            {
                if (feature.Name.StartsWith(partName))
                {
                    if (deletedFeatures.Contains(feature.Name)) continue;

                    if (feature.Select2(false, -1))
                    {
                        if (swModel.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed))
                        {
                            LogMessage($"🗑️ Deleted inserted part: {feature.Name}");
                            deletedFeatures.Add(feature.Name);
                            found = true;
                        }
                        else
                        {
                            LogMessage($"⚠️ Failed to delete inserted part: {feature.Name}");
                        }
                    }
                    else
                    {
                        LogMessage($"⚠️ Failed to select inserted part: {feature.Name}");
                    }
                }
                feature = (Feature)feature.GetNextFeature();
            }

            if (!found)
            {
                LogMessage($"⚠️ No inserted part found with prefix: {partName}");
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

        private static void LogMessage(string message)
        {
            Console.WriteLine(message);
            File.AppendAllText(LogFilePath, message + System.Environment.NewLine);
        }
    }
}
