using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.IO;

public static class DrawingCreator
{
    private static readonly string DebugPath =
        @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Drawing\DrawingCreator_Debug.txt";

    private static void Log(string message)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
        File.AppendAllText(DebugPath, $"[{System.DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}");
    }

    public static void CreateDrawingFromModel(ISldWorks swApp, IModelDoc2 partModel, string dimensionName)
    {
        Log("🛠️ Starting drawing creation...");

        string drawingTemplate = swApp.GetUserPreferenceStringValue(
            (int)swUserPreferenceStringValue_e.swDefaultTemplateDrawing);

        var drawingDoc = (IModelDoc2)swApp.NewDocument(drawingTemplate, 0, 0, 0);
        if (drawingDoc == null)
        {
            Log("❌ Failed to create drawing.");
            return;
        }

        var drawing = (IDrawingDoc)drawingDoc;
        string partPath = partModel.GetPathName();

        // ✅ FIXED: Correct number of arguments (5)
        double x = 0.3;
        double y = 0.2;
        IView insertedView = drawing.CreateDrawViewFromModelView3(partPath, "*Front", x, y, 0);

        if (insertedView != null)
        {
            insertedView.ScaleDecimal = 0.02;
            Log("✅ Front view inserted and scaled to 2%.");
        }
        else
        {
            Log("❌ Failed to insert Front view.");
        }

        bool selected = partModel.Extension.SelectByID2(
            "Sketch_Cabinet2_1", "SKETCH", 0, 0, 0, false, 0, null, 0);

        if (selected)
        {
            drawingDoc.ShowFeatureDimensions();
            Log($"✅ Dimensions from 'Sketch_Cabinet2_1' shown.");
        }
        else
        {
            Log("⚠️ Sketch_Cabinet2_1 not found or could not be selected.");
        }

        Log("✅ Drawing generation complete.");
    }

}
