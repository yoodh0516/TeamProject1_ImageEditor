using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace ImageEditor.Services
{
    public class SnsExportService
    {
        // 현재 이미지를 목표 비율에 맞게 중앙 Crop 후
        // 원하는 출력 크기로 Resize
        private Bitmap? CreateExportImage(
            Bitmap source,
            int targetWidth,
            int targetHeight)
        {
            if (source == null || source.Width <= 0 || source.Height <= 0 ||
                targetWidth <= 0 || targetHeight <= 0)
                return null;

            double sourceRatio =
                (double)source.Width / source.Height;

            double targetRatio =
                (double)targetWidth / targetHeight;

            int cropWidth;
            int cropHeight;

            // 원본이 목표보다 가로로 긴 경우
            if (sourceRatio > targetRatio)
            {
                cropHeight = source.Height;
                cropWidth =
                    (int)(source.Height * targetRatio);
            }
            else
            {
                // 원본이 목표보다 세로로 긴 경우
                cropWidth = source.Width;
                cropHeight =
                    (int)(source.Width / targetRatio);
            }

            // 가운데 기준 Crop 위치
            int x =
                (source.Width - cropWidth) / 2;

            int y =
                (source.Height - cropHeight) / 2;

            Rectangle sourceArea =
                new Rectangle(
                    x,
                    y,
                    cropWidth,
                    cropHeight
                );

            Bitmap result =
                new Bitmap(
                    targetWidth,
                    targetHeight
                );

            using (Graphics g =
                   Graphics.FromImage(result))
            {
                g.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

                g.DrawImage(
                    source,
                    new Rectangle(
                        0,
                        0,
                        targetWidth,
                        targetHeight
                    ),
                    sourceArea,
                    GraphicsUnit.Pixel
                );
            }

            return result;
        }


        // SNS / 웹 최적화 JPG 저장
        public bool Export(
            Bitmap source,
            int width,
            int height,
            long quality,
            string defaultFileName)
        {
            if (source == null || width <= 0 || height <= 0)
                return false;

            Bitmap? exportImage = CreateExportImage(source, width, height);
            if (exportImage == null)
                return false;

            using (exportImage)
            {
                using (SaveFileDialog dialog =
                       new SaveFileDialog())
                {
                    dialog.Title =
                        "SNS / 웹 최적화 내보내기";

                    dialog.Filter =
                        "JPG 파일|*.jpg";

                    dialog.DefaultExt =
                        "jpg";

                    dialog.FileName =
                        defaultFileName;

                    if (dialog.ShowDialog()
                        != DialogResult.OK)
                    {
                        return false;
                    }

                    ImageCodecInfo jpegCodec =
                        ImageCodecInfo
                        .GetImageEncoders()
                        .FirstOrDefault(
                            codec =>
                            codec.FormatID ==
                            ImageFormat.Jpeg.Guid
                        );

                    if (jpegCodec == null)
                        return false;

                    try
                    {
                        using EncoderParameters parameters =
                            new EncoderParameters(1);
                        parameters.Param[0] =
                            new EncoderParameter(
                                System.Drawing.Imaging.Encoder.Quality,
                                quality
                            );

                        exportImage.Save(
                            dialog.FileName,
                            jpegCodec,
                            parameters
                        );
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "SNS 이미지를 저장할 수 없습니다.\r\n\r\n" + ex.Message,
                            "SNS 내보내기",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
