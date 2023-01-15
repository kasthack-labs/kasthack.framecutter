namespace kasthack.Framecutter.Rendering;

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

using kasthack.Framecutter.Configuration;
public class SDGRenderer : IRenderer
{
    public Stream RenderFrame(MemoryStream readMemoryStream, CuttingOptions options)
    {
        using var sourceImage = Image.FromStream(readMemoryStream);
        using var frameImage = Image.FromFile(options.FramePath);
        using var outputImage = new Bitmap(frameImage.Width, frameImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var outputGraphics = Graphics.FromImage(outputImage))
        {
            outputGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            outputGraphics.SmoothingMode = SmoothingMode.HighQuality;
            outputGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            outputGraphics.CompositingQuality = CompositingQuality.HighQuality;

            // fill target rectangle with the  image
            var configRect = options.TargetRectangle;
            var scale = Math.Max((float)configRect.Width / sourceImage.Width, (float)configRect.Height / sourceImage.Height);
            var dx = configRect.X + (configRect.Width / 2) - (sourceImage.Width / 2 * scale);
            var dy = configRect.Y + (configRect.Height / 2) - (sourceImage.Height / 2 * scale);

            outputGraphics.SetClip(new System.Drawing.Rectangle(configRect.X, configRect.Y, configRect.Width, configRect.Height));
            outputGraphics.TranslateTransform(dx, dy);
            outputGraphics.ScaleTransform(scale, scale);

            outputGraphics.DrawImage(sourceImage, 0, 0, sourceImage.Width, sourceImage.Height);

            outputGraphics.ResetClip();
            outputGraphics.ResetTransform();

            outputGraphics.DrawImage(
                frameImage,
                destRect: new RectangleF(0, 0, frameImage.Width, frameImage.Height),
                srcRect: new RectangleF(0, 0, frameImage.Width, frameImage.Height),
                srcUnit: GraphicsUnit.Pixel);
        }

        var writeMemoryStream = new MemoryStream();
        outputImage.Save(writeMemoryStream, System.Drawing.Imaging.ImageFormat.Png);
        _ = writeMemoryStream.Seek(0, SeekOrigin.Begin);

        return writeMemoryStream;
    }
}