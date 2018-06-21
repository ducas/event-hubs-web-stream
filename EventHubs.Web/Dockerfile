FROM node:8 AS node-builder
WORKDIR /src
COPY . .
RUN npm install
RUN mkdir -p wwwroot/lib/
RUN cp node_modules/@aspnet/signalr/dist/browser/*.min.* wwwroot/lib/

FROM microsoft/dotnet:2.1-sdk AS dotnet-builder
WORKDIR /app
COPY *.csproj .
RUN dotnet restore
COPY . .
COPY --from=node-builder /src/wwwroot  ./wwwroot
RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.1-aspnetcore-runtime AS runtime
WORKDIR /app
COPY --from=dotnet-builder /app/out .
ENTRYPOINT ["dotnet", "EventHubs.Web.dll"]