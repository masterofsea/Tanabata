using System.Text.Json.Serialization;

namespace Tanabata.Domain.Osm;

public record OsmResponse
{
    [JsonPropertyName("elements")]
    public List<OsmElement> Elements { get; init; } = [];
}

public record OsmElement
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("lat")]
    public double Lat { get; init; }

    [JsonPropertyName("lon")]
    public double Lon { get; init; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; init; }

    // Удобное свойство для получения имени
    public string Name => Tags?.GetValueOrDefault("name:en") ?? Tags?.GetValueOrDefault("name") ?? $"Point {Id}";
}