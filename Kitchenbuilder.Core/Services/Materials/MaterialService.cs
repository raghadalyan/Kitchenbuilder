using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kitchenbuilder.Core.Materials
{
    public static class MaterialService
    {
        public static List<MaterialItem> GetMarbleMaterials() => new()
        {
            new MaterialItem { Name = "Marble012", ImagePath = "/Images/Materials/Marble/marble012.png" },
            new MaterialItem { Name = "Marble022", ImagePath = "/Images/Materials/Marble/marble022.png" },
            new MaterialItem { Name = "Marble025", ImagePath = "/Images/Materials/Marble/marble025.png" },
            new MaterialItem { Name = "Onyx001", ImagePath = "/Images/Materials/Marble/Onyx001.png" },
            new MaterialItem { Name = "Onyx005", ImagePath = "/Images/Materials/Marble/Onyx005.png" },
            new MaterialItem { Name = "Onyx006", ImagePath = "/Images/Materials/Marble/Onyx006.png" },
            new MaterialItem { Name = "Onyx013", ImagePath = "/Images/Materials/Marble/Onyx013.png" },
            new MaterialItem { Name = "Onyx015", ImagePath = "/Images/Materials/Marble/Onyx015.png" },
            new MaterialItem { Name = "Travertine005", ImagePath = "/Images/Materials/Marble/Travertine005.png" },
            new MaterialItem { Name = "Travertine009", ImagePath = "/Images/Materials/Marble/Travertine009.png" },
            new MaterialItem { Name = "Travertine011", ImagePath = "/Images/Materials/Marble/Travertine011.png" },
        };

        public static List<MaterialItem> GetTileMaterials() => new()
        {
            new MaterialItem { Name = "Tiles054", ImagePath = "/Images/Materials/Tiles/Tiles054.png" },
            new MaterialItem { Name = "Tiles133A", ImagePath = "/Images/Materials/Tiles/Tiles133A.png" },
            new MaterialItem { Name = "RoofingTiles013A", ImagePath = "/Images/Materials/Tiles/roofingtiles013a.png" },
        };

        public static void ApplyMaterialToBodies(ISldWorks swApp, string materialName, List<string> targetBodies, string debugPath)
        {
            var model = (ModelDoc2)swApp.ActiveDoc;
            var part = (PartDoc)model;
            var bodies = (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, true);

            if (bodies == null || bodies.Length == 0)
            {
                Log(debugPath, "❌ No bodies found in the model.");
                return;
            }

            var allBodyNames = bodies.Cast<Body2>().Select(b => b.Name).ToList();

            foreach (var bodyName in targetBodies.Distinct())
            {
                if (!allBodyNames.Contains(bodyName))
                {
                    Log(debugPath, $"⚠️ Body not found: {bodyName}");
                    continue;
                }

                Log(debugPath, $"🎯 Applying material '{materialName}' to body: {bodyName}");
                OurMaterial.ApplyCustomMaterial(swApp, bodyName, materialName);
            }

            Log(debugPath, $"✅ Applied material '{materialName}' to {targetBodies.Count} bodies.");
        }

        private static void Log(string path, string msg)
        {
            Console.WriteLine(msg);
            File.AppendAllText(path, msg + "\n");
        }
    }

    public class MaterialItem
    {
        public string Name { get; set; } = "";
        public string ImagePath { get; set; } = "";
    }
}
