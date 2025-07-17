using System.Text.Json;

public class KitchenConfigModel
{
    public string BasePath { get; set; } = "";
}

public static class KitchenConfig
{
    public static KitchenConfigModel Get()
    {
        string configPath = Path.Combine(AppContext.BaseDirectory, "JSON", "config.json");
        string json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<KitchenConfigModel>(json)!;
    }
}
