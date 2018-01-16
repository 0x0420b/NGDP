# NGDP
An ugly bot that monitors client builds. Tries to eat as little memory as possible.

Yes, it builds on mono.

## What. How.

This bot connects to IRC servers. Listens to a few commands. HTTP server is an option if you want to proxy downloading files from Blizzard's CDNs.

Note: This needs admin rights on Windows for the HTTP server.

## Command line arguments

- `--conf`, `-c`: Path to the XML configuration file. Default to `conf.xml`
- `--autodownload`, `-a`: Path to a txt file of game files to download.
- `--httpDomain`, `-d`: Domain name for the bot's public HTTP server to be reached at.
- `--bindAddr`, `-b`: The endpoint to bind on. Defaults to `0.0.0.0`.
- `--bindPort`, `-p`: The port to bind on. Defaults to `8080`.
- `--hasHttp`, `-h`: Activates the HTTP server. Implied by `--bindAddr` or `--bindPort`.

## XML File

Example configuration file
```xml
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <!-- Multiple servers can be defined -->
  <server>
    <user>casbot_dev</user>
    <address>irc.rizon.net</address>
    <port>6660</port>

    <channel>
      <name>your-channel-name</name>
      <!-- If the channel does not need a key, remove this node -->
      <key>your-channel-key</name>
    </channel>
  </server>

  <!-- Define a new set of branches by just copy-pasting this block and fixing what's inside. -->
  <branch>
    <name>endpoint-name</name>
    <description>simple-endpoint-description</description>
  </branch>
</configuration>
```
