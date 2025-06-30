using System.Text.Json;
using System.Text;
using System.IO;

namespace Kitchenbuilder.Core
{
    public static class ApplyCabinet
    {
        private static string debugPath = @"C:\Users\chouse\Downloads\Kitchenbuilder\Output\Apply cab.txt";

        private static void WriteDebug(string message)
        {
            File.AppendAllText(debugPath, $"{DateTime.Now}: {message}{Environment.NewLine}");
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
            WriteDebug($"[AddCabinet] Path: {jsonPath}, StationIndex: {stationIndex}, Width: {width}, HasDrawers: {hasDrawers}, Copies: {copiesCount}");

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
                throw new ArgumentOutOfRangeException(nameof(stationIndex), "Invalid station index.");
            }

            var station = stations[stationIndex];
            int wall = station.WallNumber;

            // Get the max cabinet number across all stations for the same wall
            int existingCount = stations
                .Where(s => s.WallNumber == wall)
                .SelectMany(s => s.Cabinets ?? new List<CabinetInfo>())
                .Count();

            for (int i = 0; i < copiesCount; i++)
            {
                int cabinetNum = existingCount + i + 1;

                var cabinet = new CabinetInfo
                {
                    SketchName = $"Sketch_Cabinet{wall}_{cabinetNum}",
                    Width = width,
                    HasDrawers = hasDrawers
                };

                WriteDebug($"➕ Adding cabinet #{cabinetNum}: {cabinet.SketchName}, Width: {width}, HasDrawers: {hasDrawers}");
                station.Cabinets.Add(cabinet);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(stations, options));

            WriteDebug("✅ Cabinets successfully added and file saved.");
        }

    }
}
