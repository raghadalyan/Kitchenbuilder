using System.Text.Json;
using System.Text;
using System.IO;
using SolidWorks.Interop.sldworks;
using System.Runtime.InteropServices;
using SolidWorks.Interop.swconst;
using Kitchenbuilder.Core.Models;

namespace Kitchenbuilder.Core
{
    public static class ApplyCabinet
    {
        private static string debugPath =>
            Path.Combine(KitchenConfig.Get().BasePath, "Kitchenbuilder", "Output", "Apply cab.txt");

        private static void WriteDebug(string message)
        {
            File.AppendAllText(debugPath, $"{DateTime.Now}: {message}{System.Environment.NewLine}");
        }

        public static bool CanAddCabinet(string jsonPath, int stationIndex, int widthToAdd, int copiesCount = 1)
        {
            WriteDebug($"[CanAddCabinet] Path: {jsonPath}, StationIndex: {stationIndex}, WidthToAdd: {widthToAdd}, Copies: {copiesCount}");

            if (!File.Exists(jsonPath))
            {
                WriteDebug("❌ File not found.");
                throw new FileNotFoundException($"File not found: {jsonPath}");
            }

            var json = File.ReadAllText(jsonPath);
            var stations = JsonSerializer.Deserialize<List<StationInfo>>(json) ?? new List<StationInfo>();

            if (stationIndex < 0 || stationIndex >= stations.Count)
            {
                WriteDebug("❌ Invalid station index.");
                return false;
            }

            var station = stations[stationIndex];
            int totalWidth = station.StationEnd - station.StationStart;
            int usedWidth = station.Cabinets?.Sum(c => c.Width) ?? 0;
            int totalNewWidth = widthToAdd * copiesCount;

            WriteDebug($"TotalWidth: {totalWidth}, UsedWidth: {usedWidth}, TotalNewWidth: {totalNewWidth}");

            bool canAdd = usedWidth + totalNewWidth <= totalWidth;
            WriteDebug($"✅ CanAdd: {canAdd}");

            return canAdd;
        }

        public static void AddCabinet(string jsonPath, int stationIndex, int width, bool hasDrawers, int height, int drawerCount, int copiesCount, IModelDoc2 swModel)
        {
            WriteDebug($"[AddCabinet] Path: {jsonPath}, StationIndex: {stationIndex}, Width: {width}, Height: {height}, HasDrawers: {hasDrawers}, Copies: {copiesCount}");
            if (!hasDrawers)
            {
                drawerCount = 1;
            }
            else
            {
                drawerCount = Math.Clamp(drawerCount, 2, 5);
            }

            if (width < 5)
            {
                WriteDebug("❌ Cabinet width must be at least 5 cm.");
                return;
            }

            if (height < 5)
            {
                WriteDebug("❌ Cabinet height must be at least 5 cm.");
                return;
            }

            if (!File.Exists(jsonPath))
            {
                WriteDebug("❌ File not found.");
                throw new FileNotFoundException($"File not found: {jsonPath}");
            }

            var json = File.ReadAllText(jsonPath);
            var stations = JsonSerializer.Deserialize<List<StationInfo>>(json) ?? new();

            if (stationIndex < 0 || stationIndex >= stations.Count)
            {
                WriteDebug("❌ Invalid station index.");
                throw new ArgumentOutOfRangeException(nameof(stationIndex), "Invalid station index.");
            }

            var station = stations[stationIndex];
            int wall = station.WallNumber;

            int existingCount = stations
                .Where(s => s.WallNumber == wall)
                .SelectMany(s => s.Cabinets ?? new List<CabinetInfo>())
                .Count();

            int currentX = station.StationStart + (station.Cabinets?.Sum(c => c.Width) ?? 0);
            List<CabinetInfo> newlyAdded = new();

            for (int i = 0; i < copiesCount; i++)
            {
                int cabinetNum = existingCount + i + 1;
                var cabinet = new CabinetInfo
                {
                    SketchName = $"Sketch_Cabinet{wall}_{cabinetNum}",
                    Width = width,
                    HasDrawers = hasDrawers,
                    Height = height,
                    DistanceX = currentX,
                    DistanceY = 15
                };

                // Create the Drawers object and compute dimensions
                var drawers = new Drawers($"Drawers{wall}_{cabinetNum}");

                double availableHeight = height - 4 - (drawerCount > 1 ? 2 * (drawerCount - 1) : 0);
                double drawerHeight = drawerCount > 0 ? availableHeight / drawerCount : 0;

                for (int d = 1; d <= drawerCount; d++)
                {
                    double dy = 2 + (drawerHeight + 2) * (drawerCount - d);

                    typeof(Drawers).GetProperty($"Width{d}")?.SetValue(drawers, drawerHeight);
                    typeof(Drawers).GetProperty($"DistanceY{d}")?.SetValue(drawers, dy);
                }
                cabinet.Drawers = drawers;


                WriteDebug($"➕ Adding cabinet #{cabinetNum}: {cabinet.SketchName}, Width: {width}, Height: {height}, DistanceX: {currentX}");

                if (station.Cabinets == null)
                    station.Cabinets = new List<CabinetInfo>();

                station.Cabinets.Add(cabinet);
                newlyAdded.Add(cabinet);
                currentX += width;
            }

            // Save updated JSON
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(stations, options));
            WriteDebug("✅ Cabinets successfully added and file saved.");

            // Apply dimensions only to newly added cabinets
            try
            {
                var tempStation = new StationInfo
                {
                    WallNumber = wall,
                    Cabinets = newlyAdded
                };

                ApplyCabinetDimensions.Apply(swModel, new List<StationInfo> { tempStation });
                WriteDebug("✅ ApplyCabinetDimensions completed for newly added cabinets.");
            }
            catch (Exception ex)
            {
                WriteDebug($"❌ Error applying cabinet dimensions: {ex.Message}");
            }
        }


    }
}