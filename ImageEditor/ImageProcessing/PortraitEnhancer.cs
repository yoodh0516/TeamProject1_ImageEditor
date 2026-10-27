using OpenCvSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using CvSize = OpenCvSharp.Size;

namespace ImageEditor.ImageProcessing
{
    public static class PortraitEnhancer
    {
        public static Bitmap Enhance(Bitmap source)
        {
            using Mat src = BitmapToMat(source);
            using Mat bgr = EnsureBgr(src);

            // 1. 원본 색감은 최대한 유지하면서 아주 약한 톤 보정만 적용
            AnalyzeImage(bgr, out double brightness, out double contrast, out double saturation);

            double gamma = Clamp(1.0 + (118.0 - brightness) / 1200.0, 0.985, 1.04);
            double saturationScale = Clamp(1.0 + (68.0 - saturation) / 1800.0, 0.995, 1.02);

            using Mat toneCorrected = ApplyGentleToneCorrection(bgr, gamma, saturationScale, contrast);

            // 2. 피부 영역 검출
            using Mat skinMaskHard = CreateSkinMask(toneCorrected);
            int skinPixels = Cv2.CountNonZero(skinMaskHard);

            if (skinPixels < Math.Max(80, toneCorrected.Rows * toneCorrected.Cols / 250))
                return MatToBitmap(toneCorrected);

            // 3. 사진마다 피부 상태를 분석해서 한 번 클릭에 체감되는 강도로 결정
            double roughness = CalculateSkinRoughness(toneCorrected, skinMaskHard);

            double blemishStrength = Clamp(0.72 + roughness / 115.0, 0.72, 0.92);
            double wrinkleStrength = Clamp(0.42 + roughness / 135.0, 0.42, 0.68);
            double wrinkleLift = Clamp(13.0 + roughness * 0.28, 13.0, 22.0);

            // 4. 작은 잡티만 선택적으로 강하게 완화
            using Mat blemishReference = new Mat();
            Cv2.MedianBlur(toneCorrected, blemishReference, 5);

            using Mat blemishMask = CreateBlemishMask(
                toneCorrected,
                blemishReference,
                skinMaskHard,
                roughness);

            using Mat blemishReduced = BlendWithMask(
                toneCorrected,
                blemishReference,
                blemishMask,
                blemishStrength);

            // 5. 주름은 얼굴 전체를 흐리지 않고 어두운 선만 선택적으로 밝게 완화
            using Mat wrinkleMask = CreateWrinkleMask(blemishReduced, skinMaskHard, roughness);
            using Mat wrinkleSoft = wrinkleMask.Clone();
            Cv2.GaussianBlur(wrinkleSoft, wrinkleSoft, new CvSize(5, 5), 0);

            using Mat wrinkleReduced = LightenWithMask(
                blemishReduced,
                wrinkleSoft,
                wrinkleStrength,
                wrinkleLift);

            // 6. 잡티/주름으로 검출된 부분만 아주 살짝 정돈
            // 얼굴 전체에 스무딩을 적용하지 않으므로 원본 선명도를 유지한다.
            using Mat treatmentMask = new Mat();
            Cv2.Max(blemishMask, wrinkleSoft, treatmentMask);

            using Mat treatmentMaskSoft = treatmentMask.Clone();
            Cv2.GaussianBlur(treatmentMaskSoft, treatmentMaskSoft, new CvSize(7, 7), 0);

            using Mat localSmooth = new Mat();
            Cv2.BilateralFilter(
                wrinkleReduced,
                localSmooth,
                5,
                24,
                24);

            using Mat locallyRetouched = BlendWithMask(
                wrinkleReduced,
                localSmooth,
                treatmentMaskSoft,
                0.28);

            // 7. 처리하지 않은 영역의 디테일은 원본 수준으로 유지
            // 피부 전체 블러 대신 약한 언샤프를 비처리 영역에만 적용한다.
            using Mat blur = new Mat();
            Cv2.GaussianBlur(locallyRetouched, blur, new CvSize(0, 0), 0.9);

            using Mat gentlySharpened = new Mat();
            Cv2.AddWeighted(
                locallyRetouched,
                1.10,
                blur,
                -0.10,
                0,
                gentlySharpened);

            using Mat preserveMask = new Mat();
            Cv2.BitwiseNot(treatmentMaskSoft, preserveMask);

            using Mat result = BlendWithMask(
                locallyRetouched,
                gentlySharpened,
                preserveMask,
                0.70);

            return MatToBitmap(result);
        }

