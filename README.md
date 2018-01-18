# NGDP
An ugly bot that monitors client builds. Tries to eat as little memory as possible.

Yes, it builds on mono.

## What. How.

This bot connects to IRC servers. Listens to a few commands. HTTP server is an option if you want to proxy downloading files from Blizzard's CDNs.

Note: This needs admin rights on Windows for the HTTP server.

## Command line arguments

- `--conf`, `-c`: Path to the XML configuration file. Default to `conf.xml`

## IRC Commands

`.listen <branch_name>`
Registers the current IRC channel to get notified about builds being pushed to the provided branch.

`.forceupdate <branch_name>`
Forces the bot to scan again the provided branch. This should not be needed since scans are performed every 30 seconds.

`.notify <branch_name>`
Registers the sender of this command to get highlighted when a build is deployed on the provided branch.

`.unnotify <branch_name>`
Does the exact opposite of `.notify`.

`.unload <build_name>`
Frees the provided build, if known and loaded, from memory.

`.downloadfile <build_name> <path/to/file>`
If the provided build is not already loaded, loads it. Otherwise, returns a link that proxies Blizzard's CDN,
unpacking the archive for you so that you end up with an exploitable file. This command is not active if HTTP proxying
was disabled in the settings.

`.listbuilds`
Lists known active builds. This can get lengthy and cause the bot to get kicked out of IRC if it runs for too long.

## XML File

Example configuration file
```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <!-- There can be multiple of these nodes. -->
  <server>
    <user>casbot_devel</user>
    <address>irc.rizon.net</address>
    <port>6660</port>

    <!--
      Each channel can have a `listen-for` attribute, which is a space-separated list
      of branches for which the bot will print out global notifications of a build being pushed. 

      If that attribute does not exist, the bot will not filter out any of its notifications.
      Use * as wildcard.
    -->
    <channel key="12345">your-channel-name</channel>
  </server>

  <proxy>
    <!-- If omitted, HTTP proxying is disabled -->
    <public-domain-name>www.please-suffer.com</public-domain-name>

    <!-- If omitted, defaults to 8080. -->
    <bind-port>8080</bind-port>
    <!-- If omitted, defaults to 0.0.0.0. -->
    <endpoint>0.0.0.0</endpoint>

    <!-- If omitted, defaults to CWD. -->
    <local-mirror-root>./</local-mirror-root>
  </proxy>

  <branch name="wow" description="Retail">
    <auto-download>Wow.exe</auto-download>
    <auto-download>Wow-64.exe</auto-download>
    <auto-download local-name="World of Warcraft">World of Warcraft.app\Contents\MacOS\World of Warcraft</auto-download>
  </branch>

  <branch name="wowt" description="PTR">
    <auto-download>WowT.exe</auto-download>
    <auto-download>WowT-64.exe</auto-download>
    <auto-download local-name="World of Warcraft Test">World of Warcraft Test.app\Contents\MacOS\World of Warcraft</auto-download>
  </branch>

  <branch name="wow_beta" description="Beta">
    <auto-download>WowB.exe</auto-download>
    <auto-download>WowB-64.exe</auto-download>
    <auto-download local-name="World of Warcraft Beta">World of Warcraft Beta.app\Contents\MacOS\World of Warcraft</auto-download>
  </branch>
</configuration>

```
