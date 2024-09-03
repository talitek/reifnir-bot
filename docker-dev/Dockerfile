FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN mkdir /keydata && \
    chown $APP_UID:$APP_UID /keydata

WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Nellebot/Nellebot.csproj", "Nellebot/"]
COPY ["Nellebot.Common/Nellebot.Common.csproj", "Nellebot.Common/"]
COPY ["Nellebot.Data/Nellebot.Data.csproj", "Nellebot.Data/"]
RUN dotnet restore "./Nellebot/Nellebot.csproj"
COPY . .
RUN dotnet build "./Nellebot/Nellebot.csproj" --no-restore

USER $APP_UID

ENTRYPOINT ["dotnet", "run", "--project", "Nellebot/Nellebot.csproj", "--no-build"]
#ENTRYPOINT ["/bin/bash", "-c", "tail -f /dev/null"]