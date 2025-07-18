using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;

namespace Kitchenbuilder.Core
{
    public static class EditExtrusionDim_IModel
    {
        public static bool EditExtrude(IModelDoc2 model, string featureName, double newDepth)
        {
            try
            {
                if (model == null)
                    throw new ArgumentNullException(nameof(model), "IModelDoc2 is null");

                var part = model as PartDoc;
                if (part == null)
                {
                    Console.WriteLine("❌ Model is not a PartDoc.");
                    return false;
                }

                var feat = part.FeatureByName(featureName) as IFeature;
                if (feat == null)
                {
                    Console.WriteLine($"❌ Feature '{featureName}' not found or not an IFeature.");
                    return false;
                }

                var def = feat.GetDefinition() as IExtrudeFeatureData;
                if (def == null)
                {
                    Console.WriteLine($"❌ Feature '{featureName}' is not an extrusion.");
                    return false;
                }

                def.SetDepth(true, newDepth / 100.0); // Convert cm to meters
                feat.ModifyDefinition(def, model, null);

                Console.WriteLine($"✅ Depth of '{featureName}' set to {newDepth}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in EditExtrude: {ex.Message}");
                return false;
            }
        }
    }
}
