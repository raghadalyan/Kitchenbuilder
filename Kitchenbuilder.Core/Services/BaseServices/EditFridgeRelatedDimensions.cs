using SolidWorks.Interop.sldworks;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class EditFridgeRelatedDimensions
    {
        private const string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\edit base debug.txt";

        public static void Edit(ModelDoc2 swModel, int wallNumber, double leftBaseLength, double rightBaseLength)
        {
            string leftDimName = $"length_L_base@fridge_base{wallNumber}";
            string rightDimName = $"length_R_base@fridge_base{wallNumber}";

            // Edit Left Base
            var leftDim = swModel.Parameter(leftDimName) as Dimension;
            if (leftDim != null)
            {
                leftDim.SystemValue = leftBaseLength / 100.0;
                File.AppendAllText(DebugPath, $"✏️ Set {leftDimName} = {leftBaseLength} cm\n");
            }
            else
            {
                File.AppendAllText(DebugPath, $"❌ Could not find dimension: {leftDimName}\n");
            }

            // Edit Right Base
            var rightDim = swModel.Parameter(rightDimName) as Dimension;
            if (rightDim != null)
            {
                rightDim.SystemValue = rightBaseLength / 100.0;
                File.AppendAllText(DebugPath, $"✏️ Set {rightDimName} = {rightBaseLength} cm\n");
            }
            else
            {
                File.AppendAllText(DebugPath, $"❌ Could not find dimension: {rightDimName}\n");
            }
        }
    }
}
