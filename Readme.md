# ReifnirBot

## About
Source code for the discord bot Reifnir, made for the Norwegian-English Language Learning Exchange (NELLE).

[Join NELLE discord!](https://discord.gg/2d37xPa)

## Bot permissions

General: Manage Roles, Change Nickname, View Channels

Text permissions: Send Messages, Embed links, Attach Files, Read Message History, Add reactions

(Permissions Integer: 335662144)

## Local developement

### Requirements

* [.NET 5 SDK](https://dotnet.microsoft.com/download/visual-studio-sdks)
* [Visual Studio](https://visualstudio.microsoft.com/) (or any other preferred editor + dotnet command line tool)
* [PostgreSQL 12+](https://www.postgresql.org/)

### Build from Visual Studio

Build solution

### Build from dotnet command line tool

`dotnet build`

### Connection string format
`Server=_DB_SERVER_IP_;Database=_DB_NAME_;User Id=_DB_USER_;Password=_DB_PASSWORD_;`

### Secrets
Connection string 

`"Nellebot:ConnectionString" "CONN_STRING_GOES_HERE"​`

Bot token

`"Nellebot:BotToken" "TOKEN_GOES_HERE"​`

## Credits

Reifnir is built on top of [DSharp+](https://dsharpplus.github.io/index.html)