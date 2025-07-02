using SolidWorks.Interop.sldworks;
using System;

namespace Kitchenbuilder.Core
{
    public static class CountertopDimensionApplier
    {
        public static void ApplyDistance(IModelDoc2 model, string sketchName, double? left, double? right, int wallNumber, Action<string> Log)
        {
            if (model == null || string.IsNullOrEmpty(sketchName))
            {
                Log("❌ Invalid model or sketch name.");
                return;
            }

            string fullSketchName = $"CT_{sketchName}";

            if (left.HasValue)
            {
                double valueToSet = (wallNumber == 1 || wallNumber == 3) ? -left.Value : left.Value;
                string leftDim = $"L@{fullSketchName}";
                bool success = SetDimension(model, leftDim, valueToSet);
                Log(success ? $"✅ Set {leftDim} to {valueToSet}" : $"❌ Failed to set {leftDim}");
            }

            if (right.HasValue)
            {
                double valueToSet = (wallNumber == 2 ) ? -right.Value : right.Value;
                string rightDim = $"R@{fullSketchName}";
                bool success = SetDimension(model, rightDim, valueToSet);
                Log(success ? $"✅ Set {rightDim} to {valueToSet}" : $"❌ Failed to set {rightDim}");
            }

            model.EditRebuild3();
        }

        private static bool SetDimension(IModelDoc2 model, string dimName, double value)
        {
            var dimension = model.Parameter(dimName) as IDimension;
            if (dimension == null)
                return false;

            dimension.SystemValue = value / 100.0; // convert from cm to meters
            return true;
        }

    }
}
