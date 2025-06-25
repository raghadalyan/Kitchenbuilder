using Kitchenbuilder.Core.Models;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using System;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class CounterTopCreator
    {
        public static void Create(string optionName)
        {
            if (string.IsNullOrEmpty(optionName))
            {
                Console.WriteLine("❌ Option name is null or empty.");
                return;
            }

            try
            {
                switch (optionName.ToLower())
                {
                    case "option1":
                        Console.WriteLine("🧱 Line Shape - handling countertop...");
                        HandleLineSapeCounterTop.Process();
                        break;

                    case "option2":
                        Console.WriteLine("🔺 L Shape - handler not implemented yet.");
                        // TODO: HandleLShapeCounterTop.Process();
                        break;

                    case "option3":
                        Console.WriteLine("🔵 U Shape - handler not implemented yet.");
                        // TODO: HandleUShapeCounterTop.Process();
                        break;

                    default:
                        Console.WriteLine($"⚠️ Unknown option: {optionName}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CounterTopCreator: {ex.Message}");
            }
        }
    }
}
