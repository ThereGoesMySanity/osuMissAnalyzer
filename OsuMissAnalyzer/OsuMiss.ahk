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
		FileGetTime, Time, %A_LoopFileFullPath%, C

		If (Time > Time_Orig)

		{
			Time_Orig := Time
			File := A_LoopFileFullPath
		}
	}
	If File is not Space
		Run, OsuMissAnalyzer.exe %File%
}