        private static Mat ApplyGentleToneCorrection(
            Mat bgr,
            double gamma,
            double saturationScale,
            double contrast)
        {
            using Mat gammaCorrected = ApplyGamma(bgr, gamma);

            // 명암이 매우 낮은 사진만 아주 약한 전역 대비 보정
            double contrastScale = Clamp(1.0 + (40.0 - contrast) / 700.0, 1.0, 1.025);

            using Mat contrastCorrected = new Mat();
            gammaCorrected.ConvertTo(
                contrastCorrected,
                MatType.CV_8UC3,
                contrastScale,
                128.0 * (1.0 - contrastScale));

            using Mat hsv = new Mat();
            Cv2.CvtColor(contrastCorrected, hsv, ColorConversionCodes.BGR2HSV);
            Mat[] channels = Cv2.Split(hsv);

            channels[1].ConvertTo(
                channels[1],
                MatType.CV_8U,
                saturationScale,
                0);

            Cv2.Merge(channels, hsv);
            foreach (Mat channel in channels)
                channel.Dispose();

            Mat output = new Mat();
            Cv2.CvtColor(hsv, output, ColorConversionCodes.HSV2BGR);
            return output;
        }

        private static Mat ApplyGamma(Mat image, double gamma)
        {
            byte[] lutValues = new byte[256];
            double inverseGamma = 1.0 / gamma;

            for (int i = 0; i < 256; i++)
            {
                double normalized = i / 255.0;
                lutValues[i] = (byte)Clamp(
                    Math.Pow(normalized, inverseGamma) * 255.0,
                    0,
                    255);
            }

            using Mat lut = new Mat(1, 256, MatType.CV_8UC1);

            for (int i = 0; i < 256; i++)
            {
                lut.Set(0, i, lutValues[i]);
            }

            Mat result = new Mat();
            Cv2.LUT(image, lut, result);

            return result;
        }

        private static Mat CreateSkinMask(Mat image)
        {
            using Mat ycrcb = new Mat();
            Cv2.CvtColor(image, ycrcb, ColorConversionCodes.BGR2YCrCb);

            using Mat ycrcbMask = new Mat();
            Cv2.InRange(
                ycrcb,
                new Scalar(20, 128, 70),
                new Scalar(255, 180, 135),
                ycrcbMask);

            using Mat hsv = new Mat();
            Cv2.CvtColor(image, hsv, ColorConversionCodes.BGR2HSV);

            using Mat hsvMask1 = new Mat();
            using Mat hsvMask2 = new Mat();

            Cv2.InRange(hsv, new Scalar(0, 18, 35), new Scalar(25, 230, 255), hsvMask1);
            Cv2.InRange(hsv, new Scalar(165, 18, 35), new Scalar(179, 230, 255), hsvMask2);

            using Mat hsvMask = new Mat();
            Cv2.BitwiseOr(hsvMask1, hsvMask2, hsvMask);

            Mat skinMask = new Mat();
            Cv2.BitwiseAnd(ycrcbMask, hsvMask, skinMask);

            using Mat kernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new CvSize(5, 5));

            Cv2.MorphologyEx(skinMask, skinMask, MorphTypes.Close, kernel);
            Cv2.MorphologyEx(skinMask, skinMask, MorphTypes.Open, kernel);

            return skinMask;
        }

