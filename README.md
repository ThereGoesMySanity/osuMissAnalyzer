

# osu! Miss Analyzer
<a href="https://github.com/ThereGoesMySanity/osuMissAnalyzer/releases/latest"><img alt="GitHub all releases" src="https://img.shields.io/github/downloads/ThereGoesMySanity/osuMissAnalyzer/total"></a>

A program to analyze misses in an osu! replay.

Credit for the beatmap and replay parsing and analysis code goes to [firedigger](https://github.com/firedigger) from his [osu! replay analyzer](https://github.com/firedigger/osuReplayAnalyzer).

### [Discord bot readme](OsuMissAnalyzer.Server/README.md)

## How To Use

First, edit the options.cfg file and specify your osu! directory (the one with osu!.db in it) and/or the songs directory.

After that, you can run the program by double-clicking the icon or dragging a replay file on to the exe. If you didn't specify a replay file manually, you can select from the five most recent replays found in your osu! directory (saved or otherwise). After this, it'll search your osu!db to find the corresponding beatmap, or open a file chooser dialog if  it couldn't find osu!.db.

After it's found the beatmap and replay, it'll analyze the misses and display them in an interactive window.

### Examples

|![](https://github.com/ThereGoesMySanity/osuMissAnalyzer/blob/missAnalyzer/OsuMissAnalyzer.Core/Images/2040036498.0.png)|![](https://github.com/ThereGoesMySanity/osuMissAnalyzer/blob/missAnalyzer/OsuMissAnalyzer.Core/Images/2283307549.0.png)|
|-|-|
| *chocomint's first Blue Zenith choke* | *chocomint's second Blue Zenith choke* |

The line is your cursor movement. The arrows are the direction of the cursor. The color changes show the hit window of the focused note. 
The small hollow circles along the line are the points where there was a click. These are also colored to reflect the note's hit windows (300 is blue, 100 is green, 50 is purple).
The tiny red circle is placed at the center of the 300 hit window - perfect accuracy.

### Controls

| Key | Action|
|-|-|
|Up Arrow/Scroll Up|Zoom in|
|Down Arrow/Scroll Down|Zoom out|
| Right Arrow/Left Mouse | Next miss |
| Left Arrow/Right Mouse | Previous miss |
| T | Draw outlines only |
| P | Save images for each miss |
| R | Select new replay |
| A | Switch between viewing only misses and viewing all objects |

Outlines only example:

![](https://github.com/ThereGoesMySanity/osuMissAnalyzer/blob/missAnalyzer/OsuMissAnalyzer.Core/Images/d90294bf796a0162aa7f03eee87838bf-132904256761690800.3.png)

### Options

In options.cfg, you can define various settings that impact the program.

To add these to options.cfg, add a new line formatted `<Setting Name>=<Value>`

| Setting | Description |
|-|-|
|OsuDir|Specify the osu! directory. Make sure that osu!.db is in here. If the program takes a while to start, please add this option.|
|SongsDir|Specify osu!'s songs dir. Only necessary if it isn't OsuDir/Songs.|
|APIKey|osu! API key|
|WatchDogMode|Enable watch dog mode, in which newest replays in OsuDir are loaded as they appear.|

## Alternate Usage

You can also run it from the command line with this format: `osuMissAnalyzer.exe [<replay> [<beatmap>]]`
