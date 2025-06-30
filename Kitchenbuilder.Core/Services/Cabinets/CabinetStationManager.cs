using System.Text.Json.Nodes;

namespace Kitchenbuilder.Core
{
    public class CabinetStation
    {
        public string Wall { get; set; }
        public string SketchName { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
        public double Width => End - Start;
    }

    public static class CabinetStationManager
    {
        public static List<CabinetStation> LoadStations(string jsonPath)
        {
            var stations = new List<CabinetStation>();
            var json = File.ReadAllText(jsonPath);
            var data = JsonNode.Parse(json)?.AsObject();
            if (data == null) return stations;

            foreach (var wall in data)
            {
                if (!wall.Key.StartsWith("Wall")) continue;
                var bases = wall.Value?["Bases"]?.AsObject();
                if (bases == null) continue;

                foreach (var basePair in bases)
                {
                    var baseObj = basePair.Value?.AsObject();
                    if (baseObj == null) continue;

                    bool visible = baseObj["Visible"]?.GetValue<bool>() == true;
                    bool hasCountertop = baseObj.ContainsKey("Countertop") && baseObj["Countertop"]?.ToString() != "null";
                    if (!visible || !hasCountertop) continue;

                    var sketch = baseObj["SketchName"]?.ToString() ?? "unknown";
                    var start = baseObj["Start"]?.GetValue<double>() ?? 0;
                    var end = baseObj["End"]?.GetValue<double>() ?? 0;

                    stations.Add(new CabinetStation
                    {
                        Wall = wall.Key,
                        SketchName = sketch,
                        Start = start,
                        End = end
                    });
                }
            }

            return stations;
        }
    }
}
