using System.Text.Json;
using Kitchenbuilder.Core.Models;
using Kitchenbuilder.Core.WallBuilders;
namespace Kitchenbuilder.Core.Services
{
    public static class KitchenGenerator
    {
        public static void GenerateKitchen(string jsonPath)
        {
            var kitchen = JsonSerializer.Deserialize<Kitchen>(File.ReadAllText(jsonPath));
            GenerateWallsAndFloor(kitchen);

            // Step 4: Create the base
            kitchen.Base = BaseCreator.CreateBase(kitchen);

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(kitchen, new JsonSerializerOptions { WriteIndented = true }));

            // ✅ Now kitchen has base, wait for user response (in UI) to continue
        }
        private static void GenerateWallsAndFloor(Kitchen kitchen)
        {
            try
            {
                int wallCount = kitchen.Walls?.Count ?? 0;

                switch (wallCount)
                {
                    case 1:
                        OneWallBuilder.Run(kitchen);
                        break;
                    case 2:
                        TwoWallBuilder.Run(kitchen);
                        break;
                    case 3:
                        ThreeWallBuilder.Run(kitchen);
                        break;
                    case 4:
                        FourWallBuilder.Run(kitchen);
                        break;
                    default:
                        return;
                }

            }
            catch (Exception ex)
            {

            }
        }

        public static void ContinueAfterBaseApproved(string jsonPath)
        {
            var kitchen = JsonSerializer.Deserialize<Kitchen>(File.ReadAllText(jsonPath));

            // Future: FridgeEvaluator.Evaluate(...), etc.

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(kitchen, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
