using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class EditBaseSize
    {
        private const string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\edit base debug.txt";

        /// <summary>
        /// Edits a specific dimension in the SolidWorks model.
        /// </summary>
        /// <param name="swModel">The opened SolidWorks model.</param>
        /// <param name="wallNumber">The wall number associated with the base.</param>
        /// <param name="dimensionBaseName">The base name of the dimension to edit (e.g., "length@Left_base").</param>
        /// <param name="newLength">The new size in centimeters.</param>
        public static void Edit(ModelDoc2 swModel, int wallNumber, string dimensionBaseName, double newLength)
        {
            string fullDimName = $"{dimensionBaseName}{wallNumber}"; // e.g., length@Left_base2
            var dim = swModel.Parameter(fullDimName) as Dimension;

            if (dim != null)
            {
                dim.SystemValue = newLength / 100.0;
                File.AppendAllText(DebugPath, $"✏️ Set {fullDimName} = {newLength} cm\n");
            }
            else
            {
                File.AppendAllText(DebugPath, $"❌ Could not find dimension: {fullDimName}\n");
            }
        }
    }
}
