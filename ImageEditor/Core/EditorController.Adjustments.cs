using System.Drawing;
using ImageEditor.ImageProcessing;

namespace ImageEditor.Core
{
    public partial class EditorController
    {
        public void ApplyAdjustments(
            Bitmap baseImage,
            int brightness,
            int contrast,
            int saturation,
            int temperature)
        {
            Bitmap? oldImage = State.CurrentImage;

            State.CurrentImage = BasicImageProcessor.Adjust(
                baseImage,
                brightness,
                contrast,
                saturation,
                temperature);

            oldImage?.Dispose();
        }

    }
}
