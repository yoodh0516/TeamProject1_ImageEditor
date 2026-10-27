using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Threading;

namespace ImageEditor.Models.AI {
    public interface IAiUpscaler {
        // AI 모델 이름 (예: "Real-ESRGAN")
        string ModelName { get; }

        // 이미지를 받아서 화질을 개선한 새 이미지를 돌려주는 핵심 메서드
        Bitmap Enhance(Bitmap inputImage, CancellationToken cancellationToken);
    }

    // 2. Real-ESRGAN 실제 구동 클래스
    public class RealEsrganModel : IAiUpscaler, IDisposable {
        public string ModelName => "Real-ESRGAN";
        private InferenceSession? session;
         
        public RealEsrganModel() {
            // Models 폴더 안의 onnx 파일 경로 지정
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "real_esrgan_x4plus.onnx");

            if (File.Exists(modelPath)) {
                session = OnnxInferenceSessionFactory.Create(modelPath);
            }
        }

        public Bitmap Enhance(
            Bitmap inputImage,
            CancellationToken cancellationToken) {
            if (session == null) {
                throw new FileNotFoundException("ONNX 모델 파일을 찾을 수 없습니다.");
            }

            // AI 세팅 값
            int tileSize = 128; // 모델이 요구하는 고정 입력 크기

            // 1. 원본 크기를 128의 배수로 넉넉하게 맞추기 위한 패딩(여백) 크기 계산
            int origW = inputImage.Width;
            int origH = inputImage.Height;
            int padW = (int)Math.Ceiling((double)origW / tileSize) * tileSize;
            int padH = (int)Math.Ceiling((double)origH / tileSize) * tileSize;

            // 2. 가장자리 반사(Reflect) 방식으로 여백을 채운 안전한 이미지(Mat) 생성
            using Mat imgMat = inputImage.ToMat();
            using Mat paddedMat = new Mat();
            Cv2.CopyMakeBorder(imgMat, paddedMat, 0, padH - origH, 0, padW - origW, BorderTypes.Reflect101);

            // 3. 모델은 각 타일을 x4로 출력하지만, 전체 x4 이미지를 만들면
            // 고해상도 사진에서 수백 MB의 임시 메모리가 필요하다. 각 결과 타일을
            // 바로 원본 크기 결과에 고품질 축소해 반영하면 품질과 최종 크기는
            // 유지하면서 큰 임시 Mat과 마지막 전체 축소를 없앨 수 있다.
            using Mat resultMat = new Mat(padH, padW, MatType.CV_8UC3);
            string inputName = session.InputMetadata.Keys.First(); // 입구 이름 자동 찾기

            // 4. 이미지를 128x128 사이즈로 잘라서 순회하며 AI 돌리기
            for (int y = 0; y < padH; y += tileSize) {
                for (int x = 0; x < padW; x += tileSize) {
                    cancellationToken.ThrowIfCancellationRequested();
                    // [4-1] 128x128 크기로 조각(Tile) 잘라내기
                    Rect roi = new Rect(x, y, tileSize, tileSize);
                    using Mat tile = new Mat(paddedMat, roi);

                    // [4-2] 전처리 (BGR -> RGB 후 텐서 변환)
                    using Mat tileRgb = new Mat();
                    Cv2.CvtColor(tile, tileRgb, ColorConversionCodes.BGR2RGB);
                    Tensor<float> inputTensor = ConvertMatToTensor(tileRgb);

                    // [4-3] AI 추론 실행
                    var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, inputTensor) };
                    using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);
                    Tensor<float> outputTensor = results.First().AsTensor<float>();

                    // [4-4] 후처리: x4 결과를 고품질 Lanczos 축소로 원본 타일 크기에 반영
                    using Bitmap outTileBmp = ConvertTensorToBitmap(outputTensor);
                    using Mat outTile = outTileBmp.ToMat();
                    using Mat downsampledTile = new Mat();
                    Cv2.Resize(
                        outTile,
                        downsampledTile,
                        new OpenCvSharp.Size(tileSize, tileSize),
                        0,
                        0,
                        InterpolationFlags.Lanczos4);

