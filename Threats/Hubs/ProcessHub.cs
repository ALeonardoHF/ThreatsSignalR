using Microsoft.AspNetCore.SignalR;

namespace Threats.Hubs
{
    public class ProcessHub : Hub
    {
        public async Task SendMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }
    }
}
