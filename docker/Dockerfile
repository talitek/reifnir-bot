FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Nellebot/Nellebot.csproj", "Nellebot/"]
COPY ["Nellebot.Common/Nellebot.Common.csproj", "Nellebot.Common/"]
COPY ["Nellebot.Data/Nellebot.Data.csproj", "Nellebot.Data/"]
RUN dotnet restore "./Nellebot/Nellebot.csproj"
COPY . .
RUN dotnet build "./Nellebot/Nellebot.csproj"

ENTRYPOINT ["dotnet", "run", "--project", "Nellebot/Nellebot.csproj"]