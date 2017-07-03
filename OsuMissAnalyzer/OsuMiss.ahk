;Credit for script goes to Snow
#NoEnv
SetWorkingDir %A_ScriptDir%
#SingleInstance, Force
RegRead,OsuDirRaw,HKEY_LOCAL_MACHINE\SOFTWARE\Classes\osu!\DefaultIcon ;/Found this registry key, give dir of osu
StringSplit,CorrectOsuDir,OsuDirRaw,"" ;Remove to get to correct path to osu!.exe
StringReplace,CurrentDir,CorrectOsuDir2,osu!.exe,Data\r ;Remove the osu!.exe

#IfWinActive osu! ;Script can only work if osu is launched
!r::  ;Alt+R
{
	File := A_Space
	Loop Files, %CurrentDir%\*.osr ;Search every replay file
	{
		CurrentTime := A_Now ;Get current time
		EnvSub, CurrentTime, %A_LoopFileTimeModified%, S ;Compare with time of file
		If CurrentTime <= 60 ;Replay file 1 min ago
		{
			File := A_LoopFileFullPath ;Get the file
			Break
		}
	}
	If File is not Space
		Run, OsuMissAnalyzer.exe %File%
}