FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Nellebot/Nellebot.csproj", "Nellebot/"]
COPY ["Nellebot.Common/Nellebot.Common.csproj", "Nellebot.Common/"]
COPY ["Nellebot.Data/Nellebot.Data.csproj", "Nellebot.Data/"]
COPY ["Nellebot.Data.Migrations/Nellebot.Data.Migrations.csproj", "Nellebot.Data.Migrations/"]
RUN dotnet restore "./Nellebot/Nellebot.csproj"
COPY . .
RUN dotnet build "./Nellebot/Nellebot.csproj" -c $BUILD_CONFIGURATION -o /output/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Nellebot/Nellebot.csproj" -c $BUILD_CONFIGURATION -o /output/publish /p:UseAppHost=false

FROM build AS migrations
RUN dotnet tool restore
RUN dotnet ef migrations script --idempotent -p Nellebot.Data.Migrations -o /output/migrations/database_migration.sql
COPY --from=build /src/scripts/nellebot-backup-db.sh /output/migrations/

FROM base AS final
WORKDIR /app
COPY --from=publish /output/publish .
COPY --from=migrations /output/migrations ./migrations/
ENTRYPOINT ["dotnet", "Nellebot.dll"]