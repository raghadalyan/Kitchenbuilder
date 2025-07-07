using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace Kitchenbuilder.Core
{
    public static class SaveSinkCooktopImage
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Sink-Cooktop\SaveSinkCooktopImage_Debug.txt";

        public static void Save(IModelDoc2 model, int layoutIndex, string description, int optionNum)
        {
            try
            {
                if (model == null)
                {
                    File.AppendAllText(DebugPath, "❌ Null model passed to Save.\n");
                    return;
                }

                string folderPath = Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                    "Downloads", "Kitchenbuilder", "Output", "Sink-Cooktop",
                    $"Option{layoutIndex}");

                Directory.CreateDirectory(folderPath);

                var swModelDocExt = model.Extension;

                SaveView(model, swModelDocExt, "*Top", (int)swStandardViews_e.swTopView, folderPath,  "Top.png");
                SaveView(model, swModelDocExt, "*Front", (int)swStandardViews_e.swFrontView, folderPath, "Front.png");
                SaveView(model, swModelDocExt, "*Right", (int)swStandardViews_e.swRightView, folderPath,  "Right.png");
                SaveView(model, swModelDocExt, "*Isometric", (int)swStandardViews_e.swIsometricView, folderPath,  "Isometric.png");

                File.AppendAllText(DebugPath, $"✅ Saved views for layout Option{layoutIndex} (based on Option{optionNum}), Description: {description}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(DebugPath, $"❌ Exception in Save: {ex.Message}\n");
            }
        }


        private static void SaveView(IModelDoc2 model, ModelDocExtension ext, string viewName, int viewType, string folderPath, string fileLabel)
        {
            try
            {
                model.ShowNamedView2(viewName, viewType);
                model.ViewZoomtofit2();

                string fileName = $"{fileLabel}";
                string path = Path.Combine(folderPath, fileName);
                int saveErrors = 0, saveWarnings = 0;

                bool ok = ext.SaveAs(path,
                    (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                    (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
                    null, ref saveErrors, ref saveWarnings);

                string result = ok ? "✅" : "❌";
                File.AppendAllText(DebugPath, $"{result} Saved {fileLabel} → {path}, errors={saveErrors}, warnings={saveWarnings}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(DebugPath, $"❌ Error saving {fileLabel}: {ex.Message}\n");
            }
        }
    }
}