                    // [4-5] 원본 크기 캔버스의 제자리에 덮어쓰기
                    Rect outRoi = new Rect(x, y, tileSize, tileSize);
                    using Mat destinationRoi = new Mat(resultMat, outRoi);
                    downsampledTile.CopyTo(destinationRoi);
                }
            }

            // 5. 처음에 추가한 패딩만 제거해 원본 크기로 반환
            Rect finalCrop = new Rect(0, 0, origW, origH);
            using Mat finalMat = new Mat(resultMat, finalCrop);

            cancellationToken.ThrowIfCancellationRequested();
            return finalMat.ToBitmap();
        }

        public void Dispose() {
            session?.Dispose();
            session = null;
        }

        // --- 전처리/후처리 수학 연산 헬퍼 메서드 ---
        private Tensor<float> ConvertMatToTensor(Mat mat) {
            int width = mat.Width;
            int height = mat.Height;
            var tensor = new DenseTensor<float>(new[] { 1, 3, height, width });
            var indexer = mat.GetGenericIndexer<Vec3b>();

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    Vec3b color = indexer[y, x];
                    // 0~255 값을 0.0~1.0 소수점으로 정규화
                    tensor[0, 0, y, x] = color.Item0 / 255.0f;
                    tensor[0, 1, y, x] = color.Item1 / 255.0f;
                    tensor[0, 2, y, x] = color.Item2 / 255.0f;
                }
            }
            return tensor;
        }

        private Bitmap ConvertTensorToBitmap(Tensor<float> tensor) {
            int height = tensor.Dimensions[2];
            int width = tensor.Dimensions[3];

            using Mat outMat = new Mat(height, width, MatType.CV_8UC3);
            var indexer = outMat.GetGenericIndexer<Vec3b>();

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    // 0.0~1.0 값을 다시 0~255 값으로 역정규화
                    float r = Math.Clamp(tensor[0, 0, y, x] * 255.0f, 0, 255);
                    float g = Math.Clamp(tensor[0, 1, y, x] * 255.0f, 0, 255);
                    float b = Math.Clamp(tensor[0, 2, y, x] * 255.0f, 0, 255);

                    indexer[y, x] = new Vec3b((byte)r, (byte)g, (byte)b);
                }
            }

            Cv2.CvtColor(outMat, outMat, ColorConversionCodes.RGB2BGR);
            return outMat.ToBitmap();
        }
    }

    // 3. 초고속 경량화 모델 구동 클래스 (기존 FsrcnnModel 대체)
    public class AnimeVideoModel : IAiUpscaler, IDisposable {
        public string ModelName => "초고속 화질 개선 (AnimeVideo)";
        private InferenceSession? session;

        private readonly int tileSize = 128;

        public AnimeVideoModel() {
            // 💡 다운로드한 파일 이름을 코드에 그대로 명시합니다.
            string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "RealESR-AnimeVideo-v3_x4.onnx");

            if (File.Exists(modelPath)) {
                session = OnnxInferenceSessionFactory.Create(modelPath);
            }
        }

        public Bitmap Enhance(
            Bitmap inputImage,
            CancellationToken cancellationToken) {
            if (session == null) {
                throw new FileNotFoundException("초고속 모델 파일을 찾을 수 없습니다. Models 폴더를 확인해 주세요.");
            }

            int origW = inputImage.Width;
            int origH = inputImage.Height;
            int padW = (int)Math.Ceiling((double)origW / tileSize) * tileSize;
            int padH = (int)Math.Ceiling((double)origH / tileSize) * tileSize;

            using Mat imgMat = inputImage.ToMat();
            using Mat paddedMat = new Mat();
            Cv2.CopyMakeBorder(imgMat, paddedMat, 0, padH - origH, 0, padW - origW, BorderTypes.Reflect101);

            // x4 결과를 타일 단위로 원본 크기에 반영해 대형 임시 Bitmap을 만들지 않는다.
            using Mat resultMat = new Mat(padH, padW, MatType.CV_8UC3);
            string inputName = session.InputMetadata.Keys.First();

            for (int y = 0; y < padH; y += tileSize) {
                for (int x = 0; x < padW; x += tileSize) {
                    cancellationToken.ThrowIfCancellationRequested();
                    Rect roi = new Rect(x, y, tileSize, tileSize);
                    using Mat tile = new Mat(paddedMat, roi);

                    using Mat tileRgb = new Mat();
                    Cv2.CvtColor(tile, tileRgb, ColorConversionCodes.BGR2RGB);
                    Tensor<float> inputTensor = ConvertMatToTensor(tileRgb);

                    var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(inputName, inputTensor) };
                    using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);
                    Tensor<float> outputTensor = results.First().AsTensor<float>();

                    using Bitmap outTileBmp = ConvertTensorToBitmap(outputTensor);
                    using Mat outTile = outTileBmp.ToMat();
                    using Mat downsampledTile = new Mat();
                    Cv2.Resize(
                        outTile,
                        downsampledTile,
                        new OpenCvSharp.Size(tileSize, tileSize),
                        0,
                        0,
                        InterpolationFlags.Lanczos4);

                    Rect outRoi = new Rect(x, y, tileSize, tileSize);
                    using Mat destinationRoi = new Mat(resultMat, outRoi);
                    downsampledTile.CopyTo(destinationRoi);
                }
            }

            Rect finalCrop = new Rect(0, 0, origW, origH);
            using Mat finalMat = new Mat(resultMat, finalCrop);

            cancellationToken.ThrowIfCancellationRequested();
            return finalMat.ToBitmap();
        }

        public void Dispose() {
            session?.Dispose();
            session = null;
        }

        // --- 전처리/후처리 수학 연산 헬퍼 메서드 ---
        private Tensor<float> ConvertMatToTensor(Mat mat) {
            int width = mat.Width;
            int height = mat.Height;
            var tensor = new DenseTensor<float>(new[] { 1, 3, height, width });
            var indexer = mat.GetGenericIndexer<Vec3b>();

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    Vec3b color = indexer[y, x];
                    tensor[0, 0, y, x] = color.Item0 / 255.0f;
                    tensor[0, 1, y, x] = color.Item1 / 255.0f;
                    tensor[0, 2, y, x] = color.Item2 / 255.0f;
                }
            }
            return tensor;
        }

        private Bitmap ConvertTensorToBitmap(Tensor<float> tensor) {
            int height = tensor.Dimensions[2];
            int width = tensor.Dimensions[3];

            using Mat outMat = new Mat(height, width, MatType.CV_8UC3);
            var indexer = outMat.GetGenericIndexer<Vec3b>();

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    float r = Math.Clamp(tensor[0, 0, y, x] * 255.0f, 0, 255);
                    float g = Math.Clamp(tensor[0, 1, y, x] * 255.0f, 0, 255);
                    float b = Math.Clamp(tensor[0, 2, y, x] * 255.0f, 0, 255);

                    indexer[y, x] = new Vec3b((byte)r, (byte)g, (byte)b);
                }
            }

            Cv2.CvtColor(outMat, outMat, ColorConversionCodes.RGB2BGR);
            return outMat.ToBitmap();
        }
    }
}
