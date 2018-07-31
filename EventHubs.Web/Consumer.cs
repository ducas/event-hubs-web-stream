using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Configuration;
using Timer = System.Timers.Timer;

namespace EventHubs.Web
{
    public interface IConsumer
    {
        bool Connected { get; }
        Task Connect();
    }

    public class Consumer : IConsumer
    {
        public bool Connected { get; private set; }

        private readonly List<PartitionReceiver> _receivers = new List<PartitionReceiver>();
        private readonly EventHubClient _eventHubClient;
        private readonly IHubContext<Broadcaster> _context;

        public Consumer(IConfiguration config, IHubContext<Broadcaster> context)
        {
            _context = context;
            _eventHubClient = EventHubClient.CreateFromConnectionString(config.GetConnectionString("EventHubs"));
        }

        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        private Timer _timer;

        public async Task Connect()
        {
            if (Connected) return;
            await SemaphoreSlim.WaitAsync();
            if (Connected) return;
            try
            {
                var runTimeInformation = await _eventHubClient.GetRuntimeInformationAsync();
                foreach (var partitionId in runTimeInformation.PartitionIds)
                {
                    var receiver = _eventHubClient.CreateReceiver(PartitionReceiver.DefaultConsumerGroupName, partitionId, EventPosition.FromEnd());
                    _receivers.Add(receiver);
                }
                _timer = new Timer(1000)
                {
                    AutoReset = true
                };
                _timer.Elapsed += Poll;
                _timer.Start();
                Connected = true;
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        private async void Poll(object sender, ElapsedEventArgs e)
        {
            foreach (var receiver in _receivers)
            {
                var ehEvents = await receiver.ReceiveAsync(100);
                if (ehEvents == null) continue;
                
                foreach (var ehEvent in ehEvents)
                {
                    var message = Encoding.UTF8.GetString(ehEvent.Body.Array);
                    await _context.Clients.All.SendAsync("Message", receiver.PartitionId, message);
                }
            }
        }
    }
}
