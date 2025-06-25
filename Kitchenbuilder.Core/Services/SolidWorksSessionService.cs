using SolidWorks.Interop.sldworks;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public class SolidWorksSessionService
    {
        private IModelDoc2? _activeModel;
        private static readonly string LogPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\sw_session_debug.txt";

        /// <summary>
        /// Sets the currently active model (e.g., after opening a .SLDPRT file).
        /// </summary>
        public void SetActiveModel(IModelDoc2 model)
        {
            _activeModel = model;
            Log($"✅ Set active model: {model.GetTitle()}");
        }

        /// <summary>
        /// Returns the active model (e.g., to edit it or activate sketches).
        /// </summary>
        public IModelDoc2? GetActiveModel()
        {
            if (_activeModel == null)
                Log("❌ Tried to get active model, but it's null.");
            else
                Log($"📤 Retrieved active model: {_activeModel.GetTitle()}");

            return _activeModel;
        }

        /// <summary>
        /// Clears the session state.
        /// </summary>
        public void Clear()
        {
            _activeModel = null;
            Log("🧹 Cleared active model session.");
        }

        private void Log(string message)
        {
            string logLine = $"[{DateTime.Now:HH:mm:ss}] {message}";
            File.AppendAllText(LogPath, logLine + System.Environment.NewLine);
        }
    }
}
