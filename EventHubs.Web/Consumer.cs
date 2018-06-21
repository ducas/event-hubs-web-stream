using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Configuration;

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

        List<PartitionReceiver> _receivers = new List<PartitionReceiver>();
        EventHubClient _eventHubClient;
        IHubContext<Broadcaster> _context;

        public Consumer(IConfiguration config, IHubContext<Broadcaster> context)
        {
            _context = context;
            _eventHubClient = EventHubClient.CreateFromConnectionString(config.GetConnectionString("EventHubs"));
        }

        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
        private System.Timers.Timer _timer;

        public async Task Connect()
        {
            if (Connected) return;
            await semaphoreSlim.WaitAsync();
            if (Connected) return;
            try
            {
                var runTimeInformation = await _eventHubClient.GetRuntimeInformationAsync();
                foreach (var partitionId in runTimeInformation.PartitionIds)
                {
                    var receiver = _eventHubClient.CreateReceiver(PartitionReceiver.DefaultConsumerGroupName, partitionId, EventPosition.FromEnd());
                    _receivers.Add(receiver);
                }
                _timer = new System.Timers.Timer(1000)
                {
                    AutoReset = true
                };
                _timer.Elapsed += Poll;
                _timer.Start();
                Connected = true;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private async void Poll(object sender, ElapsedEventArgs e)
        {
            foreach (var receiver in _receivers)
            {
                var ehEvents = await receiver.ReceiveAsync(100);
                if (ehEvents != null)
                {
                    foreach (var ehEvent in ehEvents)
                    {
                        var message = UnicodeEncoding.UTF8.GetString(ehEvent.Body.Array);
                        await _context.Clients.All.SendAsync("Message", receiver.PartitionId, message);
                    }
                }
            }
        }
    }
}
