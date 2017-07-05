#NoEnv
#SingleInstance, Force
SetWorkingDir %A_ScriptDir%

;*******************************
;Title: Osu MissAnalyser script
;Author: Snow
;Desc: Press Alt+R after played a beatmap to see where you miss
;Date: 2017
;*******************************

RegRead,OsuDirRaw,HKEY_LOCAL_MACHINE\SOFTWARE\Classes\osu!\DefaultIcon 	;Found this registry key, give dir of osu
RegRead,OsuDirRaw2,HKEY_LOCAL_MACHINE\SOFTWARE\Classes\osu\DefaultIcon 	;Found this registry key, give dir of osu
StringSplit,CorrectOsuDirA,OsuDirRaw,"" 							;Remove to get to correct path to osu!.exe
StringSplit,CorrectOsuDirB,OsuDirRaw2,"" 							;Remove to get to correct path to osu!.exe
CorrectOsuDir2 := CorrectOsuDirA2
If OsuDirRaw != %OsuDirRaw2%
{
	IfExist,%CorrectOsuDirA2%
		CorrectOsuDir2 := CorrectOsuDirA2
	IfExist,%CorrectOsuDirA2%
		CorrectOsuDir2 := CorrectOsuDirB2
}

StringReplace,CurrentDir,CorrectOsuDir2,osu!.exe, 		;Set folder osu
StringReplace,DataDir,CorrectOsuDir2,osu!.exe,Data\r 		;Set data folder
StringReplace,ReplayDir,CorrectOsuDir2,osu!.exe,Replays	;Set replay folder
StringReplace,SongsDir,CorrectOsuDir2,osu!.exe,Songs 		;Set songs folder
StringReplace,OsuDbDir,CorrectOsuDir2,osu!.exe,osu!.db 	;Set songs folder

IfNotExist,OsuMissAnalyzer.exe
{
	MsgBox,16,OsuMissAnalyser don't found!,Make sure the script file is in the same directory as the OsuMissAnalyzer.exe
	ExitApp
}

IfNotExist,DbOsuNullless.data
{
	SetBatchLines -1 								;Max Speed
	Result:=NoNulls(OsuDbDir)						;Remove null character from database file so AHK can read it
	FileAppend, %Result%, DbOsuNullless.data
	SetBatchLines 20 								;Normal Speed
}

SetTimer,Osu,120000

#IfWinActive osu! 									;Script can only work if osu is launched
	
^!r::Reload 										;Reload script why not Alt+Ctrl+R
!r::  											;Alt+R
{
	Filepath := A_Space
	Filename := A_Space
	Loop Files, %DataDir%\*.osr 						;Search every replay file
	{
		CurrentTime := A_Now 						;Get current time
		EnvSub, CurrentTime, %A_LoopFileTimeModified%, S 	;Compare with time of file
		If CurrentTime <= 60 						;Replay file 1 min ago
		{
			Filepath := A_LoopFileFullPath 			;Get the path to file
			FilenameRaw := A_LoopFileName				;Get the file name
			StringSplit,Filename,FilenameRaw,-
			Break
		}
	}
	If Filepath is not Space
	{
		FileDelete,options.cfg						;Delete option.cfg
		FileAppend,SongsDir=%SongsDir%,options.cfg		;Recreate option.cfg with good parameter
		BeatmapPath:=GetBeatmapPath(Filename1)			;Get the beatmap path
		Parameter = "%Filepath%" "%BeatmapPath%"
		Run, OsuMissAnalyzer.exe %Parameter%			;Run Miss
	}
}

Osu:
IfWinNotExist,osu! 									;It's useless to keep this script on if the player don't play osu
{
	FileDelete,DbOsuNullless.data
	ExitApp
}
Return


GetBeatmapPath(Filename){
	Global OsuDbDir
	SetBatchLines -1 								;Max Speed
	IfNotExist,DbOsuNullless.data
	{
		SplashTextOn,200,30,Please Wait...,Generating database...
		Result:=NoNulls(OsuDbDir)
		FileAppend, %Result%, DbOsuNullless.data
		SplashTextOff
	}
	Loop,read,DbOsuNullless.data
	{
		IfInString, A_LoopReadLine,%Filename%			;Search in database the HASH of beatmap
		{
			RegExMatch(A_LoopReadLine, Filename . "(.*).osu", BeatmapNameA)	;Found the string with HASH and take only part with .osu
			StringTrimLeft, Beatmap, BeatmapNameA1, 2					;Removing 2 character in left of this string
			Beatmap := Beatmap . ".osu"
			Break
		}
	}
	Loop,Files, E:\Osu\Songs\*.*, R
	{
		IfInString,A_LoopFileFullPath,%Beatmap%			;Search in Song folder the .osu
		{
			BeatmapPath := A_LoopFileFullPath			;Found it and store it in a var
			Break
		}
	}
	SetBatchLines 20 								; Back to normal
	Return, BeatmapPath								; Return the path of this beatmap
}

NoNulls(Filename) {									;Found in on google can't explain
	f := FileOpen(Filename, "r")
	While Not f.AtEOF {
		If Byte := f.ReadUChar()
			Result .= Chr(Byte)
	}
	f.Close
	Return, Result
}