using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageEditor.Core
{
    public static class CanvasViewCalculator
    {
        // 이미지가 작업 영역 안에 들어가는 배율 계산
        public static float GetFitZoom(
            Size imageSize,
            Size panelSize)
        {
            if (imageSize.Width <= 0 || imageSize.Height <= 0 || panelSize.Width <= 0 || panelSize.Height <= 0)
            {
                return 1.0f;
            }

            float widthRatio = (float)panelSize.Width / imageSize.Width;

            float heightRatio = (float)panelSize.Height / imageSize.Height;

            // 작은 이미지까지 작업 영역 크기로 과도하게 확대하지 않도록
            // 화면 맞춤은 원본 크기(100%)까지만 허용한다.
            return Math.Min(1.0f, Math.Min(widthRatio, heightRatio));
        }

        // 실제 화면에 표시할 크기
        public static Size GetDisplaySize( Size imageSize, float zoom)
        {
            // 확대/축소를 반복할 때 절삭 오차로 표시 크기가 1px씩 흔들리지 않도록
            // 동일한 반올림 기준을 사용한다.
            int width = Math.Max(1, (int)Math.Round(imageSize.Width * zoom));

            int height = Math.Max(1, (int)Math.Round(imageSize.Height * zoom));

            return new Size( width, height);
        }

        // 작업 영역 가운데 위치 계산
        public static Point GetCenterLocation( Size panelSize, Size imageSize)
        {
            int x = (panelSize.Width - imageSize.Width) / 2;

            int y = (panelSize.Height - imageSize.Height) / 2;

            if (x < 0)
                x = 0;

            if (y < 0)
                y = 0;

            return new Point(x, y);
        }

       
    }
}
