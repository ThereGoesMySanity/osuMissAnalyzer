# osu! Miss Analyzer Server

[![Discord Bots](https://top.gg/api/widget/752035690237394944.svg)](https://top.gg/bot/752035690237394944)


A program to analyze misses in an osu! replay, now in discord bot form!

## How to Use

### **!! Reinvite the bot if slash commands don't work, it might need new permissions !!**

```
Usage:
  /help
    Prints this message.
  /miss user <username> <type> [<index>]
    Finds #index recent/top play for username (index defaults to 1)
  /miss beatmap {<beatmap id>|<beatmap link>} [<index>]
    Finds #index score on beatmap (index defaults to 1)
Automatically responds to rs from owo bot and boat bot if the replay is saved online
Automatically responds to uploaded replay files
```

Discord's slash command guides should help you figure out what to put where.

### Bot says "Cannot find replay"!
**All commands except for uploading a replay file will only work if the replay is saved on the osu! servers.**
**Replays are only saved if they are in the top 500 on the leaderboard.**
**If the bot says "Cannot find replay", this is probably why.**

## Info for Server Owners

### Invite link
https://discord.com/api/oauth2/authorize?client_id=752035690237394944&permissions=3136&scope=bot%20applications.commands

### Server-specific Settings
The owner of the server can view/change settings with:
```
/settings get
  View current settings
/settings set <setting> <value>
  Set the value of <setting> to <value>
```
|Setting|Description|
|-|-|
|compact|When compact is true, replies to other bots are not sent. Instead, the "miss #" reactions are placed on the original bot's message. (Default: false)|
|prefix|Sets prefix for the bot. (Default: ">miss") **(OBSOLETE: Use slash commands instead!)**|
|tracking|(currently TinyBot only) Whether the bot responds to "tracking" messages from other bots that are automatically posted when a user sets a new top play. (Default: false)|
|autoresponses|Whether the bot responds to rs messages posted by other bots (default: true)|
|maxbuttons|The max number of buttons the bot will put on a miss message. Misses higher than this number won't be viewable. (default 10, max 25)|
|colorscheme|Changes the program's color scheme. Accepted values are Default or Dark|

~~If you don't want to clutter your channels with settings configuration, you can DM the bot instead and specify the server id of the server you want to edit.~~
Removed now that bot uses slash commands.

## Donate

Right now, I have the bot running on a home server. This works fine for now, but if the bot gets super popular it might not be sustainable.
It'd be nice to run the bot on a dedicated server but I'm not gonna throw money at one just so other people can use my bot.

If anyone wants to put money towards getting a proper server host, please [let me know](#contact) - I'd be really grateful.


## Contact

`ThereGoesMySanity#2622` on discord for any questions/concerns/suggestions/donations.
