using ImageEditor.ImageProcessing;
using ImageEditor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ImageEditor.Core
{
    public partial class EditorController : IDisposable
    {
        public EditorState State { get; private set; }

        private ImageFileService fileService;
        private SnsExportService snsExportService;
        private bool disposed;

        public EditorController()
        {
            State = new EditorState();
            fileService = new ImageFileService();
            snsExportService = new SnsExportService();
        }

        public bool OpenImage()
        {
            Bitmap? image = fileService.OpenImage(out string? filePath);

            if (image == null)
                return false;

            State.OriginalImage?.Dispose();
            State.CurrentImage?.Dispose();

            State.OriginalImage = new Bitmap(image);

            State.CurrentImage = new Bitmap(image);

            State.CurrentFilePath = filePath;

            image.Dispose();

            InitializeLayers();
            ClearUndoHistory();
            ResetMaskState();
            State.MarkSaved();

            return true;
        }
        // 현재 파일 경로에 저장
        public bool Save()
        {
            if (State.CurrentImage == null)
                return false;

            // 저장 경로가 없으면 다른 이름으로 저장
            if (string.IsNullOrEmpty(State.CurrentFilePath))
            {
                return SaveAs();
            }

            bool saved = fileService.SaveImage(State.CurrentImage, State.CurrentFilePath);
            if (saved)
                State.MarkSaved();

            return saved;
        }
        // 다른 이름으로 저장
        public bool SaveAs()
        {
            if (State.CurrentImage == null)
                return false;

            bool result = fileService.SaveImageAs(State.CurrentImage, out string filePath);

            // 저장 성공하면 현재 파일 경로도 새 경로로 변경
            if (result)
            {
                State.CurrentFilePath = filePath;
                State.MarkSaved();
            }

            return result;
        }

        // 왼쪽 회전
        public void RotateLeft()
        {
            if (State.CurrentImage == null)
                return;

            if (State.Layers == null || State.Layers.Count == 0)
            {
                Bitmap oldImage = State.CurrentImage;
                State.CurrentImage = BasicImageProcessor.RotateLeft(oldImage);
                oldImage.Dispose();
                return;
            }

            TransformAllLayers(BasicImageProcessor.RotateLeft);
            RenderLayers();
        }

        private void TransformAllLayers(Func<Bitmap, Bitmap> transform)
        {
            if (State.Layers == null)
                return;

            foreach (Layer layer in State.Layers)
            {
                if (layer.Image != null)
                {
                    Bitmap oldLayer = layer.Image;
                    layer.Image = transform(oldLayer);
                    oldLayer.Dispose();
                }

            }
        }

        // 오른쪽 회전

        public void RotateRight()
        {
            if (State.CurrentImage == null)
                return;

            if (State.Layers == null || State.Layers.Count == 0)
            {
                Bitmap oldImage = State.CurrentImage;

                State.CurrentImage =
                    BasicImageProcessor.RotateRight(oldImage);

                oldImage.Dispose();

                return;
            }

            foreach (Layer layer in State.Layers)
            {
                if (layer.Image != null)
                {
                    Bitmap oldLayer = layer.Image;

                    layer.Image =
                        BasicImageProcessor.RotateRight(oldLayer);

                    oldLayer.Dispose();
                }

            }

            RenderLayers();
        }

        // 좌우 반전
        public void FlipHorizontal()
        {
            if (State.CurrentImage == null)
                return;

            if (State.Layers == null || State.Layers.Count == 0)
            {
                Bitmap oldImage = State.CurrentImage;
                State.CurrentImage = BasicImageProcessor.FlipHorizontal(oldImage);
                oldImage.Dispose();
                return;
            }

            TransformAllLayers(BasicImageProcessor.FlipHorizontal);
            RenderLayers();
        }

        // 상하 반전
        public void FlipVertical()
        {
            if (State.CurrentImage == null)
                return;

            if (State.Layers == null || State.Layers.Count == 0)
            {
                Bitmap oldImage = State.CurrentImage;
                State.CurrentImage = BasicImageProcessor.FlipVertical(oldImage);
                oldImage.Dispose();
                return;
            }

            TransformAllLayers(BasicImageProcessor.FlipVertical);
            RenderLayers();
        }

        // 확대
        public void ZoomIn()
        {
            if (State.CurrentImage == null)
                return;

            State.SetZoom((float)Math.Round(State.ZoomRatio * 1.1f, 4));
        }

        // 축소
        public void ZoomOut()
        {
            if (State.CurrentImage == null)
                return;

            State.SetZoom((float)Math.Round(State.ZoomRatio / 1.1f, 4));
        }


        // 현재 이미지를 pnlCanvas 크기에 맞춤
        public void FitToPanel(Size panelSize)
        {
            if (State.CurrentImage == null)
                return;

            float zoom = CanvasViewCalculator.GetFitZoom(State.CurrentImage.Size, panelSize);

            State.SetZoom(zoom);
        }

        public void Resize(int width, int height)
        {
            if (State.CurrentImage == null)
                return;

            if (width <= 0 || height <= 0)
                return;

            if (State.Layers == null || State.Layers.Count == 0)
            {
                Bitmap oldImage = State.CurrentImage;

                State.CurrentImage =
                    BasicImageProcessor.Resize(
                        oldImage,
                        width,
                        height);

                oldImage.Dispose();

                return;
            }

            foreach (Layer layer in State.Layers)
            {
                if (layer.Image != null)
                {
                    Bitmap oldLayer = layer.Image;

                    layer.Image =
                        BasicImageProcessor.Resize(
                            oldLayer,
                            width,
                            height);

                    oldLayer.Dispose();
                }

            }

            RenderLayers();
        }

        // 실제 이미지 크기 변경
        public void ResizeImage(int width, int height)
        {
            if (State.CurrentImage == null)
                return;

            Bitmap result = BasicImageProcessor.Resize(State.CurrentImage, width, height);

            if (result == null)
                return;

            State.ReplaceCurrentImage(result);
            InitializeLayers();
        }

        // 선택 영역으로 이미지 자르기
        public void CropImage(Rectangle cropArea)
        {
            if (State.CurrentImage == null)
                return;

            Bitmap? result = BasicImageProcessor.Crop(State.CurrentImage, cropArea);

            if (result == null)
                return;

            State.ReplaceCurrentImage(result);
            InitializeLayers();
        }
        // JPG 품질 내보내기
        public bool ExportJpeg(
            long quality)
        {
            if (State.CurrentImage == null)
                return false;

            return fileService.ExportJpeg(
                State.CurrentImage,
                quality
            );
        }


        // PNG 내보내기
        public bool ExportPng()
        {
            if (State.CurrentImage == null)
                return false;

            return fileService.ExportPng(
                State.CurrentImage
            );
        }


        // SNS / 웹 최적화 내보내기
        public bool ExportForSns(
            int width,
            int height,
            long quality,
            string fileName)
        {
            if (State.CurrentImage == null)
                return false;

            return snsExportService.Export(
                State.CurrentImage,
                width,
                height,
                quality,
                fileName
            );
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            ClearUndoHistory();
            ResetMaskState();

            foreach (Layer layer in State.Layers)
                layer.Image?.Dispose();

            State.Layers.Clear();

            Bitmap? currentImage = State.CurrentImage;
            Bitmap? originalImage = State.OriginalImage;
            State.CurrentImage = null;
            State.OriginalImage = null;

            currentImage?.Dispose();
            if (!ReferenceEquals(originalImage, currentImage))
                originalImage?.Dispose();
        }
    

        // ===== Layer / Drawing features =====

        public void InitializeLayers()
        {
            if (State.Layers == null)
                State.Layers = new List<Layer>();

            foreach (Layer layer in State.Layers)
                layer.Image?.Dispose();

            State.Layers.Clear();

            if (State.CurrentImage == null)
                return;

            Layer background = new Layer(
                "배경",
                new Bitmap(State.CurrentImage));
            State.Layers.Add(background);

            Bitmap drawingBitmap = new Bitmap(
                State.CurrentImage.Width,
                State.CurrentImage.Height,
                PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(drawingBitmap))
                graphics.Clear(Color.Transparent);

            Layer drawingLayer = new Layer(
                "그리기 1",
                drawingBitmap);
            State.Layers.Add(drawingLayer);

            State.SelectedLayerIndex = 1;
            RenderLayers();
        }


        public void AddLayer()
        {
            if (State.CurrentImage == null)
                return;

            Bitmap bitmap =
                new Bitmap(
                    State.CurrentImage.Width,
                    State.CurrentImage.Height,
                    PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
            }

            Layer newLayer =
                new Layer(
                    "레이어 " +
                    (State.Layers.Count + 1),
                    bitmap);

            int insertIndex =
                State.SelectedLayerIndex + 1;

            if (insertIndex < 0 ||
                insertIndex > State.Layers.Count)
            {
                insertIndex = State.Layers.Count;
            }

            State.Layers.Insert(
                insertIndex,
                newLayer);

            State.SelectedLayerIndex =
                insertIndex;

            RenderLayers();
        }


        // 이미지 파일을 새 레이어로 추가
        // 현재 캔버스 크기에 맞춰 비율을 유지하면서 중앙에 배치한다.
        public bool AddImageLayer(Bitmap sourceImage, string? layerName = null)
        {
            if (sourceImage == null || State.CurrentImage == null)
                return false;

            if (sourceImage.Width <= 0 || sourceImage.Height <= 0)
                return false;

            int canvasWidth = State.CurrentImage.Width;
            int canvasHeight = State.CurrentImage.Height;

            Bitmap layerBitmap =
                new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(layerBitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                float scale = Math.Min(
                    (float)canvasWidth / sourceImage.Width,
                    (float)canvasHeight / sourceImage.Height);

                // 너무 작은 이미지는 원본 크기를 유지하고, 큰 이미지만 캔버스 안으로 맞춘다.
                scale = Math.Min(1f, scale);

                int drawWidth = Math.Max(1, (int)Math.Round(sourceImage.Width * scale));
                int drawHeight = Math.Max(1, (int)Math.Round(sourceImage.Height * scale));

                int x = (canvasWidth - drawWidth) / 2;
                int y = (canvasHeight - drawHeight) / 2;

                g.DrawImage(
                    sourceImage,
                    new Rectangle(x, y, drawWidth, drawHeight),
                    0, 0, sourceImage.Width, sourceImage.Height,
                    GraphicsUnit.Pixel);
            }

            string name = string.IsNullOrWhiteSpace(layerName)
                ? "이미지 레이어"
                : layerName;

            Layer newLayer = new Layer(name, layerBitmap);

            int insertIndex = State.SelectedLayerIndex + 1;
            if (insertIndex < 1)
                insertIndex = 1;
            if (insertIndex > State.Layers.Count)
                insertIndex = State.Layers.Count;

            State.Layers.Insert(insertIndex, newLayer);
            State.SelectedLayerIndex = insertIndex;

            RenderLayers();
            return true;
        }


        public void DeleteLayer()
        {
            if (State.SelectedLayerIndex <= 0)
                return;

            if (State.SelectedLayerIndex >= State.Layers.Count)
                return;

            Layer layer =
                State.Layers[State.SelectedLayerIndex];

            if (layer.Image != null)
                layer.Image.Dispose();

            State.Layers.RemoveAt(
                State.SelectedLayerIndex);

            if (State.Layers.Count == 0)
            {
                State.SelectedLayerIndex = -1;
            }
            else if (
                State.SelectedLayerIndex >=
                State.Layers.Count)
            {
                State.SelectedLayerIndex =
                    State.Layers.Count - 1;
            }

            RenderLayers();
        }


        public void SelectLayer(int index)
        {
            if (index < 0 ||
                index >= State.Layers.Count)
                return;

            State.SelectedLayerIndex = index;

            RenderLayers();
        }


        public Layer? GetSelectedLayer()
        {
            if (State.SelectedLayerIndex < 0 ||
                State.SelectedLayerIndex >=
                State.Layers.Count)
            {
                return null;
            }

            return State.Layers[
                State.SelectedLayerIndex];
        }


        public void ToggleLayerVisible()
        {
            Layer layer = GetSelectedLayer();

            if (layer == null)
                return;

            layer.Visible = !layer.Visible;

            RenderLayers();
        }


        public void SetLayerOpacity(float opacity)
        {
            Layer layer = GetSelectedLayer();

            if (layer == null)
                return;

            opacity =
                Math.Max(
                    0f,
                    Math.Min(
                        1f,
                        opacity));

            layer.Opacity = opacity;

            RenderLayers();
        }


        public void ToggleLayerLock()
        {
            Layer layer = GetSelectedLayer();

            if (layer == null)
                return;

            layer.Locked = !layer.Locked;
        }


        public void RenameLayer(string name)
        {
            Layer layer = GetSelectedLayer();

            if (layer == null)
                return;

            if (string.IsNullOrWhiteSpace(name))
                return;

            layer.Name = name;
        }


        public void MoveLayerUp()
        {
            int index =
                State.SelectedLayerIndex;

            // 0번은 배경 레이어이므로 위로 이동시키지 않는다.
            if (index <= 0 ||
                index >= State.Layers.Count - 1)
                return;

            Layer temp =
                State.Layers[index];

            State.Layers[index] =
                State.Layers[index + 1];

            State.Layers[index + 1] =
                temp;

            State.SelectedLayerIndex =
                index + 1;

            RenderLayers();
        }


        public void MoveLayerDown()
        {
            int index =
                State.SelectedLayerIndex;

            if (index <= 1)
                return;

            if (index >= State.Layers.Count)
                return;

            Layer temp =
                State.Layers[index];

            State.Layers[index] =
                State.Layers[index - 1];

            State.Layers[index - 1] =
                temp;

            State.SelectedLayerIndex =
                index - 1;

            RenderLayers();
        }


        public void DuplicateLayer()
        {
            Layer current =
                GetSelectedLayer();

            if (current == null ||
                current.Image == null)
                return;

            Bitmap copy =
                new Bitmap(current.Image);

            Layer duplicate =
                new Layer(
                    current.Name + " 복사본",
                    copy);

            duplicate.Visible =
                current.Visible;

            duplicate.Opacity =
                current.Opacity;

            duplicate.Locked = false;

            int newIndex =
                State.SelectedLayerIndex + 1;

            State.Layers.Insert(
                newIndex,
                duplicate);

            State.SelectedLayerIndex =
                newIndex;

            RenderLayers();
        }


        public void ClearCurrentLayer()
        {
            Layer layer =
                GetSelectedLayer();

            if (layer == null ||
                layer.Image == null)
                return;

            if (layer.Locked)
                return;

            using (Graphics g =
                   Graphics.FromImage(layer.Image))
            {
                g.Clear(Color.Transparent);
            }

            RenderLayers();
        }


        public void DrawLine(
            Point start,
            Point end,
            Color color,
            float width,
            bool eraser,
            bool renderLayers = true)
        {
            Layer layer =
                GetSelectedLayer();

            if (layer == null ||
                layer.Image == null)
                return;

            if (layer.Locked)
                return;

            if (width <= 0)
                width = 1;

            using (Graphics g =
                   Graphics.FromImage(layer.Image))
            {
                g.SmoothingMode =
                    SmoothingMode.AntiAlias;

                if (eraser)
                {
                    g.CompositingMode =
                        CompositingMode.SourceCopy;

                    using (Pen pen =
                           new Pen(
                               Color.Transparent,
                               width))
                    {
                        pen.StartCap =
                            LineCap.Round;

                        pen.EndCap =
                            LineCap.Round;

                        g.DrawLine(
                            pen,
                            start,
                            end);
                    }
                }
                else
                {
                    g.CompositingMode =
                        CompositingMode.SourceOver;

                    using (Pen pen =
                           new Pen(
                               color,
                               width))
                    {
                        pen.StartCap =
                            LineCap.Round;

                        pen.EndCap =
                            LineCap.Round;

                        g.DrawLine(
                            pen,
                            start,
                            end);
                    }
                }
            }

            if (renderLayers)
                RenderLayers();
        }


        public void RenderLayers()
        {
            if (State.Layers == null || State.Layers.Count == 0)
                return;

            Layer firstLayer = State.Layers[0];
            if (firstLayer.Image == null)
                return;

            int width = firstLayer.Image.Width;
            int height = firstLayer.Image.Height;
            Bitmap result = new Bitmap(
                width,
                height,
                PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(result))
            {
                graphics.Clear(Color.Transparent);

                foreach (Layer layer in State.Layers)
                {
                    if (layer.Image == null || !layer.Visible)
                        continue;

                    float opacity = Math.Max(0f, Math.Min(1f, layer.Opacity));
                    using ImageAttributes attributes = new ImageAttributes();
                    ColorMatrix matrix = new ColorMatrix
                    {
                        Matrix33 = opacity
                    };
                    attributes.SetColorMatrix(
                        matrix,
                        ColorMatrixFlag.Default,
                        ColorAdjustType.Bitmap);

                    graphics.DrawImage(
                        layer.Image,
                        new Rectangle(0, 0, width, height),
                        0,
                        0,
                        layer.Image.Width,
                        layer.Image.Height,
                        GraphicsUnit.Pixel,
                        attributes);
                }
            }

            State.CurrentImage?.Dispose();
            State.CurrentImage = result;
        }


        public void ResetToOriginal()
        {
            if (State.OriginalImage == null)
                return;

            if (State.CurrentImage != null)
                State.CurrentImage.Dispose();

            State.CurrentImage =
                new Bitmap(
                    State.OriginalImage);

            InitializeLayers();
            ResetMaskState();
        }
}
}
