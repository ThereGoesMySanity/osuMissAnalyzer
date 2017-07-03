^!r::Reload
Time := 999999
File := ""
!r::
{
	Loop, Files, C:\Users\willm\AppData\Local\osu!\Replays\*.osr
	{
		t := A_Now
		EnvSub, t, A_LoopFileTimeModified, S
		if t < Time
		{
			Time := t
			File := A_LoopFileFullPath
		}
	}
	Run, OsuMissAnalyzer.exe "%File%"
}