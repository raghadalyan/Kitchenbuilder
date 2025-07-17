using System;
using System.Collections.Generic;
using System.IO;
using Kitchenbuilder.Models;
using SolidWorks.Interop.sldworks;

namespace Kitchenbuilder.Core
{
    public static class AnalyzeSinkCooktop
    {
        private static string DebugPath =>
            Path.Combine(KitchenConfig.Get().BasePath, "Kitchenbuilder", "Output", "Sink-Cooktop", "AnalyzeSinkCooktop.txt");

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{System.Environment.NewLine}");
        }

        public static void Process(List<Countertop> relevantCountertops, int optionNum, IModelDoc2 model)
        {
            if (relevantCountertops == null || relevantCountertops.Count == 0)
            {
                Log("❌ No valid countertops to process.");
                return;
            }
            Show_Bodies_In_Sld_IModel.ShowMultipleBodies(model, new[] { "Faucet Inox", "Kitchen_Cooktop_Simple", "Cut-Sink" });

            Log($"▶️ Starting analysis for Option {optionNum}");
            Log($"🔍 Relevant countertops count: {relevantCountertops?.Count ?? 0}");

            if (relevantCountertops == null || relevantCountertops.Count == 0)
            {
                Log("❌ No valid countertops to process.");
                return;
            }

            foreach (var ct in relevantCountertops)
            {
                Log($"📏 Wall{ct.WallNumber}.{ct.BaseKey} | Start={ct.Start}, End={ct.End}, Width={ct.Width}");
            }

            if (relevantCountertops.Count == 1)
            {
                Log("🔧 Only one valid countertop found. Delegating to OneCountertopSelector...");
                OneCountertopSelector.SuggestLayouts(relevantCountertops[0], optionNum, model);
            }
            else if (relevantCountertops.Count == 2)
            {
                Log("🔧 Two valid countertops found. Delegating to TwoCountertopSelector...");
                TwoCountertopSelector.SuggestLayouts(relevantCountertops, optionNum, model);
            }
            else
            {
                Log("⚠️ More than two countertops found. Delegating to MultiCountertopSelector...");
                MultiCountertopSelector.SuggestLayouts(relevantCountertops, optionNum, model);
            }
        }
    }
}
