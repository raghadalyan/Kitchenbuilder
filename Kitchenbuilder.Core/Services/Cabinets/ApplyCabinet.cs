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
        private static string debugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Apply cab.txt";

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

        public static void AddCabinet(string jsonPath, int stationIndex, int width, bool hasDrawers, int copiesCount = 1)
        {
            WriteDebug($"[AddCabinet] Path: {jsonPath}, StationIndex: {stationIndex}, Width: {width}, Height: {height}, HasDrawers: {hasDrawers}, Copies: {copiesCount}");

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
                string sketchName = $"Sketch_Cabinet{wall}_{cabinetNum}";
                string drawerSketch = $"Drawers_{wall}_{cabinetNum}";

                var cabinet = new CabinetInfo
                {
                    SketchName = sketchName,
                    Width = width,
                    HasDrawers = hasDrawers,
                    Height = height,
                    DistanceX = currentX,
                    DistanceY = 70
                };

                WriteDebug($"➕ Adding cabinet #{cabinetNum}: {cabinet.SketchName}, Width: {width}, DistanceX: {currentX}, DistanceY: 70");

                if (station.Cabinets == null)
                    station.Cabinets = new List<CabinetInfo>();

                station.Cabinets.Add(cabinet);
                newlyAdded.Add(cabinet);
                currentX += width;

                WriteDebug($"➕ Added cabinet #{cabinetNum} with {actualDrawerCount} drawers.");
            }

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