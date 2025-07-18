using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;

namespace Kitchenbuilder.Core.Materials
{
    public static class ColorApplier
    {
        public static bool ApplyColorToBody(IModelDoc2 model, string bodyName, string hexColor, double specular, double reflectivity, double transparency)
        {

            if (model == null)
            {
                Console.WriteLine("❌ model is null");
                return false;
            }

            var rgb = ParseHexColor(hexColor);

            double[] appearance = new double[9]
            {
                rgb[0], rgb[1], rgb[2],
                1.0, // alpha
                0, 0,
                specular,
                reflectivity,
                transparency
            };

            var selMgr = (SelectionMgr)model.SelectionManager;

            bool selected = model.Extension.SelectByID2(
                bodyName, "SOLIDBODY", 0, 0, 0, false, 0, null, 0);

            if (!selected) return false;

            var selectedObj = selMgr.GetSelectedObject6(1, -1);
            if (selectedObj is not IBody2 body) return false;

            body.MaterialPropertyValues2 = appearance;
            model.ClearSelection2(true);

            return true;
        }

        private static double[] ParseHexColor(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Hex color is null or empty.");

            if (hex.StartsWith("#"))
                hex = hex[1..];

            if (hex.Length < 6)
                throw new ArgumentException($"Invalid hex color: {hex}");

            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);

            return new double[] { r / 255.0, g / 255.0, b / 255.0 };
        }

    }
}
