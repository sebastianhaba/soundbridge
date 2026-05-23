FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/SoundBridge.App/SoundBridge.App.csproj src/SoundBridge.App/
RUN dotnet restore src/SoundBridge.App/SoundBridge.App.csproj
COPY . .
RUN dotnet publish src/SoundBridge.App/SoundBridge.App.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
VOLUME /app/data
EXPOSE 1900/udp
EXPOSE 5000
ENTRYPOINT ["dotnet", "SoundBridge.App.dll"]
