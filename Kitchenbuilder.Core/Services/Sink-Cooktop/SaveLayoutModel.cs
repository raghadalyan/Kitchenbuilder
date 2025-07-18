using System;
using System.IO;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public static class SaveLayoutModel
    {
        private static readonly string LogPath = Path.Combine(
            KitchenConfig.Get().BasePath,
            "Kitchenbuilder", "Output", "Sink-Cooktop", "OneCountertopSelector.txt"
        );

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}");
        }

        public static void Save(IModelDoc2 model, int index)
        {
            try
            {
                string folder = Path.Combine(
                    KitchenConfig.Get().BasePath,
                    "Kitchenbuilder", "Output", "temp", "Layout"
                );
                Directory.CreateDirectory(folder);

                string path = Path.Combine(folder, $"Layout{index}.sldprt");
                bool result = model.SaveAs(path);

                Log(result
                    ? $"💾 Saved layout copy to {path}"
                    : $"❌ Failed to save layout copy to {path}");
            }
            catch (Exception ex)
            {
                Log($"❌ Exception during saving layout copy: {ex.Message}");
            }
        }
    }
}
