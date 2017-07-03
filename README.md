# osu! Miss Analyzer
A program to analyze misses in an osu! replay.

Credit for the beatmap and replay parsing and analysis code goes to [firedigger](https://github.com/firedigger) from his [osu! replay analyzer](https://github.com/firedigger/osuReplayAnalyzer).

## How To Use

Run the program by double-clicking the icon or dragging a replay file on to the exe. If you didn't drag a replay file, it'll ask you to select one with a file dialog. Once you do this, the program will search its current directory and all subdirectories for any .osu files whose hash matches the hash specified in the replay file. After this it will search the Songs directory specified in your options.cfg file (see below). If it doesn't find any beatmaps with a matching hash, it'll ask you to manually select the beatmap file.

After you select both of these, it'll analyze the misses and display them in an interactive window.

### Examples

|![](https://github.com/ThereGoesMySanity/osuMissAnalyzer/blob/missAnalyzer/OsuMissAnalyzer/Images/replay-0_658127_2040036498.0.png)|![](https://github.com/ThereGoesMySanity/osuMissAnalyzer/blob/missAnalyzer/OsuMissAnalyzer/Images/replay-0_658127_2283307549.0.png)|
|-|-|
| *Cookiezi's first Blue Zenith choke* | *Cookiezi's second Blue Zenith choke* |

Not shown: In the newest update, the color of the missed hitcircle is tinted red.

The colored lines represent what the accuracy of the hit would be if you clicked when the cursor was at that point. The circle is also colored to reflect what the accuracy would be.

This is standard osu! coloring (300 is blue, 100 is green, 50 is purple).

### Controls

| Key | Action|
|-|-|
| Right | Next miss |
| Left | Previous miss |
| T | Draw outlines only |
| P | Save images for each miss |
| R | Select new replay|

Outlines only example:

![](https://github.com/ThereGoesMySanity/osuMissAnalyzer/blob/missAnalyzer/OsuMissAnalyzer/Images/replay-0_658127_2040036498.1.png)

### Options

In options.cfg, you can define various settings that impact the program.

To add these to options.cfg, add a new line formatted `<Setting Name>=<Value>`

| Setting | Description |
|-|-|
|SongsDir|Specify osu!'s songs dir.|
|Size| Specify the width/height of the window.|

## Alternate Usage

You can also run it from the command line with this format: `osuMissAnalyzer.exe [<replay> [<beatmap

## AutoHotKey script

The [AutoHotKey script](https://github.com/ThereGoesMySanity/osuMissAnalyzer/blob/missAnalyzer/OsuMissAnalyzer/OsuMiss.ahk) in the repository lets you view your most recent replay.

The way it works is that after you complete a map, if you have the script running, you can hit Alt+R and it'll open that replay. Make sure the script file is in the same directory as the OsuMissAnalyzer.exe.
