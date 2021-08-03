# streamimage
MJPEG to Youtube (test)

1 - Run this program
dotnet StreamImage.dll

2 - start ffmpeg to stream in youtube
ffmpeg -reconnect 1 -reconnect_at_eof 1 -reconnect_streamed 1 -reconnect_delay_max 2 \ 
 -rtbufsize 200M \
 -f mjpeg -use_wallclock_as_timestamps true -i http://localhost:4003 \
 -f lavfi -re -i anullsrc \
 -vsync cfr -r 30 -c:v libx264 -crf 24 \
 -f flv rtmp://x.rtmp.youtube.com/live2/xxxx-xxxx-xxxx-xxxx-xxxx

More info in my blog:
https://blogs.aspitalia.com/az/