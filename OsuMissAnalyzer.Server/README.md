# osu! Miss Analyzer Server

A program to analyze misses in an osu! replay, now in discord bot form!

## How to Use

```
Usage:
  >miss {user-recent|user-top} <username> [<index>]
    Finds #index recent/top play for username (index defaults to 1)
  >miss beatmap {<beatmap id>|<beatmap link>} [<index>]
    Finds #index score on beatmap (index defaults to 1)
Automatically responds to rs from owo bot and boat bot if the replay is saved online
Automatically responds to uploaded replay files
```

If you're not familiar with command-line argument notation, <> is something you need to substitute in, [] is optional, and {|} represents multiple options separated by |.

### Bot says "Cannot find replay"!
**All commands except for uploading a replay file will only work if the replay is saved on the osu! servers.**
**Replays are only saved if they are in the top 500 on the leaderboard.**
**If the bot says "Cannot find replay", this is probably why.**

## Info for Server Owners

### Invite link
https://discord.com/oauth2/authorize?client_id=752035690237394944&scope=bot&permissions=100416

### Server-specific Settings
The owner of the server can view/change settings with:
```
>miss settings [<server-id>]
  View current settings
>miss settings [<server-id>] set <setting> <value>
  Set the value of <setting> to <value>
```
|Setting|Description|
|-|-|
|compact|When compact is true, replies to other bots are not sent. Instead, the "miss #" reactions are placed on the original bot's message. (Default: false)|
|prefix|Sets prefix for the bot. (Default: ">miss")|

If you don't want to clutter your server with settings configuration, you can DM the bot instead and specify the server id of the server you want to edit.

## Donate

Right now, I have the bot running on a home server. This works fine for now, but if the bot gets super popular it might not be sustainable.
It'd be nice to run the bot on a dedicated server but I'm not gonna throw money at one just so other people can use my bot.

If anyone wants to put money towards getting a proper server host, please [let me know](#contact) - I'd be really grateful.


## Contact

`ThereGoesMySanity#2622` on discord for any questions/concerns/suggestions/donations.
