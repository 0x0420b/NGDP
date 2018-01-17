# NGDP
An ugly bot that monitors client builds. Tries to eat as little memory as possible.

Yes, it builds on mono.

## What. How.

This bot connects to IRC servers. Listens to a few commands. HTTP server is an option if you want to proxy downloading files from Blizzard's CDNs.

Note: This needs admin rights on Windows for the HTTP server.

## Command line arguments

- `--conf`, `-c`: Path to the XML configuration file. Default to `conf.xml`

## XML File

Example configuration file
```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <server>
    <user>casbot_dev</user>
    <address>irc.rizon.net</address>
    <port>6660</port>

    <channel key="12345">your-channel-name</channel>
  </server>

  <proxy>
    <!-- If omitted, HTTP proxy is disabled -->
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
