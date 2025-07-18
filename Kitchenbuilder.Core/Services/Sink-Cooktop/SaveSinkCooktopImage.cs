using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace Kitchenbuilder.Core
{
    public static class SaveSinkCooktopImage
    {
        private static readonly string DebugPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Output", "Sink-Cooktop", "SaveSinkCooktopImage_Debug.txt"
        );

        public static void Save(IModelDoc2 model, int layoutIndex, string description, int optionNum)
        {
            try
            {
                if (model == null)
                {
                    File.AppendAllText(DebugPath, "❌ Null model passed to Save.\n");
                    return;
                }

                // ✅ Save images to wwwroot
                string imageFolder = Path.Combine(
                    AppContext.BaseDirectory,
                    "wwwroot", "Output", "Sink_Cooktop", $"Option{layoutIndex}"
                );
                Directory.CreateDirectory(imageFolder);

                var swModelDocExt = model.Extension;

                SaveView(model, swModelDocExt, "*Top", (int)swStandardViews_e.swTopView, imageFolder, "Top.png");
                SaveView(model, swModelDocExt, "*Front", (int)swStandardViews_e.swFrontView, imageFolder, "Front.png");
                SaveView(model, swModelDocExt, "*Right", (int)swStandardViews_e.swRightView, imageFolder, "Right.png");
                SaveView(model, swModelDocExt, "*Isometric", (int)swStandardViews_e.swIsometricView, imageFolder, "Isometric.png");

                File.AppendAllText(DebugPath, $"✅ Saved views for layout Option{layoutIndex}, Description: {description}\n");

                // ✅ Save SLDPRT copy to specific layout path
                string layoutFolder = Path.Combine(
                    @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\temp\Layout"
                );
                Directory.CreateDirectory(layoutFolder);

                string sldPath = Path.Combine(layoutFolder, $"Layout{layoutIndex}.SLDPRT");
                int saveErrors = 0, saveWarnings = 0;

                bool result = swModelDocExt.SaveAs(
                    sldPath,
                    (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                    (int)swSaveAsOptions_e.swSaveAsOptions_Copy,
                    null, ref saveErrors, ref saveWarnings
                );

                string sldResult = result ? "✅" : "❌";
                File.AppendAllText(DebugPath, $"{sldResult} Saved model → {sldPath}, errors={saveErrors}, warnings={saveWarnings}\n");
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

                string path = Path.Combine(folderPath, fileLabel);
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
