using Kitchenbuilder.Core.Models;
using Kitchenbuilder.Core.Services.BaseServices.SLD_IMPLEMENTAION;
using SolidWorks.Interop.sldworks;
using System;
using System.IO;
using SolidWorks.Interop.swconst;

namespace Kitchenbuilder.Core
{
    public static class JsonReader
    {
        private static readonly string KitchenFolder = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Kitchen\";
        private static readonly string TempFolder = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\temp\";
        private static readonly string LogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\ImplementInSld.txt";

        private static string GetWallPartPath()
        {
            try
            {
                var files = Directory.GetFiles(KitchenFolder, "Wall*.SLDPRT");
                if (files.Length == 0)
                {
                    File.AppendAllText(LogPath, $"❌ No file starting with 'Wall' found in {KitchenFolder}\n");
                    return null;
                }

                File.AppendAllText(LogPath, $"✅ Found wall part file: {files[0]}\n");
                return files[0]; // Return the first match
            }
            catch (Exception ex)
            {
                File.AppendAllText(LogPath, $"❌ Error locating wall part file: {ex.Message}\n");
                return null;
            }
        }

        public static void ProcessJson(Kitchen kitchen, string jsonPath)
        {
            try
            {
                File.AppendAllText(LogPath, $"\n************** JsonReader ***************\n");

                if (!Directory.Exists(TempFolder))
                    Directory.CreateDirectory(TempFolder);

                // Extract base name (e.g., Option1 from Option1SLD.json)
                string baseName = Path.GetFileNameWithoutExtension(jsonPath).Replace("SLD", "");
                string copiedPath = Path.Combine(TempFolder, $"temp_{baseName}.SLDPRT");

                string sourcePath = GetWallPartPath();
                if (string.IsNullOrEmpty(sourcePath))
                {
                    File.AppendAllText(LogPath, $"❌ Source part file not found. Aborting.\n");
                    return;
                }

                File.Copy(sourcePath, copiedPath, overwrite: true);
                File.AppendAllText(LogPath, $"📁 Copied part from {sourcePath} to {copiedPath}\n");

                SldWorks swApp = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
                swApp.Visible = true;

                ModelDoc2 model = (ModelDoc2)swApp.OpenDoc(copiedPath, (int)swDocumentTypes_e.swDocPART);
                if (model == null)
                {
                    File.AppendAllText(LogPath, "❌ Failed to open the SolidWorks part.\n");
                    return;
                }

                File.AppendAllText(LogPath, "✅ Part opened successfully in SolidWorks.\n");

                // Create the floor
                CreateFloor_Base.Create(kitchen, model, jsonPath);

                // Make bases visible
                CheckVisibleElements.ProcessVisibleBases(jsonPath, model);

                // Apply smart dimensions
                ApplySmartDims.ApplyDimensions(jsonPath, model);

                // Save the updated model
                SaveOptionModel.Save(model, jsonPath);

                string modelTitle = model.GetTitle();
                swApp.CloseDoc(modelTitle);
                File.AppendAllText(LogPath, $"📁 Closed document {modelTitle}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(LogPath, $"❌ Error in JsonReader.ProcessJson: {ex.Message}\n");
            }
        }
    }
}
