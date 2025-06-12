using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace Kitchenbuilder.Core.WallBuilders
{
    public static class WindowDoorInserter
    {
        private static double ToMeters(double cm) => cm / 100.0;

        public static void SetWindow(ModelDoc2 swModel, int index, int sketchNumber, double width, double height, double verticalOffset, double horizontalOffset)
        {
            if (index < 1 || index > 3)
            {
                Console.WriteLine($"❌ Invalid window index: {index}");
                return;
            }

            try
            {
                ((Dimension)swModel.Parameter($"Window{index}Display@Sketch{sketchNumber}")).SystemValue = ToMeters(width);
                ((Dimension)swModel.Parameter($"Window{index}Length@Sketch{sketchNumber}")).SystemValue = ToMeters(height);
                ((Dimension)swModel.Parameter($"Window{index}BottomOffset@Sketch{sketchNumber}")).SystemValue = ToMeters(verticalOffset);
                ((Dimension)swModel.Parameter($"Window{index}RightOffset@Sketch{sketchNumber}")).SystemValue = ToMeters(horizontalOffset);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to update Window{index} at Sketch{sketchNumber}: {ex.Message}");
            }
        }

        public static void SetDoor(ModelDoc2 swModel, int index, int sketchNumber, double width, double height, double verticalOffset, double horizontalOffset)
        {
            if (index < 1 || index > 2)
            {
                Console.WriteLine($"❌ Invalid door index: {index}");
                return;
            }

            try
            {
                ((Dimension)swModel.Parameter($"Door{index}Display@Sketch{sketchNumber}")).SystemValue = ToMeters(width);
                ((Dimension)swModel.Parameter($"Door{index}length@Sketch{sketchNumber}")).SystemValue = ToMeters(height);
                ((Dimension)swModel.Parameter($"Door{index}BottomOffset@Sketch{sketchNumber}")).SystemValue = ToMeters(verticalOffset);
                ((Dimension)swModel.Parameter($"Door{index}RightOffset@Sketch{sketchNumber}")).SystemValue = ToMeters(horizontalOffset);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to update Door{index} at Sketch{sketchNumber}: {ex.Message}");
            }
        }
    }
}
