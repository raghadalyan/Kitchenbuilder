using SolidWorks.Interop.sldworks;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class SaveOptionModel
    {
        private static readonly string BaseOutputPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "Output");
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\SaveOptionModel_Debug.txt";

        public static void Save(ModelDoc2 model, string jsonPath)
        {
            try
            {
                File.AppendAllText(DebugPath, $"🧩 Received jsonPath: {jsonPath}{System.Environment.NewLine}");

                if (string.IsNullOrWhiteSpace(jsonPath))
                {
                    File.AppendAllText(DebugPath, $"❌ Invalid jsonPath. Skipping folder creation.{System.Environment.NewLine}");
                    return;
                }

                string optionName = Path.GetFileNameWithoutExtension(jsonPath);
                if (optionName.EndsWith("SLD"))
                    optionName = optionName.Substring(0, optionName.Length - 3);

                File.AppendAllText(DebugPath, $"📁 Final folder name: {optionName}{System.Environment.NewLine}");

                string folderPath = Path.Combine(BaseOutputPath, optionName);

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    File.AppendAllText(DebugPath, $"✅ Created folder: {folderPath}{System.Environment.NewLine}");
                }
                else
                {
                    File.AppendAllText(DebugPath, $"ℹ️ Folder already exists: {folderPath}{System.Environment.NewLine}");
                }

                File.AppendAllText(DebugPath, $"📸 Calling SaveImgs.Save...{System.Environment.NewLine}");
                SaveImgs.Save(model, folderPath);
            }
            catch (Exception ex)
            {
                File.AppendAllText(DebugPath, $"❌ Exception: {ex.Message}{System.Environment.NewLine}");
            }
        }
    }
}
