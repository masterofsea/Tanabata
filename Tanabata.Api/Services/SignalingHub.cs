using Microsoft.AspNetCore.SignalR;

namespace Tanabata.Api.Services;

public class SignalingHub : Hub
{
    public async Task SendSignal(string targetId, string data)
    {
        await Clients.Client(targetId).SendAsync("ReceiveSignal", Context.ConnectionId, data);
    }

    public string GetMyId() => Context.ConnectionId;
}