# event-hubs-web-stream
Connect to an event hub to see all messages as they stream in

## Getting started

1. Clone the repo
1. Update **EventHubs.Web\appsettings.json** to include your connection string
1. With docker, run `docker-compose up`. Otherwise, run `dotnet run -p EventHubs.Web`

## What it does...

This project lets you watch messages on an Event Hub by:

1. Establishing a connection between the user's browser and a web server using Signalr and web sockets
1. Polling partition receivers to get messages from the hub every second
1. Broadcasting all messages from the Event Hub to all clients over the socket
