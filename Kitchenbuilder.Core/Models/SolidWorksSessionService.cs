using SolidWorks.Interop.sldworks;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public class SolidWorksSessionService
    {
        private IModelDoc2? _activeModel;
        private ISldWorks? _swApp; // ✅ Store SW instance
        private static string LogPath =>
            Path.Combine(KitchenConfig.Get().BasePath, "Kitchenbuilder", "Output", "sw_session_debug.txt");

        public void SetApp(ISldWorks swApp)
        {
            _swApp = swApp;
            Log("✅ SolidWorks app instance stored in session.");
        }

        public ISldWorks GetApp()
        {
            if (_swApp == null)
            {
                Log("❌ SolidWorks app is null.");
                throw new InvalidOperationException("SolidWorks app not initialized.");
            }

            Log("📤 Retrieved SolidWorks app instance.");
            return _swApp;
        }

        public void SetActiveModel(IModelDoc2 model)
        {
            _activeModel = model;
            Log($"✅ Set active model: {model.GetTitle()}");
        }

        public IModelDoc2? GetActiveModel()
        {
            if (_activeModel == null)
                Log("❌ Tried to get active model, but it's null.");
            else
                Log($"📤 Retrieved active model: {_activeModel.GetTitle()}");

            return _activeModel;
        }

        public void Clear()
        {
            _activeModel = null;
            _swApp = null;
            Log("🧹 Cleared SolidWorks session.");
        }

        private void Log(string message)
        {
            string logLine = $"[{DateTime.Now:HH:mm:ss}] {message}";
            File.AppendAllText(LogPath, logLine + System.Environment.NewLine);
        }
    }
}
