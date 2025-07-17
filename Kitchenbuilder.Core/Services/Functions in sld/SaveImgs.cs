using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class SaveImgs
    {
        private static readonly string DebugPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Output", "SaveImgs_Debug.txt"
        );

        public static void Save(ModelDoc2 model, string folderPath)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(folderPath))
                {
                    File.AppendAllText(DebugPath, "❌ Invalid model or folder path.\n");
                    return;
                }

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                var swModelDocExt = model.Extension;

                SaveViewImage(model, swModelDocExt, "*Top", (int)swStandardViews_e.swTopView, folderPath, "Top.png");
                SaveViewImage(model, swModelDocExt, "*Front", (int)swStandardViews_e.swFrontView, folderPath, "Front.png");
                SaveViewImage(model, swModelDocExt, "*Right", (int)swStandardViews_e.swRightView, folderPath, "Right.png");
                SaveViewImage(model, swModelDocExt, "*Isometric", (int)swStandardViews_e.swIsometricView, folderPath, "Isometric.png");

                File.AppendAllText(DebugPath, $"✅ Saved Top, Front, Right, Isometric views to: {folderPath}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(DebugPath, $"❌ Exception: {ex.Message}\n");
            }
        }


        private static void SaveViewImage(ModelDoc2 model, ModelDocExtension swModelDocExt, string viewName, int viewType, string folderPath, string fileLabel)
        {
            try
            {
                model.ShowNamedView2(viewName, viewType);
                model.ViewZoomtofit2();

                string filePath = Path.Combine(folderPath, $"{fileLabel}.png");

                int saveErrors = 0;
                int saveWarnings = 0;

                bool status = swModelDocExt.SaveAs(filePath,
                                                   (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                                                   (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
                                                   null,
                                                   ref saveErrors,
                                                   ref saveWarnings);

                string result = status ? "✅" : "❌";
                File.AppendAllText(DebugPath, $"{result} Saved view '{fileLabel}' → {filePath}, errors={saveErrors}, warnings={saveWarnings}\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText(DebugPath, $"❌ Error saving '{fileLabel}' view: {ex.Message}\n");
            }
        }

    }
}
