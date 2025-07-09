//using SolidWorks.Interop.sldworks;
//using SolidWorks.Interop.swconst;

//namespace Kitchenbuilder.Core
//{
//    public static class SolidWorksLoader
//    {
//        public static ModelDoc2 LoadPart(string path)
//        {
//            var swApp = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
//            if (swApp == null) return null;

//            swApp.Visible = true;

//            int errors = 0, warnings = 0;
//            var model = swApp.OpenDoc6(
//                path,
//                (int)swDocumentTypes_e.swDocPART,
//                (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
//                "", ref errors, ref warnings
//            ) as ModelDoc2;

//            return model;
//        }
//    }
//}
