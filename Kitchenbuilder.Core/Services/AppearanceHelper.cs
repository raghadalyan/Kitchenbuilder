using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;

public static class AppearanceHelper
{
    public static void ApplyColorToBody(IModelDoc2 model, string bodyName)
    {
        SelectionMgr selMgr = (SelectionMgr)model.SelectionManager;

        // Select the body by name
        bool selected = model.Extension.SelectByID2(
            bodyName,
            "SOLIDBODY",
            0, 0, 0, false, 0, null, 0);

        if (!selected)
        {
            Console.WriteLine($"❌ Could not select body: {bodyName}");
            return;
        }

        // Get the selected body
        object selectedObj = selMgr.GetSelectedObject6(1, -1);
        if (selectedObj is not IBody2 body)
        {
            Console.WriteLine("❌ Failed to cast selected object to IBody2.");
            return;
        }

        double[] appearance = new double[9] { 1.0, 1.0, 1.0, 1.0, 0, 0, 0.1,0.0, 0.5}; //white 

        body.MaterialPropertyValues2 = appearance;
        Console.WriteLine($"✅ Appearance set on body: {bodyName}");

        model.ClearSelection2(true);
    }
}
