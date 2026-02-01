using System.Globalization;
using Tanabata.Domain.Osm;

namespace Tanabata.Api;

public interface IOsmService
{
    Task<List<OsmElement>> GetTourismPlacesAsync(double lat, double lon, int radiusMeters = 1000);
}

public class OsmService  : IOsmService 
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OsmService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // Используем CultureInfo.InvariantCulture, чтобы не было проблем с запятыми в числах
    // Захардкоженная строка запроса для крупных городов Японии
    // [place~"city"] - ищем только крупные города
    // population > 100000 - фильтр, чтобы список был управляемым
    private const string OverpassQuery = @"
        [out:json][timeout:60];
        area[""name:en""=""Russia""]->.searchArea;
        (
          node[""place""=""city""](area.searchArea);
          node[""place""=""town""][""population""~""^[5-9][0-9]{4,}|[1-9][0-9]{5,}""](area.searchArea);
        );
        out tags center;";
    
    private const string OverpassQueryRussia = @"
    [out:json][timeout:60];
    area[""name:en""=""Russia""]->.searchArea;
    (
      node[""place""=""city""](area.searchArea);
      node[""place""=""town""][""population""~""^[5-9][0-9]{4,}|[1-9][0-9]{5,}""](area.searchArea);
    );
    out tags center;";
    
    public async Task<List<OsmElement>> GetTourismPlacesAsync(double lat, double lon, int radiusMeters = 1000)
    {


        var url = $"https://overpass-api.de/api/interpreter?data={Uri.EscapeDataString(OverpassQuery)}";
    
        var httpClient = _httpClientFactory.CreateClient(); 
        httpClient.DefaultRequestHeaders.Add("User-Agent", "TanabataProject/1.0 (contact: masterofsea147@gmail.com)");
        
        var response = await httpClient.GetFromJsonAsync<OsmResponse>(url);
        var cities = response?.Elements ?? [];

        // Сортируем по населению (если указано), чтобы в топе были мегаполисы
        return cities.OrderByDescending(c => 
            c.Tags?.TryGetValue("population", out var tag) is true 
                ? long.Parse(tag) 
                : 0).ToList();
    }
}