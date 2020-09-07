# osu! Miss Analyzer
A program to analyze misses in an osu! replay.

Credit for the beatmap and replay parsing and analysis code goes to [firedigger](https://github.com/firedigger) from his [osu! replay analyzer](https://github.com/firedigger/osuReplayAnalyzer).

## How To Use

First, edit the options.cfg file and specify your osu! directory (the one with osu!.db in it) and/or the songs directory.

After that, you can run the program by double-clicking the icon or dragging a replay file on to the exe. If you didn't specify a replay file manually, you can select from the five most recent replays found in your osu! directory (saved or otherwise). After this, it'll search your osu!db to find the corresponding beatmap, or open a file chooser dialog if  it couldn't find osu!.db.

After it's found the beatmap and replay, it'll analyze the misses and display them in an interactive window.

### Examples

|![](https://github.com/ThereGoesMySanity/osuMissAnalyzer/blob/missAnalyzer/OsuMissAnalyzer.Core/Images/replay-0_658127_2040036498.0.png)|![](https://github.com/ThereGoesMySanity/osuMissAnalyzer/blob/missAnalyzer/OsuMissAnalyzer.Core/Images/replay-0_658127_2283307549.0.png)|
|-|-|
| *Cookiezi's first Blue Zenith choke* | *Cookiezi's second Blue Zenith choke* |

Not shown: In the newest update, the color of the missed hitcircle is tinted red and there are arrows to indicate the direction of movement.

The colored lines represent what the accuracy of the hit would be if you clicked when the cursor was at that point. The circle is also colored to reflect what the accuracy would be (300 is blue, 100 is green, 50 is purple).

### Controls

| Key | Action|
|-|-|
|Up|Zoom in|
|Down|Zoom out|
|Mouse wheel|Zoom in/out|
| Right | Next miss |
| Left | Previous miss |
| T | Draw outlines only |
| P | Save images for each miss |
| R | Select new replay |
| A | Switch between viewing only misses and viewing all objects |

Outlines only example:

![](https://github.com/ThereGoesMySanity/osuMissAnalyzer/blob/missAnalyzer/OsuMissAnalyzer.Core/Images/replay-0_658127_2040036498.1.png)

### Options

In options.cfg, you can define various settings that impact the program.

To add these to options.cfg, add a new line formatted `<Setting Name>=<Value>`

| Setting | Description |
|-|-|
|OsuDir|Specify the osu! directory. Make sure that osu!.db is in here. If the program takes a while to start, please add this option.|
|SongsDir|Specify osu!'s songs dir. Only necessary if it isn't OsuDir/Songs.|
|APIKey|osu! API key|

## Alternate Usage

You can also run it from the command line with this format: `osuMissAnalyzer.exe [<replay> [<beatmap>]]`
