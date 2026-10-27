using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ImageEditor.ImageProcessing
{
    public static partial class BasicImageProcessor
    {
        public static Bitmap Adjust(
            Bitmap image,
            int brightness,
            int contrast,
            int saturation,
            int temperature)
        {
            // 픽셀마다 GDI+ 호출을 하는 GetPixel/SetPixel 대신 LockBits를 사용한다.
            // 기존 보정 수식과 출력 해상도는 그대로 유지한다.
            using Bitmap source = new Bitmap(
                image.Width,
                image.Height,
                PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(source))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.DrawImageUnscaled(image, 0, 0);
            }

            Bitmap result = new Bitmap(
                image.Width,
                image.Height,
                PixelFormat.Format32bppArgb);

            double brightnessOffset = brightness * 2.55;
            double contrastValue = contrast * 2.55;

            double contrastFactor =
                (259 * (contrastValue + 255)) /
                (255 * (259 - contrastValue));

            double saturationFactor =
                1.0 + saturation / 100.0;

            double temperatureOffset =
                temperature * 1.25;

            Rectangle bounds = new Rectangle(0, 0, image.Width, image.Height);
            BitmapData sourceData = source.LockBits(
                bounds,
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            BitmapData resultData = result.LockBits(
                bounds,
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                int rowBytes = image.Width * 4;
                byte[] sourceRow = new byte[rowBytes];
                byte[] resultRow = new byte[rowBytes];

                for (int y = 0; y < image.Height; y++)
                {
                    IntPtr sourceScan = IntPtr.Add(
                        sourceData.Scan0,
                        y * sourceData.Stride);
                    IntPtr resultScan = IntPtr.Add(
                        resultData.Scan0,
                        y * resultData.Stride);
                    Marshal.Copy(sourceScan, sourceRow, 0, rowBytes);

                    for (int offset = 0; offset < rowBytes; offset += 4)
                    {
                        double b = sourceRow[offset];
                        double g = sourceRow[offset + 1];
                        double r = sourceRow[offset + 2];

                        r += brightnessOffset;
                        g += brightnessOffset;
                        b += brightnessOffset;

                        r = contrastFactor * (r - 128) + 128;
                        g = contrastFactor * (g - 128) + 128;
                        b = contrastFactor * (b - 128) + 128;

                        double gray =
                            0.299 * r +
                            0.587 * g +
                            0.114 * b;

                        r = gray + (r - gray) * saturationFactor;
                        g = gray + (g - gray) * saturationFactor;
                        b = gray + (b - gray) * saturationFactor;

                        r += temperatureOffset;
                        b -= temperatureOffset;

                        resultRow[offset] = ClampToByte(b);
                        resultRow[offset + 1] = ClampToByte(g);
                        resultRow[offset + 2] = ClampToByte(r);
                        resultRow[offset + 3] = sourceRow[offset + 3];
                    }

                    Marshal.Copy(resultRow, 0, resultScan, rowBytes);
                }
            }
            finally
            {
                source.UnlockBits(sourceData);
                result.UnlockBits(resultData);
            }

            return result;
        }

        private static byte ClampToByte(double value)
        {
            if (value < 0)
                return 0;

            if (value > 255)
                return 255;

            return (byte)Math.Round(value);
        }
        }
}
