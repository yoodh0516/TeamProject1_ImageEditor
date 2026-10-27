using ImageEditor.Controls;
using ImageEditor.Core;
using ImageEditor.ImageProcessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageEditor.Tools
{
    public class CropTool : IDisposable
    {
        private readonly PictureBox pictureBox;

        private readonly EditorState state;

        private readonly CropControl cropControl;

        private Bitmap? previousImage;

        private Bitmap? croppedImageReference;

        // 현재 Crop 기능 사용 중인지
        private bool isActive;

        // 현재 드래그 중인지
        private bool isDragging;

        // 마우스를 처음 누른 위치
        private Point startPoint;

        // PictureBox 기준 선택 영역
        private Rectangle selectionArea = Rectangle.Empty;

        // Crop이 적용되었음을 MainForm에 알림
        public event Action? CropApplied;
        public event Action? CropApplying;

        public CropTool(PictureBox pictureBox, EditorState state, CropControl cropControl)
        {
            this.pictureBox = pictureBox;

            this.state = state;

            this.cropControl = cropControl;

            ConnectEvents();
        }
        // 필요한 이벤트 연결
        private void ConnectEvents()
        {
            pictureBox.MouseDown += PictureBox_MouseDown;

            pictureBox.MouseMove += PictureBox_MouseMove;

            pictureBox.MouseUp += PictureBox_MouseUp;

            pictureBox.Paint += PictureBox_Paint;

            cropControl.ApplyRequested += ApplyCrop;

            cropControl.ResetRequested += ResetSelection;

            cropControl.UndoRequested += UndoCrop;

        }

        private void UndoCrop()
        {
            if (previousImage == null ||
                croppedImageReference == null ||
                !ReferenceEquals(state.CurrentImage, croppedImageReference))
            {
                ClearPreviousCrop();
                MessageBox.Show( "되돌릴 자르기 작업이 없습니다.");

                return;
            }


            // 이전 이미지 복사본 생성
            Bitmap restoreImage = new Bitmap(previousImage);

            // 현재 이미지와 교체
            state.ReplaceCurrentImage( restoreImage);

            // 이전 이미지 정리
            previousImage.Dispose();
            previousImage = null;
            croppedImageReference = null;

            // 선택 영역 초기화
            selectionArea =Rectangle.Empty;

            isDragging = false;

            cropControl.SetSelectionBounds(Rectangle.Empty);

            // MainForm에 이미지 변경 알림
            CropApplied?.Invoke();

            pictureBox.Invalidate();
        }

        private void ApplyCrop()
        {
            if (!isActive)
                return;

            if (state.CurrentImage == null)
                return;

            if (selectionArea.Width <= 0 ||selectionArea.Height <= 0)
            {
                MessageBox.Show( "자를 영역을 먼저 선택해주세요.");
                return;
            }

            Rectangle imageArea = ConvertToImageArea();


            // 실제 자르기
            Bitmap? result = BasicImageProcessor.Crop(state.CurrentImage, imageArea);

            if (result == null)
                return;

            // 이전 Crop 이미지 정리
            previousImage?.Dispose();

            // 현재 이미지를 Crop 전 상태로 저장
            previousImage = new Bitmap(state.CurrentImage);

            // Undo 스냅샷은 패널을 열 때가 아니라 실제 적용 직전에만 남긴다.
            CropApplying?.Invoke();
            state.ReplaceCurrentImage( result);
            croppedImageReference = state.CurrentImage;

            // MainForm에 이미지 변경 알림
            CropApplied?.Invoke();

            Stop();
        }

        private void PictureBox_Paint(object? sender, PaintEventArgs e)
        {
            if (!isActive)
                return;

            if (selectionArea.Width <= 0 ||
                selectionArea.Height <= 0)
                return;


            using (Pen pen = new Pen(Color.Red, 2))
            {
                e.Graphics.DrawRectangle(pen,selectionArea);
            }
        }

        private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
        {
            if (!isActive)
                return;

            if (!isDragging)
                return;

            isDragging = false;

            Rectangle imageArea = ConvertToImageArea();

            cropControl.SetSelectionBounds(imageArea);
        }

        private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!isActive)
                return;

            if (!isDragging)
                return;


            int x =Math.Min(startPoint.X,e.X);

            int y =Math.Min(startPoint.Y, e.Y);

            int width = Math.Abs( e.X - startPoint.X);

            int height =Math.Abs( e.Y - startPoint.Y);


            selectionArea = new Rectangle( x, y, width,height);

            // 선택 사각형 다시 그리기
            pictureBox.Invalidate();
        }

        private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (!isActive)
                return;

            if (state.CurrentImage == null)
                return;

            if (e.Button != MouseButtons.Left)
                return;

            isDragging = true;

            startPoint = e.Location;

            selectionArea =Rectangle.Empty;
        }

        // 자르기 도구 시작
        public void Start()
        {
            if (state.CurrentImage == null)
                return;

            isActive = true;

            isDragging = false;

            selectionArea = Rectangle.Empty;

            pictureBox.Cursor = Cursors.Cross;

            cropControl.SetSelectionBounds(Rectangle.Empty);

            pictureBox.Invalidate();
        }
        // 자르기 도구 종료
        public void Stop()
        {
            isActive = false;

            isDragging = false;

            selectionArea =Rectangle.Empty;

            pictureBox.Cursor =Cursors.Default;

            pictureBox.Invalidate();
        }

        private Rectangle ConvertToImageArea()
        {
            if (state.CurrentImage == null)
                return Rectangle.Empty;

            if (pictureBox.Width <= 0 || pictureBox.Height <= 0)
                return Rectangle.Empty;

            float scaleX = (float)state.CurrentImage.Width /pictureBox.Width;

            float scaleY =(float)state.CurrentImage.Height / pictureBox.Height;

            int x =(int)( selectionArea.X *scaleX);

            int y =(int)(selectionArea.Y * scaleY);

            int width = (int)(selectionArea.Width * scaleX);

            int height = (int)( selectionArea.Height * scaleY );

            Rectangle imageArea = new Rectangle(x, y, width, height);
            Rectangle imageBounds = new Rectangle(
                0,
                0,
                state.CurrentImage.Width,
                state.CurrentImage.Height);

            return Rectangle.Intersect(imageArea, imageBounds);
        }

        // 선택 영역 초기화
        private void ResetSelection()
        {
            // 기존 선택 영역 제거
            selectionArea = Rectangle.Empty;

            // 드래그 상태도 해제
            isDragging = false;

            // 오른쪽 선택 영역 표시 초기화
            cropControl.SetSelectionBounds(Rectangle.Empty);

            // 다시 자를 수 있도록 Crop 모드는 유지
            isActive = true;

            // 마우스 모양도 계속 자르기 상태
            pictureBox.Cursor =
                Cursors.Cross;

            // 선택 사각형 다시 그리기
            pictureBox.Invalidate();
        }

        public void ClearPreviousCrop()
        {
            previousImage?.Dispose();
            previousImage = null;
            croppedImageReference = null;
        }

        public void Dispose()
        {
            Stop();
            ClearPreviousCrop();

            pictureBox.MouseDown -= PictureBox_MouseDown;
            pictureBox.MouseMove -= PictureBox_MouseMove;
            pictureBox.MouseUp -= PictureBox_MouseUp;
            pictureBox.Paint -= PictureBox_Paint;

            cropControl.ApplyRequested -= ApplyCrop;
            cropControl.ResetRequested -= ResetSelection;
            cropControl.UndoRequested -= UndoCrop;
        }
    }
}
