using Kitchenbuilder.Core.Models;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class BaseCreator
    {
        public static Base CreateBase(Kitchen kitchen)
        {
            if (kitchen.Walls.Count < 2)
                throw new InvalidOperationException("Need 2 walls to create a base.");

            double width1 = kitchen.Walls[0].Width;
            double width2 = kitchen.Walls[1].Width;

            Console.WriteLine($"[CreateBase] Wall1 Width: {width1} mm, Wall2 Width: {width2} mm");

            // Step 1: Copy and modify the SLDPRT file
            ModifyBasePart(width1, width2);

            // Step 2: Return the Base object with the saved file name
            return new Base
            {
                Width1 = width1,
                Width2 = width2,
                FileName = "Left_L_Base_Edited.SLDPRT"
            };
        }

        private static void ModifyBasePart(double width1, double width2)
        {
            string sourcePath = @"C:\Users\Asus\Desktop\Raghad\Kitchenbuilder\KitchenParts\cabinets\Left_L_Base.SLDPRT";
            string destFolder = @"C:\Users\Asus\Desktop\Kitchen";
            string destPath = Path.Combine(destFolder, "Left_L_Base_Edited.SLDPRT");

            Console.WriteLine($"[ModifyBasePart] Copying file from: {sourcePath}");
            Directory.CreateDirectory(destFolder);
            File.Copy(sourcePath, destPath, true);

            SldWorks swApp = new SldWorks();
            Console.WriteLine($"[ModifyBasePart] Opening file in SolidWorks: {destPath}");
            ModelDoc2 swModel = (ModelDoc2)swApp.OpenDoc(destPath, (int)swDocumentTypes_e.swDocPART);

            // Get existing dimensions
            Dimension dimD1 = (Dimension)swModel.Parameter("D1@Sketch1");
            Dimension dimD2 = (Dimension)swModel.Parameter("D2@Sketch1");

            Console.WriteLine($"[Before Edit] D1 (Wall2) = {dimD1.SystemValue * 1000.0} mm");
            Console.WriteLine($"[Before Edit] D2 (Wall1) = {dimD2.SystemValue * 1000.0} mm");

            // Apply new values
            dimD1.SystemValue = width2 / 100.0;
            dimD2.SystemValue = width1 / 100.0;

            Console.WriteLine($"[After Edit] D1 = {width2} mm, D2 = {width1} mm");

            swModel.EditRebuild3();
            swModel.Save();
            swApp.CloseDoc(destPath);
            Console.WriteLine($"[ModifyBasePart] Saved and closed: {destPath}");
        }
    }
}
