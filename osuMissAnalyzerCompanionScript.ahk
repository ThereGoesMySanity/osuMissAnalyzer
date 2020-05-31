;  .----------------------------------------------------------------------------------.
; /  .-.                                                                          .-.  \
;|  /   \                                                                        /   \  |
;| |\_.  |  osu! Miss Analyzer Companion AHK Script                             |    /| |
;|\|  | /|  https://github.com/ceilingwaffle/osuMissAnalyzer                    |\  | |/|
;| `---' |                                                                      | `---' |
;|       |  HOW IT WORKS                                                        |       | 
;|       |  - See DigitalHypno's reddit comment: reddit.com/comments/8l43p0     |       | 
;|       |                                                                      |       | 
;|       |  HOW TO USE THIS FILE                                                |       | 
;|       |  - Modify the values below to your own settings                      |       | 
;|       |  - Open this file with AutoHotKey                                    |       | 
;|       |  https://www.autohotkey.com/docs/Tutorial.htm                        |       | 
;|       |                                                                      |       | 
;|       |  HOW TO ANALYZE YOUR MISSES                                          |       | 
;|       |  - Create a new osu! multiplayer lobby                               |       | 
;|       |  - Select a map to play                                              |       | 
;|       |  - Press your 'StartMapKey' binding defined below                    |       | 
;|       |  - When you miss, press your 'EndMapKey' binding                     |       | 
;|       |  - Press your 'ToggleMissAnalyzer' binding to show OsuMissAnalyzer   |       | 
;|       |  - Use the L/R arrow keys to check your misses                       |       | 
;|       |  - Press your 'ToggleMissAnalyzer' binding to close OsuMissAnalyzer  |       | 
;|       |                                                                      |       | 
;|       |  AHK SCRIPT CHANGELOG                                                |       | 
;|       |     v0.1.0:                                                          |       | 
;|       |     - Initial release (expect bugs)                                  |       | 
;|       |----------------------------------------------------------------------|       |
;\       |                                                                      |       /
; \     /                                                                        \     /
;  `---'                                                                          `---'

GoSub, DeclareHotKeys
;-------------------------------------
; Shortcut Key Bindings
; For a list of keys, see: https://www.autohotkey.com/docs/KeyList.htm
;-------------------------------------
DefineShortcutKeyBindings:
	ToggleMissAnalyzer = Scrolllock ; Press this key once to launch and focus the analyzer, then again to close it
	StartMapKey = PgUp
	EndMapKey = PgDn
return
;-------------------------------------
; osu! Key Bindings
; These should match your key bindings defined in osu!
;-------------------------------------
DefineOsuKeyBindings:
	ExportReplayKey = F2
	ToggleChatKey = F8
return
;-------------------------------------
; osu! Miss Analyzer Configuration
; Download here: https://github.com/ceilingwaffle/osuMissAnalyzer/releases/latest
; Define your osu! directory location and osu! API key in options.cfg
;-------------------------------------
DefineConfig:
	OsuMissAnalyzerPath := "O:\apps\OsuMissAnalyzer" ; path of the folder containing OsuMissAnalyzer.exe
	ShowAnalyzerOnSecondMonitor := 1 ; 1 (true) or 0 (false)
	OsuMonitorResolutionWidth := 1920 ; Width in pixels of the monitor you use for osu!
	SecondMonitorLocationFromOsu := "right" ; left/right
return


;!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
;!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
;
; Do not edit below this line (unless you know what you're doing :D)
;
;!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
;!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
DeclareHotKeys:
	GoSub, DefineShortcutKeyBindings
	HotKey, %ToggleMissAnalyzer%, ToggleMissAnalyzerAction
	HotKey, %EndMapKey%, EndMapAction
	HotKey, %StartMapKey%, StartMapAction
return

ToggleMissAnalyzerAction:
	GoSub, DefineConfig
	Process, Exist, OsuMissAnalyzer.exe
	If (ErrorLevel = 0) ;If OsuMissAnalyzer.exe is not running, run it
	{
		Run, OsuMissAnalyzer.exe, %OsuMissAnalyzerPath%
		WinWait, Miss Analyzer
		{
			Send {Enter}
		} 
		If (ShowAnalyzerOnSecondMonitor = 1) ;Move the window to the second monitor
		{
			WinWait, Miss Analyzer
			{
				WinGetPos, Xpos, Ypos  ;Get position of OsuMissAnalyzer
				IfLess, Xpos, OsuMonitorResolutionWidth - 8  ;This value needs to be 8 pixels less than the monitor width because when a window is maximized, it hangs over by 8 pixels.
				{
					Xposplus := 0
					If (SecondMonitorLocationFromOsu = "left")
						Xposplus := (Xpos - OsuMonitorResolutionWidth)  ;Xpos plus width of monitor
					Else If (SecondMonitorLocationFromOsu = "right")
						Xposplus := (Xpos + OsuMonitorResolutionWidth)  ;Xpos plus width of monitor
					WinMove, %Xposplus%, %Ypos%  ;Moves window
				}
			}
		}
		return
	}
	Else ; If it is running, close it.
	{
		Process, Close, %ErrorLevel% ; close the OsuMissAnalyzer 
		
		; switch back to osu!
		Processname=osu!.exe
		Process, Exist, %Processname%
		If !ErrorLevel ; do nothing if process not running
		{
			return
		}
		pid := ErrorLevel
		IfWinNotActive, % "ahk_pid " pid
		{
			WinActivate, % "ahk_pid " pid
		}
		return
	}
return

; https://www.reddit.com/r/osugame/comments/8l43p0/how_to_save_failed_replays_how_to_use_solo_multi/

; Start lobby
StartMapAction:
	Processname=osu!.exe
	Process, Exist, %Processname%
	If !ErrorLevel ; do nothing if process not running
	{
		return
	}
	pid := ErrorLevel
	IfWinActive, % "ahk_pid " pid
	{
		GoSub, DefineOsuKeyBindings
		Send {!}mp start
		Sleep, 10
		Send {Enter down}
		Sleep, 5
		Send {Enter up}
		Sleep, 1000
		Send, {%ToggleChatKey% down} ; hide chat
		Sleep, 1000
		Sleep, 5
		Send, {%ToggleChatKey% up}
		return
	}
return

; Abort lobby and export replay
EndMapAction:
	Processname=osu!.exe
	Process, Exist, %Processname%
	If !ErrorLevel ; do nothing if process not running
	{
		return
	}
	pid := ErrorLevel
	IfWinActive, % "ahk_pid " pid
	{
		GoSub, DefineOsuKeyBindings
		Send, {%ToggleChatKey% down} ; show chat
		Sleep, 5
		Send, {%ToggleChatKey% up}
		Sleep, 50
		Send, {%ToggleChatKey% down}
		Sleep, 5
		Send, {%ToggleChatKey% up}
		Sleep, 500
		Send {!}mp abort ; send !mp abort command
		Sleep, 10
		Send {Enter down}
		Sleep, 5
		Send {Enter up}
		Sleep, 10
		Send, {%ToggleChatKey% down} ; hide chat
		Sleep, 5
		Send, {%ToggleChatKey% up}
		Sleep, 800
		Send, {%ExportReplayKey% down} ; export replay
		Sleep, 5
		Send, {%ExportReplayKey% up}
		Sleep, 300
		Send, {Esc down} ; return to lobby
		Sleep, 5
		Send, {Esc up}
		return
	}
return