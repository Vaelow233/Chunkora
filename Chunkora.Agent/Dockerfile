FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Chunkora.Agent/Chunkora.Agent.csproj", "Chunkora.Agent/"]
RUN dotnet restore "Chunkora.Agent/Chunkora.Agent.csproj"
COPY . .
WORKDIR "/src/Chunkora.Agent"
RUN dotnet build "./Chunkora.Agent.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Chunkora.Agent.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Chunkora.Agent.dll"]
