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

                    // Determine walls to keep
                    List<int> wallsToKeep = ExtractWallsFromDescription(description);

                    // Determine walls to delete
                    var wallsToDelete = Enumerable.Range(1, 4).Except(wallsToKeep).ToList();

                    // Delete unwanted walls
                    foreach (var wall in wallsToDelete)
                    {
                        DeleteWall.DeleteWallByNumber(destinationFilePath, wall);
                    }

                    // Save screenshots
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