        private static Mat CreateBlemishMask(
            Mat image,
            Mat smoothReference,
            Mat skinMask,
            double roughness)
        {
            using Mat gray = new Mat();
            using Mat refGray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.CvtColor(smoothReference, refGray, ColorConversionCodes.BGR2GRAY);

            // 주변보다 어두운 잡티 위주 검출
            using Mat darkDifference = new Mat();
            Cv2.Subtract(refGray, gray, darkDifference);

            double threshold = Clamp(7.5 + roughness * 0.05, 7.5, 10.0);

            Mat mask = new Mat();
            Cv2.Threshold(darkDifference, mask, threshold, 255, ThresholdTypes.Binary);
            Cv2.BitwiseAnd(mask, skinMask, mask);

            using Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new CvSize(3, 3));
            Cv2.MorphologyEx(mask, mask, MorphTypes.Open, kernel);

            // 너무 가는 윤곽선보다 작은 점/얼룩을 부드럽게 처리
            Cv2.GaussianBlur(mask, mask, new CvSize(3, 3), 0);
            return mask;
        }

        private static Mat CreateWrinkleMask(
            Mat image,
            Mat skinMask,
            double roughness)
        {
            using Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            // BlackHat = 주변 피부보다 어두운 가는 선 강조
            using Mat kernel = Cv2.GetStructuringElement(
                MorphShapes.Ellipse,
                new CvSize(9, 9));

            using Mat blackHat = new Mat();
            Cv2.MorphologyEx(gray, blackHat, MorphTypes.BlackHat, kernel);

            double threshold = Clamp(7.5 + roughness * 0.05, 7.5, 9.5);

            Mat mask = new Mat();
            Cv2.Threshold(blackHat, mask, threshold, 255, ThresholdTypes.Binary);
            Cv2.BitwiseAnd(mask, skinMask, mask);

            using Mat thinKernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new CvSize(3, 3));
            Cv2.MorphologyEx(mask, mask, MorphTypes.Open, thinKernel);

            return mask;
        }

        private static Mat LightenWithMask(
            Mat original,
            Mat mask,
            double strength,
            double maxLift)
        {
            using Mat maskFloat = new Mat();
            mask.ConvertTo(maskFloat, MatType.CV_32FC1, strength / 255.0);

            using Mat mask3 = new Mat();
            Cv2.Merge(new[] { maskFloat, maskFloat, maskFloat }, mask3);

            using Mat originalFloat = new Mat();
            original.ConvertTo(originalFloat, MatType.CV_32FC3);

            using Mat lift = new Mat(original.Size(), MatType.CV_32FC3, new Scalar(maxLift, maxLift, maxLift));
            using Mat liftMasked = new Mat();
            Cv2.Multiply(lift, mask3, liftMasked);

            using Mat resultFloat = new Mat();
            Cv2.Add(originalFloat, liftMasked, resultFloat);

            Mat result = new Mat();
            resultFloat.ConvertTo(result, MatType.CV_8UC3);
            return result;
        }

        private static void AnalyzeImage(
            Mat bgr,
            out double brightness,
            out double contrast,
            out double saturation)
        {
            using Mat gray = new Mat();
            Cv2.CvtColor(bgr, gray, ColorConversionCodes.BGR2GRAY);

            Cv2.MeanStdDev(gray, out Scalar grayMean, out Scalar grayStdDev);
            brightness = grayMean.Val0;
            contrast = grayStdDev.Val0;

            using Mat hsv = new Mat();
            Cv2.CvtColor(bgr, hsv, ColorConversionCodes.BGR2HSV);
            Mat[] channels = Cv2.Split(hsv);
            saturation = Cv2.Mean(channels[1]).Val0;

            foreach (Mat channel in channels)
                channel.Dispose();
        }

        private static double CalculateSkinRoughness(Mat image, Mat skinMask)
        {
            if (Cv2.CountNonZero(skinMask) == 0)
                return 10.0;

            using Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            using Mat blur = new Mat();
            Cv2.GaussianBlur(gray, blur, new CvSize(7, 7), 0);

            using Mat detail = new Mat();
            Cv2.Absdiff(gray, blur, detail);

            Scalar detailMean = Cv2.Mean(detail, skinMask);
            return Clamp(detailMean.Val0 * 3.2, 0.0, 42.0);
        }

        private static Mat RestoreSkinTexture(
            Mat smoothed,
            Mat original,
            Mat skinMask,
            double amount)
        {
            using Mat originalBlur = new Mat();
            Cv2.GaussianBlur(original, originalBlur, new CvSize(0, 0), 1.15);

            using Mat originalFloat = new Mat();
            using Mat blurFloat = new Mat();
            using Mat smoothFloat = new Mat();
            original.ConvertTo(originalFloat, MatType.CV_32FC3);
            originalBlur.ConvertTo(blurFloat, MatType.CV_32FC3);
            smoothed.ConvertTo(smoothFloat, MatType.CV_32FC3);

            using Mat detail = new Mat();
            Cv2.Subtract(originalFloat, blurFloat, detail);

            using Mat maskFloat = new Mat();
            skinMask.ConvertTo(maskFloat, MatType.CV_32FC1, amount / 255.0);

            using Mat mask3 = new Mat();
            Cv2.Merge(new[] { maskFloat, maskFloat, maskFloat }, mask3);

            using Mat maskedDetail = new Mat();
            Cv2.Multiply(detail, mask3, maskedDetail);

            using Mat resultFloat = new Mat();
            Cv2.Add(smoothFloat, maskedDetail, resultFloat);

            Mat result = new Mat();
            resultFloat.ConvertTo(result, MatType.CV_8UC3);
            return result;
        }

        private static double CalculateSharpness(Mat image)
        {
            using Mat gray = new Mat();
            Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

            using Mat laplacian = new Mat();
            Cv2.Laplacian(gray, laplacian, MatType.CV_64F);
            Cv2.MeanStdDev(laplacian, out _, out Scalar stdDev);
            return stdDev.Val0;
        }

        private static Mat BlendWithMask(
            Mat original,
            Mat overlay,
            Mat mask,
            double strength)
        {
            using Mat maskFloat = new Mat();
            mask.ConvertTo(maskFloat, MatType.CV_32FC1, strength / 255.0);

            using Mat mask3 = new Mat();
            Cv2.Merge(new[] { maskFloat, maskFloat, maskFloat }, mask3);

            using Mat originalFloat = new Mat();
            using Mat overlayFloat = new Mat();
            original.ConvertTo(originalFloat, MatType.CV_32FC3);
            overlay.ConvertTo(overlayFloat, MatType.CV_32FC3);

            using Mat inverseMask = new Mat(mask3.Size(), mask3.Type(), Scalar.All(1.0));
            Cv2.Subtract(inverseMask, mask3, inverseMask);

            using Mat originalPart = new Mat();
            using Mat overlayPart = new Mat();
            Cv2.Multiply(originalFloat, inverseMask, originalPart);
            Cv2.Multiply(overlayFloat, mask3, overlayPart);

            using Mat resultFloat = new Mat();
            Cv2.Add(originalPart, overlayPart, resultFloat);

            Mat result = new Mat();
            resultFloat.ConvertTo(result, MatType.CV_8UC3);
            return result;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static Mat EnsureBgr(Mat source)
        {
            if (source.Channels() == 3)
                return source.Clone();

            Mat result = new Mat();

            if (source.Channels() == 4)
                Cv2.CvtColor(source, result, ColorConversionCodes.BGRA2BGR);
            else
                Cv2.CvtColor(source, result, ColorConversionCodes.GRAY2BGR);

            return result;
        }

        private static Mat BitmapToMat(Bitmap bitmap)
        {
            using MemoryStream stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return Cv2.ImDecode(stream.ToArray(), ImreadModes.Unchanged);
        }

        private static Bitmap MatToBitmap(Mat mat)
        {
            Cv2.ImEncode(".png", mat, out byte[] bytes);
            using MemoryStream stream = new MemoryStream(bytes);
            using Bitmap temp = new Bitmap(stream);
            return new Bitmap(temp);
        }
    }
}
