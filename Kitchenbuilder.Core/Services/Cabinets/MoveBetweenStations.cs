using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public static class MoveBetweenStations
    {
        public static void SelectStation(int index, List<StationInfo> stations, ref int currentIndex, IModelDoc2 swModel)
        {
            if (index >= 0 && index < stations.Count)
            {
                currentIndex = index;
                SelectExtrudeFeature(swModel, stations[currentIndex].ExtrudeName);
            }
        }

        public static void PreviousStation(List<StationInfo> stations, ref int currentIndex, IModelDoc2 swModel)
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                SelectExtrudeFeature(swModel, stations[currentIndex].ExtrudeName);
            }
        }

        public static void NextStation(List<StationInfo> stations, ref int currentIndex, IModelDoc2 swModel)
        {
            if (currentIndex < stations.Count - 1)
            {
                currentIndex++;
                SelectExtrudeFeature(swModel, stations[currentIndex].ExtrudeName);
            }
        }

        private static void SelectExtrudeFeature(IModelDoc2 swModel, string extrudeName)
        {
            if (swModel == null || string.IsNullOrEmpty(extrudeName))
                return;

            // Exit active sketch if needed
            if (swModel.SketchManager.ActiveSketch != null)
            {
                swModel.SketchManager.InsertSketch(true);
            }

            Feature feature = (Feature)swModel.FirstFeature();
            while (feature != null)
            {
                if (feature.Name.Equals(extrudeName, StringComparison.OrdinalIgnoreCase))
                {
                    feature.Select2(false, -1);
                    break;
                }

                feature = (Feature)feature.GetNextFeature();
            }
        }
    }
}
