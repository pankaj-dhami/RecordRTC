echo y | del merged\*.mp4*
bin\ffmpeg -y -i "uploads\3.mp4" -qscale:v 1 3.mp4.mpg
bin\ffmpeg -y -i "uploads\3.wav" -qscale:v 1 3.wav.mpg
bin\ffmpeg -y -i "uploads\7.mp4" -qscale:v 1 7.mp4.mpg
bin\ffmpeg -y -i "uploads\7.wav" -qscale:v 1 7.wav.mpg
bin\ffmpeg -y -i "uploads\11.mp4" -qscale:v 1 11.mp4.mpg
bin\ffmpeg -y -i "uploads\11.wav" -qscale:v 1 11.wav.mpg
bin\ffmpeg -y -i concat:"3.mp4.mpg|3.wav.mpg|7.mp4.mpg|7.wav.mpg|11.mp4.mpg|11.wav.mpg" -c copy intermediate_all.mpg
bin\ffmpeg -y -i intermediate_all.mpg -qscale:v 2 merged\output.mp4
echo y | del *.mpg*
echo y | del uploads\*.wav*
echo y | del uploads\*.mp4*
