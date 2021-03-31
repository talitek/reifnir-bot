# reifnir-bot
Source code for the discord bot Reifnir, made for the Norwegian-English Language Learning Exchange (NELLE).

## Database

#install ef-tools
dotnet tool install dotnet-ef -g

#update dev database
dotnet ef database update -p Nellebot.Data.Migrations

#update to target migration
dotnet ef database update TargetMigration -p Nellebot.Data.Migrations

#add migration
dotnet ef migrations add MigrationName -p Nellebot.Data.Migrations

#remove last migration
dotnet ef migrations remove -p Kattbot.Data.Migrations

#generate database upgrade script
dotnet ef migrations script --idempotent -o database_migration.sql -p Nellebot.Data.Migrations

#connection string format
Server=_DB_SERVER_IP_;Database=_DB_NAME_;User Id=_DB_USER_;Password=_DB_PASSWORD_

## Publish

#publish windows release
dotnet publish -c Release -r win10-x64 --self-contained false

#publish linux release
dotnet publish -c Release -r linux-x64 --self-contained false

## User secrets

dotnet user-secrets list

# Required secrets for local development
dotnet user-secrets set "Nellebot:ConnectionString" "CONN_STRING_GOES_HERE"​
dotnet user-secrets set "Nellebot:BotToken" "TOKEN_GOES_HERE"​

## Bot

# Required bot permissions (Permissions Integer: 335662144)

General: Manage Roles, Change Nickname, View Channels
Text permissions: Send Messages, Embed links, Attach Files, Read Message History, Add reactions