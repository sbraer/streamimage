using Microsoft.IO;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;

namespace StreamImage
{
    public interface IImageCreator : IDisposable
    {
        MemoryStream CreateImage();
    }

    public class ImageCreator : IImageCreator
    {
        const int FONT_SIZE = 108;
        const int MAXX = 1920;
        const int MAXY = 1080;
        const int CLOCK_RADIUS = 400;
        const int CLOCK_DIAMETER = CLOCK_RADIUS * 2;
        const int CLOCK_X = MAXX / 2 - CLOCK_RADIUS / 2;
        const int CLOCK_Y = 30;
        const int SECHAND = 370;
        const int MINHAND = 295;
        const int HRHAND = 220;
        const int CX = CLOCK_RADIUS / 2;
        const int CY = CLOCK_DIAMETER / 2;

        const string BACKGROUND_COLOR = "00bb55";
        const string TEXT_COLOR = "ffffff";
        const string CLOCK_COLOR = "ffffff";
        const string FONT_NAME = "Tahoma"; // <- Check if exist in destination

        private readonly IHelper _helper;
		private readonly RecyclableMemoryStreamManager _memoryStreamManager;
		private readonly SKPaint _textPaint;
        private readonly SKPaint _circlePaint;
        private readonly SKPaint _linePaint;

        private ImageCreator() => throw new NotSupportedException();

        public ImageCreator(in IHelper helper, RecyclableMemoryStreamManager memoryStreamManager)
        {
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
			_memoryStreamManager = memoryStreamManager ?? throw new ArgumentNullException(nameof(memoryStreamManager));

            _textPaint = new SKPaint
            {
                Typeface = SKTypeface.FromFamilyName(FONT_NAME),
                TextSize = FONT_SIZE,
                Color = SKColor.Parse(TEXT_COLOR),
                TextAlign = SKTextAlign.Center
            };

            _circlePaint = new SKPaint
            {
                Color = SKColor.Parse(CLOCK_COLOR),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 4
            };

            _linePaint = new SKPaint
            {
                Color = SKColor.Parse(TEXT_COLOR),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3
            };
        }

        public MemoryStream CreateImage()
        {
            var imageInfo = new SKImageInfo(
                width: MAXX,
                height: MAXY,
                colorType: SKColorType.Rgba8888,
                alphaType: SKAlphaType.Premul);

            using var surface = SKSurface.Create(imageInfo);
            using var canvas = surface.Canvas;
            canvas.Clear(SKColor.Parse(BACKGROUND_COLOR));

            var dt = _helper.GetDateTime();
            string text = $"{dt:dd/MM/yyyy HH:mm:ss} UTC";

            DrawText(canvas, text);
            DrawClock(canvas, dt.Hour, dt.Minute, dt.Second);
            DrawDebugText(canvas);

            using SKImage image = surface.Snapshot();
            using SKData data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
            return _memoryStreamManager.GetStream(data.ToArray());
        }

        private void DrawText(in SKCanvas canvas, in string text)
        {
            canvas.DrawText(text, MAXX / 2, MAXY - FONT_SIZE, _textPaint);
        }

        [Conditional("DEBUG")]
        private void DrawDebugText(in SKCanvas canvas)
        {
            canvas.DrawText("DEBUG", MAXX / 6, FONT_SIZE, _textPaint);
        }

        // https://www.c-sharpcorner.com/blogs/how-to-create-analog-clock-with-c-sharp
        private void DrawClock(in SKCanvas canvas, in int hour, in int minute, in int second)
        {
            //draw a circle
            canvas.DrawCircle(CLOCK_X + CX, CLOCK_Y + CY, CLOCK_RADIUS, _circlePaint);

            //draw seconds hand  
            (int x, int y) cord = MsCoord(second, SECHAND);
            canvas.DrawLine(CLOCK_X + CX, CLOCK_Y + CY, CLOCK_X + cord.x, CLOCK_Y + cord.y, _linePaint);
            //draw minutes hand  
            cord = MsCoord(minute, MINHAND);
            canvas.DrawLine(CLOCK_X + CX, CLOCK_Y + CY, CLOCK_X + cord.x, CLOCK_Y + cord.y, _linePaint);
            //draw hours hand  
            cord = HrCoord(hour % 12, minute, HRHAND);
            canvas.DrawLine(CLOCK_X + CX, CLOCK_Y + CY, CLOCK_X + cord.x, CLOCK_Y + cord.y, _linePaint);
        }

        //coord for minute and second  
        private (int x, int y) MsCoord(in int val, in int hlen)
        {
            int x, y;
            int valx = val * 6; // note: each minute and seconds make a 6 degree  
            if (valx >= 0 && valx <= 100)
            {
                x = CX + (int)(hlen * Math.Sin(Math.PI * valx / 180));
                y = CY - (int)(hlen * Math.Cos(Math.PI * valx / 180));
            }
            else
            {
                x = CX - (int)(hlen * -Math.Sin(Math.PI * valx / 180));
                y = CY - (int)(hlen * Math.Cos(Math.PI * valx / 180));
            }

            return (x, y);
        }

        //coord for hour  
        private (int x, int y) HrCoord(in int hval, in int mval, in int hlen)
        {
            int x, y;
            //each hour makes 60 degree with min making 0.5 degree  
            int val = (int)((hval * 30) + (mval * 0.5));
            if (val >= 0 && val <= 180)
            {
                x = CX + (int)(hlen * Math.Sin(Math.PI * val / 180));
                y = CY - (int)(hlen * Math.Cos(Math.PI * val / 180));
            }
            else
            {
                x = CX - (int)(hlen * -Math.Sin(Math.PI * val / 180));
                y = CY - (int)(hlen * Math.Cos(Math.PI * val / 180));
            }

            return (x, y);
        }

        public void Dispose()
        {
            _textPaint?.Dispose();
            _circlePaint?.Dispose();
            _linePaint?.Dispose();
        }
    }
}
