using System.Text.Json;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core.Services
{
    public static class KitchenGenerator
    {
        public static void GenerateKitchen(string jsonPath)
        {
            var kitchen = JsonSerializer.Deserialize<Kitchen>(File.ReadAllText(jsonPath));

            // Step 4: Create the base
            kitchen.Base = BaseCreator.CreateBase(kitchen);

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(kitchen, new JsonSerializerOptions { WriteIndented = true }));

            // ✅ Now kitchen has base, wait for user response (in UI) to continue
        }

        public static void ContinueAfterBaseApproved(string jsonPath)
        {
            var kitchen = JsonSerializer.Deserialize<Kitchen>(File.ReadAllText(jsonPath));

            // Future: FridgeEvaluator.Evaluate(...), etc.

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(kitchen, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
