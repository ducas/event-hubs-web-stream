using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EventHubs.Web
{
    [Authorize(JwtBearerDefaults.AuthenticationScheme)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Broadcaster : Hub
    {
        private readonly IConsumer _consumer;

        public Broadcaster(IConsumer consumer) {
            _consumer = consumer;
        }

        public Task Broadcast(string sender, string message) =>
            Clients.All.SendAsync("Message", sender, message);

        public override Task OnConnectedAsync()
        {
            _consumer.Connect();
            return Task.CompletedTask;
        }
    }
}