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
        private static readonly string SourcePartPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\KitchenParts\base\base with fridge.SLDPRT";
        private static readonly string TempFolder = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\temp\";
        private static readonly string LogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\ImplementInSld.txt";

        public static void ProcessJson(Kitchen kitchen, string jsonPath)
        {
            try
            {
                File.AppendAllText(LogPath, $"\n************** JsonReader ***************\n");

                if (!Directory.Exists(TempFolder))
                    Directory.CreateDirectory(TempFolder);

                // Extract readable base name (e.g., Option1 from Option1SLD.json)
                string baseName = Path.GetFileNameWithoutExtension(jsonPath).Replace("SLD", "");
                string copiedPath = Path.Combine(TempFolder, $"temp_{baseName}.SLDPRT");

                File.Copy(SourcePartPath, copiedPath, overwrite: true);
                File.AppendAllText(LogPath, $"📁 Copied part to {copiedPath}\n");

                SldWorks swApp = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
                swApp.Visible = true;

                ModelDoc2 model = (ModelDoc2)swApp.OpenDoc(copiedPath, (int)swDocumentTypes_e.swDocPART);
                if (model == null)
                {
                    File.AppendAllText(LogPath, "❌ Failed to open the SolidWorks part.\n");
                    return;
                }

                File.AppendAllText(LogPath, "✅ Part opened successfully in SolidWorks.\n");

                //Create the floor 
                CreateFloor_Base.Create(kitchen, model, jsonPath);

                //Visible Elements
                CheckVisibleElements.ProcessVisibleBases(jsonPath, model);

                ApplySmartDims.ApplyDimensions(jsonPath, model);


                //string modelTitle = model.GetTitle();
                //swApp.CloseDoc(modelTitle);
                //File.AppendAllText(LogPath, $"📁 Closed document {modelTitle}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(LogPath, $"❌ Error in JsonReader.ProcessJson: {ex.Message}\n");
            }
        }
    }
}
