using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ImageEditor.Core
{
    public partial class EditorController
    {
        public Bitmap? SelectionMask { get; private set; }

        private string lastSelectionEffect = string.Empty;
        private Bitmap? lastSelectionEffectMask;
        private Bitmap? lastSelectionEffectOriginalLayer;
        private int lastSelectionEffectLayerIndex = -1;

        public bool CanSwapLastEffect =>
            !string.IsNullOrWhiteSpace(lastSelectionEffect) &&
            lastSelectionEffectMask != null &&
            lastSelectionEffectOriginalLayer != null &&
            lastSelectionEffectLayerIndex >= 0 &&
            lastSelectionEffectLayerIndex < State.Layers.Count;

        public void InitializeSelectionMask()
        {
            Size? imageSize = GetSelectedLayer()?.Image.Size ?? State.CurrentImage?.Size;
            if (imageSize == null || imageSize.Value.Width <= 0 || imageSize.Value.Height <= 0)
                return;

            SelectionMask?.Dispose();
            SelectionMask = new Bitmap(
                imageSize.Value.Width,
                imageSize.Value.Height,
                PixelFormat.Format32bppArgb);
        }

        public void ResetMaskState()
        {
            SelectionMask?.Dispose();
            SelectionMask = null;

            lastSelectionEffectMask?.Dispose();
            lastSelectionEffectMask = null;

            lastSelectionEffectOriginalLayer?.Dispose();
            lastSelectionEffectOriginalLayer = null;

            lastSelectionEffect = string.Empty;
            lastSelectionEffectLayerIndex = -1;
        }

        public void DrawSelectionLine(Point start, Point end, bool erase, float width)
        {
            if (SelectionMask == null)
                return;

            using Graphics graphics = Graphics.FromImage(SelectionMask);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.CompositingMode = CompositingMode.SourceCopy;

            Color color = erase ? Color.Transparent : Color.White;
            using Pen pen = new Pen(color, Math.Max(1f, width))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            graphics.DrawLine(pen, start, end);
        }

        public void DrawSelectionShape(Rectangle rectangle, string shapeType, bool erase)
        {
            if (SelectionMask == null || rectangle.Width <= 0 || rectangle.Height <= 0)
                return;

            using Graphics graphics = Graphics.FromImage(SelectionMask);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.CompositingMode = CompositingMode.SourceCopy;

            using SolidBrush brush = new SolidBrush(erase ? Color.Transparent : Color.White);

            if (shapeType == "Ellipse")
                graphics.FillEllipse(brush, rectangle);
            else
                graphics.FillRectangle(brush, rectangle);
        }

        public void ClearSelection()
        {
            if (SelectionMask == null)
                return;

            using Graphics graphics = Graphics.FromImage(SelectionMask);
            graphics.Clear(Color.Transparent);
        }

        public bool HasSelection()
        {
            if (SelectionMask == null)
                return false;

            Rectangle area = new Rectangle(0, 0, SelectionMask.Width, SelectionMask.Height);
            BitmapData data = SelectionMask.LockBits(
                area,
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                int byteCount = Math.Abs(data.Stride) * data.Height;
                byte[] values = new byte[byteCount];
                Marshal.Copy(data.Scan0, values, 0, byteCount);

                for (int y = 0; y < data.Height; y++)
                {
                    int row = y * Math.Abs(data.Stride);
                    for (int x = 0; x < data.Width; x++)
                    {
                        if (values[row + x * 4 + 3] > 0)
                            return true;
                    }
                }

                return false;
            }
            finally
            {
                SelectionMask.UnlockBits(data);
            }
        }

        public void InvertSelection()
        {
            if (SelectionMask == null)
                return;

            InvertMaskBitmap(SelectionMask);
        }

        public bool ApplyEffectToSelection(string effectType)
        {
            Layer? layer = GetSelectedLayer();
            if (layer == null || layer.Locked || SelectionMask == null || !HasSelection())
                return false;

            lastSelectionEffectMask?.Dispose();
            lastSelectionEffectMask = new Bitmap(SelectionMask);

            lastSelectionEffectOriginalLayer?.Dispose();
            lastSelectionEffectOriginalLayer = new Bitmap(layer.Image);

            lastSelectionEffect = effectType;
            lastSelectionEffectLayerIndex = State.SelectedLayerIndex;

            ApplyEffectCore(effectType, layer, SelectionMask);
            RenderLayers();
            return true;
        }

        public bool SwapLastEffect()
        {
            if (!CanSwapLastEffect ||
                lastSelectionEffectMask == null ||
                lastSelectionEffectOriginalLayer == null)
            {
                return false;
            }

            Layer layer = State.Layers[lastSelectionEffectLayerIndex];
            if (layer.Locked)
                return false;

            layer.Image.Dispose();
            layer.Image = new Bitmap(lastSelectionEffectOriginalLayer);

            InvertMaskBitmap(lastSelectionEffectMask);
            ApplyEffectCore(lastSelectionEffect, layer, lastSelectionEffectMask);
            RenderLayers();
            return true;
        }

        private static void ApplyEffectCore(string effectType, Layer layer, Bitmap selectionMask)
        {
            Bitmap working = CreateArgbCopy(layer.Image);
            Bitmap mask = CreateArgbCopy(selectionMask);

            Rectangle area = new Rectangle(0, 0, working.Width, working.Height);
            BitmapData imageData = working.LockBits(
                area,
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);
            BitmapData maskData = mask.LockBits(
                area,
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                int imageStride = Math.Abs(imageData.Stride);
                int maskStride = Math.Abs(maskData.Stride);
                int imageByteCount = imageStride * working.Height;
                int maskByteCount = maskStride * mask.Height;

                byte[] imageValues = new byte[imageByteCount];
                byte[] originalValues = new byte[imageByteCount];
                byte[] maskValues = new byte[maskByteCount];

                Marshal.Copy(imageData.Scan0, imageValues, 0, imageByteCount);
                Marshal.Copy(maskData.Scan0, maskValues, 0, maskByteCount);
                Array.Copy(imageValues, originalValues, imageByteCount);

                for (int y = 0; y < working.Height; y++)
                {
                    int imageRow = y * imageStride;
                    int maskRow = y * maskStride;

                    for (int x = 0; x < working.Width; x++)
                    {
                        int pixel = imageRow + x * 4;
                        int maskPixel = maskRow + x * 4;

                        if (maskValues[maskPixel + 3] == 0)
                            continue;

                        switch (effectType)
                        {
                            case "Erase":
                                imageValues[pixel + 3] = 0;
                                break;

                            case "Grayscale":
                                byte gray = (byte)(
                                    imageValues[pixel + 2] * 0.30 +
                                    imageValues[pixel + 1] * 0.59 +
                                    imageValues[pixel] * 0.11);
                                imageValues[pixel] = gray;
                                imageValues[pixel + 1] = gray;
                                imageValues[pixel + 2] = gray;
                                break;

                            case "ColorInvert":
                                imageValues[pixel] = (byte)(255 - imageValues[pixel]);
                                imageValues[pixel + 1] = (byte)(255 - imageValues[pixel + 1]);
                                imageValues[pixel + 2] = (byte)(255 - imageValues[pixel + 2]);
                                break;

                            case "Blur":
                                ApplyBlurPixel(
                                    originalValues,
                                    imageValues,
                                    imageStride,
                                    working.Width,
                                    working.Height,
                                    x,
                                    y,
                                    pixel);
                                break;
                        }
                    }
                }

                Marshal.Copy(imageValues, 0, imageData.Scan0, imageByteCount);
            }
            finally
            {
                working.UnlockBits(imageData);
                mask.UnlockBits(maskData);
                mask.Dispose();
            }

            layer.Image.Dispose();
            layer.Image = working;
        }

        private static void ApplyBlurPixel(
            byte[] source,
            byte[] destination,
            int stride,
            int width,
            int height,
            int x,
            int y,
            int destinationIndex)
        {
            const int radius = 4;
            int blue = 0;
            int green = 0;
            int red = 0;
            int alpha = 0;
            int count = 0;

            for (int offsetY = -radius; offsetY <= radius; offsetY++)
            {
                int sourceY = y + offsetY;
                if (sourceY < 0 || sourceY >= height)
                    continue;

                for (int offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    int sourceX = x + offsetX;
                    if (sourceX < 0 || sourceX >= width)
                        continue;

                    int sourceIndex = sourceY * stride + sourceX * 4;
                    blue += source[sourceIndex];
                    green += source[sourceIndex + 1];
                    red += source[sourceIndex + 2];
                    alpha += source[sourceIndex + 3];
                    count++;
                }
            }

            if (count == 0)
                return;

            destination[destinationIndex] = (byte)(blue / count);
            destination[destinationIndex + 1] = (byte)(green / count);
            destination[destinationIndex + 2] = (byte)(red / count);
            destination[destinationIndex + 3] = (byte)(alpha / count);
        }

        private static Bitmap CreateArgbCopy(Bitmap source)
        {
            Bitmap result = new Bitmap(
                source.Width,
                source.Height,
                PixelFormat.Format32bppArgb);

            using Graphics graphics = Graphics.FromImage(result);
            graphics.DrawImageUnscaled(source, 0, 0);
            return result;
        }

        private static void InvertMaskBitmap(Bitmap bitmap)
        {
            Rectangle area = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(
                area,
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            try
            {
                int stride = Math.Abs(data.Stride);
                int byteCount = stride * bitmap.Height;
                byte[] values = new byte[byteCount];
                Marshal.Copy(data.Scan0, values, 0, byteCount);

                for (int y = 0; y < bitmap.Height; y++)
                {
                    int row = y * stride;
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        int pixel = row + x * 4;
                        bool selected = values[pixel + 3] > 0;
                        byte value = selected ? (byte)0 : (byte)255;
                        values[pixel] = value;
                        values[pixel + 1] = value;
                        values[pixel + 2] = value;
                        values[pixel + 3] = value;
                    }
                }

                Marshal.Copy(values, 0, data.Scan0, byteCount);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
    }
}

