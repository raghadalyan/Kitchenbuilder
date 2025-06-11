//using System;
//using System.IO;
//using SolidWorks.Interop.sldworks;
//using SolidWorks.Interop.swconst;

//namespace Kitchenbuilder.Core
//{
//    public static class SldprtSaver
//    {
//        public static void SaveAllViews(SldWorks swApp, string outputFolder)
//        {
//            ModelDoc2 swModel = swApp.ActiveDoc as ModelDoc2;
//            if (swModel == null)
//            {
//                LogMessage("❌ No active document found.");
//                return;
//            }

//            if (swModel.GetType() != (int)swDocumentTypes_e.swDocPART)
//            {
//                LogMessage("❌ The active document is not a .SLDPRT file.");
//                return;
//            }

//            IModelDocExtension swModelDocExt = swModel.Extension;
//            Directory.CreateDirectory(outputFolder);

//            SaveView(swModel, swModelDocExt, outputFolder, "*Top", swStandardViews_e.swTopView, "Top");
//            SaveView(swModel, swModelDocExt, outputFolder, "*Front", swStandardViews_e.swFrontView, "Front");
//            SaveView(swModel, swModelDocExt, outputFolder, "*Right", swStandardViews_e.swRightView, "Right");
//        }

//        private static void SaveView(ModelDoc2 swModel, IModelDocExtension swModelDocExt, string folderPath, string viewNameInSw, swStandardViews_e viewEnum, string imageName)
//        {
//            swModel.ShowNamedView2(viewNameInSw, (int)viewEnum);
//            swModel.ViewZoomtofit2();

//            string imageFilePath = Path.Combine(folderPath, $"{imageName}.png");
//            int saveErrors = 0;
//            int saveWarnings = 0;
//            bool result = swModelDocExt.SaveAs3(
//                imageFilePath,
//                (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
//                (int)swSaveAsOptions_e.swSaveAsOptions_Silent,
//                null,
//                null,
//                ref saveErrors,
//                ref saveWarnings);

//            if (result && File.Exists(imageFilePath))
//            {
//                LogMessage($"✅ Image saved: {imageFilePath}");
//            }
//            else
//            {
//                LogMessage($"❌ Error saving image: {imageFilePath} (ErrorCode: {saveErrors}, Warnings: {saveWarnings})");
//            }
//        }

//        private static void LogMessage(string message)
//        {
//            Console.WriteLine(message);
//            // Optional: Append to log file if you want
//        }
//    }
//}
