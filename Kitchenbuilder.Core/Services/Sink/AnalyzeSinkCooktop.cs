using System;
using System.Collections.Generic;
using System.IO;
using Kitchenbuilder.Models;

namespace Kitchenbuilder.Core
{
    public static class AnalyzeSinkCooktop
    {
        private static readonly string DebugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Sink-Cooktop\AnalyzeSinkCooktop.txt";

        private static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DebugPath)!);
            File.AppendAllText(DebugPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        public static void Process(List<Countertop> relevantCountertops, int optionNum)
        {
            Log($"▶️ Starting analysis for Option {optionNum}");
            Log($"🔍 Relevant countertops count: {relevantCountertops.Count}");

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
                OneCountertopSelector.SuggestLayouts(relevantCountertops[0], optionNum);
            }
            else if (relevantCountertops.Count == 2)
            {
                Log("🔧 Two valid countertops found. Delegating to TwoCountertopSelector...");
                TwoCountertopSelector.SuggestLayouts(relevantCountertops, optionNum);
            }
            else
            {
                MultiCountertopSelector.SuggestLayouts(relevantCountertops, optionNum);
                Log("⚠️ More than two countertops ");
            }
        }
    }
}
