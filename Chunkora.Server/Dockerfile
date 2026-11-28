FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5131
EXPOSE 5201

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Chunkora.Server/Chunkora.Server.csproj", "Chunkora.Server/"]
COPY ["Chunkora.Contracts/Chunkora.Contracts.csproj", "Chunkora.Contracts/"]
RUN dotnet restore "Chunkora.Server/Chunkora.Server.csproj" /p:PublishSingleFile=false /p:SelfContained=false /p:UseAppHost=false
COPY . .
WORKDIR "/src/Chunkora.Server"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Chunkora.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:PublishSingleFile=false /p:SelfContained=false /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Chunkora.Server.dll"]
