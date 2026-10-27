using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageEditor.Services
{
    public class ImageFileService
    {
        // 이미지 파일을 선택해서 Bitmap으로 반환
        public Bitmap? OpenImage(out string? filePath)
        {
            filePath = null;

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "이미지 불러오기";

                dialog.Filter =
                    "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp";

                // 사용자가 취소하면 null 반환
                if (dialog.ShowDialog() != DialogResult.OK)
                    return null;

                filePath = dialog.FileName;

                try
                {
                    // 파일 잠금을 피하기 위해 Bitmap 복사본 생성
                    using Image image = Image.FromFile(filePath);
                    return new Bitmap(image);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "이미지 파일을 열 수 없습니다.\r\n\r\n" + ex.Message,
                        "이미지 열기",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    filePath = null;
                    return null;
                }
            }
        }
    

        // 현재 경로에 이미지 저장
        public bool SaveImage(Bitmap image, string filePath)
        {
            if (image == null)
                return false;

            if (string.IsNullOrEmpty(filePath))
                return false;

            try
            {
                string extension =
                    Path.GetExtension(filePath).ToLowerInvariant();

                switch (extension)
                {
                    case ".jpg":
                    case ".jpeg":
                        SaveJpeg(image, filePath, 95L);
                        break;

                    case ".bmp":
                        image.Save(filePath, ImageFormat.Bmp);
                        break;

                    default:
                        image.Save(filePath, ImageFormat.Png);
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "이미지 파일을 저장할 수 없습니다.\r\n\r\n" + ex.Message,
                    "이미지 저장",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private static void SaveJpeg(Bitmap image, string filePath, long quality)
        {
            ImageCodecInfo? jpegCodec = ImageCodecInfo
                .GetImageEncoders()
                .FirstOrDefault(codec => codec.FormatID == ImageFormat.Jpeg.Guid);

            if (jpegCodec == null)
            {
                image.Save(filePath, ImageFormat.Jpeg);
                return;
            }

            // JPEG는 투명도를 지원하지 않는다. 투명 픽셀이 검게 저장되지 않도록
            // 흰색 배경의 표준 24비트 이미지로 합성한 뒤 품질을 명시한다.
            using Bitmap jpegImage = new Bitmap(
                image.Width,
                image.Height,
                PixelFormat.Format24bppRgb);
            if (image.HorizontalResolution > 0 && image.VerticalResolution > 0)
                jpegImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (Graphics graphics = Graphics.FromImage(jpegImage))
            {
                graphics.Clear(Color.White);
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height));
            }

            using EncoderParameters parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(
                System.Drawing.Imaging.Encoder.Quality,
                quality);
            jpegImage.Save(filePath, jpegCodec, parameters);
        }

        // 다른 이름으로 저장
        public bool SaveImageAs( Bitmap image,out string filePath)
        {
            filePath = null;

            if (image == null)
                return false;

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "다른 이름으로 저장";

                dialog.Filter = "PNG 파일|*.png|" + "JPG 파일|*.jpg|" + "BMP 파일|*.bmp";

                dialog.FilterIndex = 1;

                dialog.DefaultExt = "png";

                dialog.AddExtension = true;

                dialog.OverwritePrompt = true;

                dialog.FileName = "edited_image";

                if (dialog.ShowDialog()!= DialogResult.OK)
                {
                    return false;
                }

                filePath = dialog.FileName;

                return SaveImage(image, filePath);
            }
        }

        // 품질을 지정해서 JPG 내보내기
        public bool ExportJpeg(
            Bitmap image,
            long quality)
        {
            if (image == null)
                return false;

            if (quality < 1)
                quality = 1;

            if (quality > 100)
                quality = 100;

            using (SaveFileDialog dialog =
                   new SaveFileDialog())
            {
                dialog.Title =
                    "JPG 이미지 내보내기";

                dialog.Filter =
                    "JPG 파일|*.jpg";

                dialog.DefaultExt =
                    "jpg";

                dialog.FileName =
                    "edited_image.jpg";

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

                    image.Save(
                        dialog.FileName,
                        jpegCodec,
                        parameters
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "JPG 파일을 내보낼 수 없습니다.\r\n\r\n" + ex.Message,
                        "JPG 내보내기",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                return true;
            }
        }

        public bool ExportPng(
    Bitmap image)
        {
            if (image == null)
                return false;

            using (SaveFileDialog dialog =
                   new SaveFileDialog())
            {
                dialog.Title =
                    "PNG 이미지 내보내기";

                dialog.Filter =
                    "PNG 파일|*.png";

                dialog.DefaultExt =
                    "png";

                dialog.FileName =
                    "edited_image.png";

                if (dialog.ShowDialog()
                    != DialogResult.OK)
                {
                    return false;
                }

                try
                {
                    image.Save(
                        dialog.FileName,
                        ImageFormat.Png
                    );
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "PNG 파일을 내보낼 수 없습니다.\r\n\r\n" + ex.Message,
                        "PNG 내보내기",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                return true;
            }
        }
    }

}
