using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageEditor.ImageProcessing
{
    public static partial class BasicImageProcessor
    {
        // 왼쪽으로 90도 회전
        public static Bitmap RotateLeft(Bitmap image)
        {
            ArgumentNullException.ThrowIfNull(image);

            Bitmap result = new Bitmap(image);

            result.RotateFlip(RotateFlipType.Rotate270FlipNone);

            return result;
        }

        // 오른쪽으로 90도 회전
        public static Bitmap RotateRight(Bitmap image)
        {
            ArgumentNullException.ThrowIfNull(image);

            Bitmap result = new Bitmap(image);

            result.RotateFlip(RotateFlipType.Rotate90FlipNone);

            return result;
        }

        // 좌우 반전
        public static Bitmap FlipHorizontal(Bitmap image)
        {
            ArgumentNullException.ThrowIfNull(image);

            Bitmap result = new Bitmap(image);

            result.RotateFlip( RotateFlipType.RotateNoneFlipX);

            return result;
        }
        // 상하 반전
        public static Bitmap FlipVertical(Bitmap image)
        {
            ArgumentNullException.ThrowIfNull(image);

            Bitmap result = new Bitmap(image);

            result.RotateFlip( RotateFlipType.RotateNoneFlipY);

            return result;
        }

        // 실제 이미지 크기 변경
        public static Bitmap Resize( Bitmap image,int width, int height)
        {
            ArgumentNullException.ThrowIfNull(image);

            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    "이미지 크기는 1픽셀 이상이어야 합니다.");

            Bitmap result = new Bitmap(width, height);

            using (Graphics g = Graphics.FromImage(result))
            {
                // 크기 변경 시 화질을 조금 더 부드럽게 처리
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                g.DrawImage( image, 0, 0,width, height);
            }

            return result;
        }


        // 선택한 영역만 잘라서 새로운 Bitmap 반환
        public static Bitmap? Crop(Bitmap image, Rectangle cropArea)
        {
            ArgumentNullException.ThrowIfNull(image);

            if (cropArea.Width <= 0 ||cropArea.Height <= 0)
                return null;


            // 실제 이미지 전체 범위
            Rectangle imageArea =new Rectangle(0, 0,image.Width,image.Height);


            // 이미지 밖으로 나간 부분 제거
            Rectangle validArea =Rectangle.Intersect(imageArea,cropArea);


            if (validArea.Width <= 0 ||validArea.Height <= 0)
                return null;


            // 잘라낼 크기의 새로운 Bitmap 생성
            Bitmap result =new Bitmap(validArea.Width,validArea.Height);


            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage( image,

                    // 결과 Bitmap 전체 영역
                    new Rectangle( 0,0, result.Width, result.Height),
                    // 원본에서 가져올 영역
                    validArea, GraphicsUnit.Pixel
                );
            }
            return result;
        }



    }
}
