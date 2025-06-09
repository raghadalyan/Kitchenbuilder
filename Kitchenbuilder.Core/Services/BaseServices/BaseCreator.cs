using Kitchenbuilder.Core.Models;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.IO;

namespace Kitchenbuilder.Core.Services
{
    public static class BaseCreator
    {
        public static Kitchenbuilder.Core.Models.Base CreateBase(Kitchen kitchen)
        {
            try
            {
                if (kitchen.Floor.Width <= 0 || kitchen.Floor.Length <= 0)
                    throw new InvalidOperationException("Floor width and length must be greater than 0.");

                double floorWidth = kitchen.Floor.Width;
                double floorLength = kitchen.Floor.Length;

                WriteDebugLog($"[CreateBase] Floor Width: {floorWidth} cm, Floor Length: {floorLength} cm");

                // Step 1: Copy the SLDPRT file
                string userDownloads = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads");
                string sourcePath = Path.Combine(userDownloads, "Kitchenbuilder", "KitchenParts", "base", "base with fridge.SLDPRT");
                string destFolder = Path.Combine(userDownloads, "Kitchenbuilder", "Output");
                Directory.CreateDirectory(destFolder);
                string destPath = Path.Combine(destFolder, "base_with_fridge_edited.SLDPRT");

                WriteDebugLog($"[CreateBase] Copying file from: {sourcePath}");
                File.Copy(sourcePath, destPath, true);

                // Step 2: Open the file in SolidWorks
                SldWorks swApp = new SldWorks();
                WriteDebugLog($"[CreateBase] Opening file in SolidWorks: {destPath}");
                ModelDoc2 swModel = (ModelDoc2)swApp.OpenDoc(destPath, (int)swDocumentTypes_e.swDocPART);

                if (swModel == null)
                    throw new InvalidOperationException($"Failed to open SolidWorks file: {destPath}");
                swModel.Visible = true;  // Makes the model visible
                swApp.ActivateDoc2(swModel.GetTitle(), false, 0);

                // Step 3: Edit dimensions
                EditFloorMeasures(swModel, floorWidth, floorLength);

                // Step 4: Analyze empty spaces using BaseAnalyzer
                var emptySpaces = BaseAnalyzer.AnalyzeEmptySpaces(kitchen);
                WriteDebugLog("[CreateBase] BaseAnalyzer completed. Empty space details saved.");

                // Step 5: Leave SolidWorks open
                WriteDebugLog($"[CreateBase] File updated and left open for user review.");

                // Step 6: Return Base object
                return new Kitchenbuilder.Core.Models.Base
                {
                    Width1 = floorWidth,
                    Width2 = floorLength,
                    FileName = "base_with_fridge_edited.SLDPRT"
                };
            }
            catch (Exception ex)
            {
                WriteDebugLog($"❌ Error: {ex.Message}");
                throw;
            }
        }

        private static void EditFloorMeasures(ModelDoc2 swModel, double floorWidth, double floorLength)
        {
            try
            {
                // Get existing dimensions
                Dimension widthDim = (Dimension)swModel.Parameter("width@master_wall1");
                Dimension lengthDim = (Dimension)swModel.Parameter("length@master_wall2");

                WriteDebugLog($"[EditFloorMeasures] Before Edit - width@master_wall1 = {widthDim?.SystemValue * 1000.0} mm");
                WriteDebugLog($"[EditFloorMeasures] Before Edit - length@master_wall2 = {lengthDim?.SystemValue * 1000.0} mm");

                // Apply new values (convert cm to meters)
                if (widthDim != null)
                {
                    widthDim.SystemValue = floorWidth / 100.0; // cm to meters
                    WriteDebugLog($"[EditFloorMeasures] Updated width@master_wall1 to {floorWidth} cm");
                }
                else
                {
                    WriteDebugLog("⚠️ width@master_wall1 not found.");
                }

                if (lengthDim != null)
                {
                    lengthDim.SystemValue = floorLength / 100.0; // cm to meters
                    WriteDebugLog($"[EditFloorMeasures] Updated length@master_wall2 to {floorLength} cm");
                }
                else
                {
                    WriteDebugLog("⚠️ length@master_wall2 not found.");
                }

                swModel.EditRebuild3();
                swModel.ViewZoomtofit2();
                swModel.Save();
                WriteDebugLog($"[EditFloorMeasures] Changes saved successfully.");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"❌ Error in EditFloorMeasures: {ex.Message}");
                throw;
            }
        }

        private static void WriteDebugLog(string message)
        {
            try
            {
                string userDownloads = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Downloads");
                string debugPath = Path.Combine(userDownloads, "Kitchenbuilder", "Output", "debug.txt");

                using (StreamWriter writer = new StreamWriter(debugPath, true))
                {
                    writer.WriteLine($"[{DateTime.Now}] {message}");
                }
            }
            catch
            {
                // In case writing to the file fails, just skip logging.
            }
        }
    }
}
