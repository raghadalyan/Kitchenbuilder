using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static readonly string WwwRootOutputFolder = Path.Combine(
    AppContext.BaseDirectory,
    "wwwroot",
    "Output");

        public static void CopyAndOpenFiles(Dictionary<int, string> suggestedDescriptions)
        {
            Directory.CreateDirectory(TempFolderPath);
            Directory.CreateDirectory(OutputFolderPath);

            // Clear or create log file
            File.WriteAllText(LogFilePath, $"Run started at {DateTime.Now}{System.Environment.NewLine}");

            List<string> openedFiles = new List<string>();

            // Start SolidWorks application
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
                string destinationFileName = $"base_with_fridge_option_{optionNumber}.SLDPRT";
                string destinationFilePath = Path.Combine(TempFolderPath, destinationFileName);

                try
                {
                    File.Copy(SourceFilePath, destinationFilePath, overwrite: true);
                    LogMessage($"✅ File {destinationFileName} copied successfully.");

                    // Open the copied file
                    int errors = 0;
                    int warnings = 0;
                    ModelDoc2 swModel = swApp.OpenDoc6(
                        destinationFilePath,
                        (int)swDocumentTypes_e.swDocPART,
                        (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                        "",
                        ref errors,
                        ref warnings);

                    if (swModel != null)
                    {
                        // Create output folder for this option
                        string optionFolder = Path.Combine(WwwRootOutputFolder, $"Option{optionNumber}");
                        Directory.CreateDirectory(optionFolder);

                        IModelDocExtension swModelDocExt = swModel.Extension;

                        // Save Top view
                        swModel.ShowNamedView2("*Top", (int)swStandardViews_e.swTopView);
                        swModel.ViewZoomtofit2();
                        SaveImage(swModelDocExt, optionFolder, "Top");

                        // Save Front view
                        swModel.ShowNamedView2("*Front", (int)swStandardViews_e.swFrontView);
                        swModel.ViewZoomtofit2();
                        SaveImage(swModelDocExt, optionFolder, "Front");

                        // Save Right view
                        swModel.ShowNamedView2("*Right", (int)swStandardViews_e.swRightView);
                        swModel.ViewZoomtofit2();
                        SaveImage(swModelDocExt, optionFolder, "Right");

                        openedFiles.Add(destinationFilePath);
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

        private static void SaveImage(IModelDocExtension swModelDocExt, string folderPath, string viewName)
        {
            string imageFilePath = Path.Combine(folderPath, $"{viewName}.png");
            int saveErrors = 0;
            int saveWarnings = 0;
            bool result = swModelDocExt.SaveAs3(
                imageFilePath,
                (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
                null,
                null,
                ref saveErrors,
                ref saveWarnings);

            if (result && File.Exists(imageFilePath))
            {
                LogMessage($"✅ Image saved: {imageFilePath}");
            }
            else
            {
                LogMessage($"❌ Error saving image: {imageFilePath} (ErrorCode: {saveErrors}, Warnings: {saveWarnings})");
            }
        }

        private static void LogMessage(string message)
        {
            Console.WriteLine(message);
            File.AppendAllText(LogFilePath, message + System.Environment.NewLine);
        }
    }
}
