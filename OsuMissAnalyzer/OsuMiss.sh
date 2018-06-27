shopt -s nocasematch
while read -r line; do
    if [[ $line == OsuDir* ]] ; then
        OsuDir=`echo "$line" | cut -d'=' -f 2`
    elif [[ $line == SongsDir* ]] ; then
        SongsDir=`echo "$line" | cut -d'=' -f 2`
    fi
done < "options.cfg"
    
if [ -n "$OsuDir" -a -z "$SongsDir" ]; then
    SongsDir="$OsuDir/Songs"
fi
unset -v latest
for file in "$SongsDir/*"; do
    [[ $file -nt $latest ]] && latest=$file
done
./OsuMissAnalyzer $file
