using SolidWorks.Interop.sldworks;
using System.Collections.Generic;

namespace Kitchenbuilder.Core
{
    public static class BodyNameHelper
    {
        public static List<string> GetAllBodyNames(IModelDoc2 model)
        {
            var result = new List<string>();

            Feature feature = model.FirstFeature() as Feature;
            while (feature != null)
            {
                string type = feature.GetTypeName2();
                if (type == "SolidBodyFolder")
                {
                    var folder = feature.GetSpecificFeature2() as IBodyFolder;
                    if (folder?.GetBodies() is object[] bodies)
                    {
                        foreach (object b in bodies)
                        {
                            if (b is IBody2 body && !string.IsNullOrWhiteSpace(body.Name))
                            {
                                result.Add(body.Name);
                            }
                        }
                    }
                }

                feature = feature.GetNextFeature() as Feature;
            }

            return result;
        }
    }
}
