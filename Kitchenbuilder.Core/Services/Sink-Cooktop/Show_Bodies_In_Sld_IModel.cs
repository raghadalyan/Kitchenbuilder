using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class Show_Bodies_In_Sld_IModel
    {
        private static readonly string LogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Show_Bodies_In_Sld_IModel_Log.txt";

        private static readonly HashSet<string> ShownBodies = new();

        public static void ShowBody(IModelDoc2 modelDoc, string targetBodyName)
        {
            if (modelDoc is not ModelDoc2 model)
            {
                Log("❌ Cannot cast IModelDoc2 to ModelDoc2.");
                return;
            }

            Feature feature = (Feature)model.FirstFeature();
            bool bodyWasShown = false;

            while (feature != null)
            {
                string featureType = feature.GetTypeName2();

                if (featureType == "SolidBodyFolder" || featureType == "SurfaceBodyFolder")
                {
                    var bodyFolder = feature.GetSpecificFeature2() as IBodyFolder;
                    if (bodyFolder != null)
                    {
                        object[] bodies = bodyFolder.GetBodies() as object[];
                        if (bodies != null)
                        {
                            foreach (object obj in bodies)
                            {
                                var body = obj as IBody2;
                                if (body != null && body.Name == targetBodyName)
                                {
                                    if (!body.Visible)
                                    {
                                        body.HideBody(false); // Show it
                                        ShownBodies.Add(body.Name);
                                        bodyWasShown = true;
                                        Log($"👁️ Made body visible: {body.Name}");
                                    }
                                    else
                                    {
                                        Log($"✅ Body already visible: {body.Name}");
                                    }

                                    return;
                                }
                            }
                        }
                    }
                }

                feature = (Feature)feature.GetNextFeature();
            }

            Log($"❌ Body not found: {targetBodyName}");
        }

        public static void ShowMultipleBodies(IModelDoc2 modelDoc, string[] bodyNames)
        {
            foreach (string body in bodyNames)
            {
                ShowBody(modelDoc, body);
            }

            SaveAfterShowing(modelDoc);
        }

        public static void SaveAfterShowing(IModelDoc2 modelDoc)
        {
            if (modelDoc is not ModelDoc2 model)
                return;

            if (ShownBodies.Count == 0)
            {
                Log("ℹ️ No bodies were shown, skipping save.");
                return;
            }

            model.EditRebuild3();

            int errors = 0, warnings = 0;
            bool saved = model.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref errors, ref warnings);
            Log(saved ? "💾 Model saved after showing bodies." : $"❌ Save failed. Errors: {errors}, Warnings: {warnings}");
        }

        private static void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            File.AppendAllText(LogPath, $"[{timestamp}] {message}{System.Environment.NewLine}");
        }
    }
}
