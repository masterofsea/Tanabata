using Grpc.Core;
using Tanabata.Domain.Protos;

namespace Tanabata.Api.Services;

public class SkyStreamService : SkyService.SkyServiceBase
{
    private readonly ILogger<SkyStreamService> _logger;
    private readonly ILiveLocationStore _liveLocationStore;

    public SkyStreamService(
        ILogger<SkyStreamService> logger,
        ILiveLocationStore liveLocationStore)
    {
        _logger = logger;
        _liveLocationStore = liveLocationStore;
    }

    public override async Task ConnectSky(
        IAsyncStreamReader<StarFrame> requestStream,
        IServerStreamWriter<SkySnapshot> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("New star connected to the Tanabata Protocol.");

        try
        {
            await foreach (var frame in requestStream.ReadAllAsync())
            {
                _logger.LogDebug("Star {Id} moved to [{X}, {Y}]", frame.StarId, frame.X, frame.Y);
                
                await _liveLocationStore.UpdateAsync(frame);
                
                var snapshot = new SkySnapshot();
                snapshot.NearbyStars.AddRange(await _liveLocationStore.GetNearbyAsync(frame.X, frame.Y, 1000));

                await responseStream.WriteAsync(snapshot);
            }
        }
        catch (IOException)
        {
            _logger.LogWarning("Connection lost with a star.");
        }
    }
    
    public override async Task<PlacesList> GetNearbyPlaces(Coords request, ServerCallContext context)
    {
        var p = await _liveLocationStore.LoadPlacesToRedisAsync(request.Lat, request.Lon);            
        var response = new PlacesList();
    
        foreach (var el in p)
        {
            response.Places.Add(new Place {
                Name = el.Name,
                Lat = el.Lat,
                Lon = el.Lon,
                DistanceMeters = 0 // Для простоты пока 0, или посчитай вручную
            });
        }
    
        return response;
    }
}