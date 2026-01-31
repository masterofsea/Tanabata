using StackExchange.Redis;
using Tanabata.Domain.Osm;
using Tanabata.Domain.Protos;

namespace Tanabata.Api;

public interface ILiveLocationStore
{
    Task UpdateAsync(StarFrame frame);
    
    Task<List<StarFrame>> GetNearbyAsync(double x, double y, double radius);
    
    Task<List<OsmElement>> LoadPlacesToRedisAsync(double lat, double lon);
}

public class LiveLocationStore : ILiveLocationStore
{
    private const string RedisKey = "sky_map";

    private readonly IDatabase _db;
    private readonly IOsmService _osmService;
    
    // Все настройки трансформации в одном месте
    private const double Scale = 20.0;
    private const double MaxLon = 179.9;
    private const double MaxLat = 85.0;

    public LiveLocationStore(
        IOsmService osmService,
        IConnectionMultiplexer redis)
    {
        _osmService = osmService;
        _db = redis.GetDatabase();
    }

    // Единая точка входа для координат
    private (double lon, double lat) ToGeo(double x, double y) => (
        Math.Clamp(x / Scale, -MaxLon, MaxLon),
        Math.Clamp(y / Scale, -MaxLat, MaxLat)
    );

    // Единая точка выхода
    private (double x, double y) FromGeo(double lon, double lat) => (
        lon * Scale,
        lat * Scale
    );

    public async Task UpdateAsync(StarFrame frame) 
    {
        var (lon, lat) = ToGeo(frame.X, frame.Y);
        await _db.GeoAddAsync(RedisKey, lon, lat, frame.StarId);
    }

    public async Task<List<StarFrame>> GetNearbyAsync(double x, double y, double radius)
    {
        var (searchLon, searchLat) = ToGeo(x, y);
        
        var results = await _db.GeoSearchAsync(
            RedisKey, searchLon, searchLat, new GeoSearchCircle(radius / Scale));

        return results.Select(r =>
        {
            var (posX, posY) = FromGeo(r.Position.Value.Longitude, r.Position.Value.Latitude);
            
            return new StarFrame {
                StarId = r.Member.ToString(),
                X = posX,
                Y = posY
            };
        }).ToList();
    }
    
    public async Task<List<OsmElement>> LoadPlacesToRedisAsync(double lat, double lon)
    {
        var places = await _osmService.GetTourismPlacesAsync(lat, lon);
    
        foreach (var place in places)
        {
            // Используем GeoAdd для сохранения в "вечный" ключ
            await _db.GeoAddAsync("map_static", place.Lon, place.Lat, place.Name);
        }
    
        return places;
    }
}