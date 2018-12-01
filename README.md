# Build Tools Downloader #

Build Tools Downloader (BTD) is used to automatically download Build Tools JAR file from Spigot, compile it, and (optionally) run Minecraft. It will also report this information to a text file so that that information can be used to monitor the health of the server as well as the current build of Spigot.

I originally created this project to monitor my Minecraft server from my personal website at [NoahW.org/Minecraft](https://www.noahw.org/minecraft/).

## Requirements ##

* A Computer capable of running .NET Core 2.0+
* The [.NET Core runtime](https://dotnet.microsoft.com/download)
* [GIT](https://git-scm.com/downloads)
	* Windows clients will also need to install GIT Bash
* The [Java Runtime Environment](https://java.com/en/download/) (JRE) to run BuildTools.jar

## Setup Instructions ##

Formal setup instructions coming soon. Most information needed to get the application up and running can be found by modifying the configuration file included with the application.

**NOTE:** I have tested this application running on a Windows client only, running this on macOS or a Linux host may require additional steps.

## Sample Configuration Information ##

```json
{
  
  "BuildInfo": "https://hub.spigotmc.org/jenkins/view/RSS/job/Spigot-RSS/lastSuccessfulBuild/api/xml",
  "BuildTools": "https://hub.spigotmc.org/jenkins/job/BuildTools/lastSuccessfulBuild/artifact/target/BuildTools.jar",
  "CurrentBuild": 306,
  "Bash": "C:\\Program Files\\Git\\bin\\bash.exe",
  "BuildLocation": "C:\\",
  "BuildFolderFormat": "MC",
  "RequireUniqueDir": false,
  "SpigotLocation": "C:\\Minecraft Server\\spigot.jar",
  "StartMinecraft": true,
  "CurrentBuildFolder": "C:\\MC",
  "JavaArguments": "-Xincgc -Xms1024M -Xmx3072M -XX:MaxPermSize=128M -jar",
  "BuildToolsArguments": "",
  "AlwaysCheckForNewBuild": false,
  "BuildLog": "build.txt",
  "TimeToReload": 5
}
```