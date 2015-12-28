[<image src="https://ci.appveyor.com/api/projects/status/7tdp1nrwatndex5r/branch/master?svg=true">](https://ci.appveyor.com/project/jackpoz/botfarm)

Console application used to spawn multiple WoW automated players.

Credit for the authentication process and the base implementation goes to mangos, WCell, PseuWoW and TrinityCore.

The application configuration is setup by editing the *.config file in the output folder:
- fill the [Username](BotFarm/App.config#L17) and [Password](BotFarm/App.config#L20) fields with an account with ".account create" permissions to allow the BotFarm to automatically create new accounts when required. Make sure this account has already a character.
- fill the [Min](BotFarm/App.config#L35)/[MaxBotsCount](BotFarm/App.config#L32) with the amount of bots you want to connect at same time. This will automatically create new accounts when required
- set paths for [MAPsFolderPath](BotFarm/App.config#L44), [VMAPsFolderPath](BotFarm/App.config#L41), [MMAPsFolderPath](BotFarm/App.config#L38) and [DBCsFolderPath](BotFarm/App.config#L47)
- make sure to read [README.md in BotFarm/lib folder](BotFarm/lib/README.md) too with additional steps about required dependencies

Write "stats" in the console to view some statistics about the current status of bots.
Write "quit" to cleanly shut down the application and persist the new connection infos to botsinfos.xml file.

Windows x64 binaries built with Visual Studio 2015 can be downloaded at [https://ci.appveyor.com/project/jackpoz/botfarm/build/artifacts](https://ci.appveyor.com/project/jackpoz/botfarm/build/artifacts), make sure to install [Visual C++ Redistributable for Visual Studio 2015](https://www.microsoft.com/en-US/download/details.aspx?id=48145) first.
