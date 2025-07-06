using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Runtime.InteropServices;

public class SolidWorksMaterialEditor
{
    public void ApplyMaterialToPart()
    {
        try
        {
            // Connect to SolidWorks
            Type swType = Type.GetTypeFromProgID("SldWorks.Application");
            SldWorks swApp = (SldWorks)Activator.CreateInstance(swType);

            // Ensure SolidWorks is visible
            swApp.Visible = true;

            // File path
            string filePath = @"C:\Users\chouse\OneDrive - Azrieli - Jerusalem College of Engineering\Desktop\Part1.SLDPRT";
            string materialName = "Maple";
            string materialPath = swApp.GetExecutablePath() + @"\lang\english\solidworks materials\solidworks materials.sldmat";

            // Open part
            int errors = 0, warnings = 0;
            ModelDoc2 swModel = swApp.OpenDoc6(filePath, (int)swDocumentTypes_e.swDocPART,
                                               (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref errors, ref warnings);

            if (swModel == null)
            {
                Console.WriteLine("❌ Failed to open file.");
                return;
            }

            // Cast to Part
            PartDoc swPart = (PartDoc)swModel;

            // Apply material
        swPart.SetMaterialPropertyName2("", materialPath, materialName);

            // Force update + repaint
            swModel.ForceRebuild3(false);  // Rebuild the model
            swModel.GraphicsRedraw2();     // Redraw graphics
            swModel.ViewZoomtofit2();      // Optional: Zoom to fit

            // Optional: Save the document
            swModel.Save();

        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Exception: " + ex.Message);
        }
    }
}
