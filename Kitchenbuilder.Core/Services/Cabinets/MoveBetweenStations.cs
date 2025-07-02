using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
namespace Kitchenbuilder.Core
{
    public static class MoveBetweenStations
    {
        public static void SelectStation(int index, List<StationInfo> stations, ref int currentIndex, ModelDoc2 swModel)
        {
            if (index >= 0 && index < stations.Count)
            {
                currentIndex = index;
                SelectExtrudeFeature(swModel, stations[currentIndex].ExtrudeName);
            }
        }

        public static void PreviousStation(List<StationInfo> stations, ref int currentIndex, ModelDoc2 swModel)
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                SelectExtrudeFeature(swModel, stations[currentIndex].ExtrudeName);
            }
        }

        public static void NextStation(List<StationInfo> stations, ref int currentIndex, ModelDoc2 swModel)
        {
            if (currentIndex < stations.Count - 1)
            {
                currentIndex++;
                SelectExtrudeFeature(swModel, stations[currentIndex].ExtrudeName);
            }
        }


        private static void SelectExtrudeFeature(ModelDoc2 swModel, string extrudeName)
        {
            if (swModel == null || string.IsNullOrEmpty(extrudeName))
                return;

            // Exit active sketch if any
            if (swModel.SketchManager.ActiveSketch != null)
            {
                swModel.SketchManager.InsertSketch(true);
            }

            Feature feature = (Feature)swModel.FirstFeature(); // ⬅ explicit cast
            while (feature != null)
            {
                if (feature.Name.Equals(extrudeName, StringComparison.OrdinalIgnoreCase))
                {
                    feature.Select2(false, 1);

                    swModel.ViewZoomToSelection(); // Zoom to the selected feature
                    //swModel.ShowNamedView2("*Isometric", (int)swStandardViews_e.swIsometricView);

                    break;
                }

                feature = (Feature)feature.GetNextFeature(); // ⬅ explicit cast
            }
        }

    }
}
