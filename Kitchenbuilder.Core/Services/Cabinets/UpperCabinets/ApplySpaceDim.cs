using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core
{
    public static class ApplySpaceDim
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\upper\ApplySpaceDim_Debug.txt";

        private static void Log(string msg)
        {
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {msg}{System.Environment.NewLine}");
        }

        public static void Apply(IModelDoc2 model, Space space, int wallNumber, double floorLength, double floorWidth)
        {
            try
            {
                string type = space.Type.Replace(" ", ""); // Clean type name like "Microwave"
                Log($"📦 Applying space type: {type} on wall {wallNumber} (FloorLength: {floorLength}, FloorWidth: {floorWidth})");

                // Step 1: Reset D1 and D2 on Sketch_Move
                Log("➡️ Resetting D1 and D2 to 0...");
                EditSketchDim_IModel.SetDimension(model, $"D1@Sketch_Move_{type}", 0);
                EditSketchDim_IModel.SetDimension(model, $"D2@Sketch_Move_{type}", 0);

                // Step 2: Wall-specific logic
                if (wallNumber == 1)
                {
                    double dy = floorWidth - space.DistanceX;
                    Log("🧱 Wall 1 detected:");
                    Log($"  ↪ angle = 180°, DX = 30 cm, DY = {dy} cm, Plane offset = {space.DistanceY} cm");

                    EditSketchDim_IModel.SetDimension(model, $"angle@Sketch_Rotate_{type}", 180);
                    EditSketchDim_IModel.SetDimension(model, $"DX@Sketch_Move_{type}", 30);
                    EditSketchDim_IModel.SetDimension(model, $"DY@Sketch_Move_{type}", dy- (space.Width / 2));
                    EditPlaneOffset.SetOffset(model, $"Plane_{space.Type}", space.DistanceY);
                }
                else if (wallNumber == 2)
                {
                    Log("🧱 Wall 2 detected:");
                    Log($"  ↪ angle = 90°, DX = {space.DistanceX} cm, DY = 30 cm, Plane offset = {space.DistanceY} cm");

                    EditSketchDim_IModel.SetDimension(model, $"angle@Sketch_Rotate_{type}", 90);
                    EditSketchDim_IModel.SetDimension(model, $"DX@Sketch_Move_{type}", space.DistanceX+(space.Width/2));
                    EditSketchDim_IModel.SetDimension(model, $"DY@Sketch_Move_{type}", 30);
                    EditPlaneOffset.SetOffset(model, $"Plane_{space.Type}", space.DistanceY);
                }
                else if (wallNumber == 3)
                {
                    double dx = floorLength - 30;
                    Log("🧱 Wall 3 detected:");
                    Log($"  ↪ angle = 360°, DX = {dx} cm, DY = {space.DistanceX} cm, Plane offset = {space.DistanceY} cm");

                    EditSketchDim_IModel.SetDimension(model, $"angle@Sketch_Rotate_{type}", 360);
                    EditSketchDim_IModel.SetDimension(model, $"DX@Sketch_Move_{type}", dx);
                    EditSketchDim_IModel.SetDimension(model, $"DY@Sketch_Move_{type}", space.DistanceX + (space.Width / 2));
                    EditPlaneOffset.SetOffset(model, $"Plane_{space.Type}", space.DistanceY);
                }
                else
                {
                    Log($"❌ Unsupported wall number: {wallNumber}");
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Exception in Apply: {ex.Message}");
            }
        }
    }
}
