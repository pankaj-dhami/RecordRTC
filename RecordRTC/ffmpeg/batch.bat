bin\ffmpeg -i 3.mp4 -qscale:v 1 intermediate1.mpg
bin\ffmpeg -i 3.wav -qscale:v 1 intermediate2.mpg
bin\ffmpeg -i 7.mp4 -qscale:v 1 intermediate3.mpg
bin\ffmpeg -i 7.wav -qscale:v 1 intermediate4.mpg
bin\ffmpeg -y -i concat:"intermediate1.mpg|intermediate2.mpg|intermediate3.mpg|intermediate4.mpg" -c copy intermediate_all.mpg
bin\ffmpeg -y -i intermediate_all.mpg -qscale:v 2 output.avi
echo y | del *.mpg*