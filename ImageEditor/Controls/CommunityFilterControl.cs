using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageEditor.Controls
{
    public partial class CommunityFilterControl : UserControl
    {
        public event Action<string>? FilterSelected;
        public event Action? ResetRequested;
        public event Action<int>? StickerSizeChanged;
        public event Action<int>? RotationChanged;
        public event Action<bool>? AutoFitChanged;
        public event Action? ApplyRequested;
        public event Action? CancelRequested;
        private Button? applyButton;
        private Button? cancelButton;
        private CheckBox? autoFitCheckBox;
        private bool applyAvailable;

        public bool AutoFitEnabled => autoFitCheckBox?.Checked ?? true;

        public void SetAutoFitAvailable(bool available)
        {
            if (autoFitCheckBox == null)
                return;

            if (!available)
                autoFitCheckBox.Checked = false;

            autoFitCheckBox.Enabled = available;
            autoFitCheckBox.Text = available
                ? "얼굴 자동 맞춤"
                : "얼굴 자동 맞춤 (로그인 필요)";
            autoFitCheckBox.AccessibleDescription = available
                ? "얼굴을 인식해 스티커 위치와 크기를 자동으로 맞춥니다."
                : "AI 얼굴 자동 맞춤은 로그인 후 사용할 수 있습니다.";
        }

        public Image DogImage => dogImage
            ?? throw new InvalidOperationException("강아지 스티커가 준비되지 않았습니다.");

        public Image FlowerImage
        {
            get { return picFlowerCrown.Image; }
        }

        public Image CatImage => catImage
            ?? throw new InvalidOperationException("고양이 스티커가 준비되지 않았습니다.");

        private Image? dogImage;
        private Image? dogEarImage;
        private Image? dogNoseImage;
        private Image? catImage;
        private Image? catEarImage;
        private Image? catNoseImage;
        private Image? bunnyImage;
        private Image? bunnyEarImage;
        private Image? bunnyNoseImage;
        private Image? princessCrownImage;
        private Image? angelHaloImage;
        private Image? devilHornsImage;

        public Image BunnyImage => bunnyImage
            ?? throw new InvalidOperationException("토끼 얼굴 오버레이가 준비되지 않았습니다.");

        public Image PrincessCrownImage => princessCrownImage
            ?? throw new InvalidOperationException("공주 왕관 오버레이가 준비되지 않았습니다.");

        public Image AngelHaloImage => angelHaloImage
            ?? throw new InvalidOperationException("천사 후광 오버레이가 준비되지 않았습니다.");

        public Image DevilHornsImage => devilHornsImage
            ?? throw new InvalidOperationException("악마 뿔 오버레이가 준비되지 않았습니다.");

        public bool TryGetSplitFilterImages(
            string filterName,
            out Image earImage,
            out Image noseImage)
        {
            Image? ear = null;
            Image? nose = null;

            switch (filterName)
            {
                case "DogLens":
                    ear = dogEarImage;
                    nose = dogNoseImage;
                    break;
                case "CatFace":
                    ear = catEarImage;
                    nose = catNoseImage;
                    break;
                case "BunnyFace":
                    ear = bunnyEarImage;
                    nose = bunnyNoseImage;
                    break;
            }

            earImage = ear!;
            noseImage = nose!;
            return ear != null && nose != null;
        }

        public CommunityFilterControl()
        {
            InitializeComponent();
            trkSize.Minimum = 5;
            LoadCommunityImages();
            ReplaceAdjustmentCardsWithOverlays();
            ConfigureAutoFitOption();
            ConfigureSessionButtons();
            Resize += (_, _) => UpdateResponsiveLayout();
            UpdateResponsiveLayout();
            SetAutoFitStatus("스티커 선택 시 얼굴에 자동으로 맞춥니다.");
            Disposed += CommunityFilterControl_Disposed;
        }

        private void ConfigureSessionButtons()
        {
            cancelButton = new Button { Location = new Point(96, 609), Size = new Size(72, 36), Text = "취소" };
            applyButton = new Button { Location = new Point(174, 609), Size = new Size(72, 36), Text = "적용", BackColor = Color.FromArgb(98, 78, 210), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Enabled = false };
            applyButton.FlatAppearance.BorderSize = 0;
            cancelButton.Click += (_, _) => CancelRequested?.Invoke();
            applyButton.Click += (_, _) => ApplyRequested?.Invoke();
            Controls.Add(cancelButton);
            Controls.Add(applyButton);
        }

        private void ConfigureAutoFitOption()
        {
            autoFitCheckBox = new CheckBox
            {
                AutoSize = true,
                Checked = true,
                CheckState = CheckState.Checked,
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(58, 72, 102),
                Location = new Point(12, 62),
                Name = "chkCommunityAutoFit",
                Text = "얼굴 자동 맞춤",
                UseVisualStyleBackColor = true
            };
            autoFitCheckBox.CheckedChanged += (_, _) =>
                AutoFitChanged?.Invoke(autoFitCheckBox.Checked);
            Controls.Add(autoFitCheckBox);
        }

        private void UpdateResponsiveLayout()
        {
            int contentWidth = Math.Max(190, ClientSize.Width - 24);
            label1.Left = 12;
            lblSubTitle.AutoSize = false;
            lblSubTitle.SetBounds(13, 40, contentWidth, 25);
            autoFitCheckBox?.SetBounds(12, 62, contentWidth, 22);
            const int columnGap = 12;
            int cardWidth = Math.Max(82, (contentWidth - columnGap) / 2);
            PictureBox[] pictures =
            {
                picBoldGlam, picCuteHeart, picDogLens, picFlowerCrown,
                picSoftGlam, picRetroStory, picKBeauty, picCatFace
            };
            Label[] labels = { label2, label3, label4, FlowerCrown, label8, label9, label6, label7 };

            for (int i = 0; i < pictures.Length; i++)
            {
                int column = i % 2;
                int row = i / 2;
                int left = 12 + column * (cardWidth + columnGap);
                int top = 88 + row * 96;
                pictures[i].SetBounds(left, top, cardWidth, 68);
                labels[i].AutoSize = false;
                labels[i].SetBounds(left, top + 70, cardWidth, 22);
                labels[i].TextAlign = ContentAlignment.MiddleCenter;
            }

            btnCommunityReset.Left = Math.Max(12, ClientSize.Width - btnCommunityReset.Width - 12);
            lbSize.Top = 478;
            trkSize.SetBounds(12, 492, contentWidth, trkSize.Height);
            lblRotation.Top = 536;
            trkRotation.SetBounds(12, 550, contentWidth, trkRotation.Height);
            AutoScrollMinSize = new Size(0, 657);

            if (cancelButton == null || applyButton == null)
                return;

            const int actionGap = 6;
            int actionWidth = Math.Max(72, (contentWidth - actionGap) / 2);
            cancelButton.SetBounds(12, 609, actionWidth, 36);
            applyButton.SetBounds(12 + actionWidth + actionGap, 609, actionWidth, 36);
        }

        public void SetApplyEnabled(bool enabled)
        {
            applyAvailable = enabled;
            if (applyButton != null) applyButton.Enabled = enabled;
        }

        public void SetAutoFitBusy(bool busy, string message, bool isError = false)
        {
            foreach (PictureBox picture in GetFilterPictures())
                picture.Enabled = !busy;

            btnCommunityReset.Enabled = !busy;
            trkSize.Enabled = !busy;
            trkRotation.Enabled = !busy;
            if (applyButton != null)
                applyButton.Enabled = !busy && applyAvailable;
            if (cancelButton != null)
                cancelButton.Enabled = true;

            SetAutoFitStatus(message, isError);
        }

        public void SetAutoFitStatus(string message, bool isError = false)
        {
            lblSubTitle.Text = message;
            lblSubTitle.ForeColor = isError
                ? Color.FromArgb(220, 38, 38)
                : Color.FromArgb(100, 116, 139);
        }

        public void SetOverlayTransformValues(float scale, float rotation)
        {
            int scaleValue = Math.Max(
                trkSize.Minimum,
                Math.Min(trkSize.Maximum, (int)Math.Round(scale * 100f)));
            int rotationValue = Math.Max(
                trkRotation.Minimum,
                Math.Min(trkRotation.Maximum, (int)Math.Round(rotation)));

            trkSize.Value = scaleValue;
            trkRotation.Value = rotationValue;
            lbSize.Text = $"크기 : {scaleValue}%";
            lblRotation.Text = $"회전 : {rotationValue}°";
        }

        private PictureBox[] GetFilterPictures()
        {
            return new[]
            {
                picBoldGlam,
                picCuteHeart,
                picDogLens,
                picFlowerCrown,
                picSoftGlam,
                picRetroStory,
                picKBeauty,
                picCatFace
            };
        }

        private void LoadCommunityImages()
        {
            picCuteHeart.Image = LoadCommunityImage("cute-heart.png");
            picFlowerCrown.Image = LoadCommunityImage("flower-crown.png");

            dogEarImage = LoadCommunityImage("dog-lens-ear.png");
            dogNoseImage = LoadCommunityImage("dog-lens-nose.png");
            catEarImage = LoadCommunityImage("cat-face-ear.png");
            catNoseImage = LoadCommunityImage("cat-face-nose.png");
            bunnyEarImage = LoadCommunityImage("bunny-face-ear.png");
            bunnyNoseImage = LoadCommunityImage("bunny-face-nose.png");

            dogImage = CreateSplitOverlayPreview(dogEarImage, dogNoseImage);
            catImage = CreateSplitOverlayPreview(catEarImage, catNoseImage);
            bunnyImage = CreateSplitOverlayPreview(bunnyEarImage, bunnyNoseImage);
            princessCrownImage = LoadCommunityImage("princess-crown.png");
            angelHaloImage = LoadCommunityImage("angel-halo.png");
            devilHornsImage = LoadCommunityImage("devil-horns.png");

            picDogLens.Image = dogImage;
            picCatFace.Image = catImage;
        }

        private static Bitmap CreateSplitOverlayPreview(Image ear, Image nose)
        {
            int canvasWidth = Math.Max(1, ear.Width);
            int earHeight = Math.Max(
                1,
                (int)Math.Round(ear.Height * (canvasWidth / (double)ear.Width)));
            int noseWidth = Math.Max(1, canvasWidth / 2);
            int noseHeight = Math.Max(
                1,
                (int)Math.Round(nose.Height * (noseWidth / (double)nose.Width)));
            int noseTop = earHeight + Math.Max(12, canvasWidth / 10);
            Bitmap result = new Bitmap(
                canvasWidth,
                noseTop + noseHeight,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using Graphics graphics = Graphics.FromImage(result);
            graphics.Clear(Color.Transparent);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(ear, 0, 0, canvasWidth, earHeight);
            graphics.DrawImage(
                nose,
                (canvasWidth - noseWidth) / 2,
                noseTop,
                noseWidth,
                noseHeight);
            return result;
        }

        private static Image LoadCommunityImage(string fileName)
        {
            string resourceName = $"ImageEditor.CommunityAssets.{fileName}";
            using Stream stream = typeof(CommunityFilterControl).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"스티커 이미지 리소스를 찾을 수 없습니다: {fileName}");
            using Image source = Image.FromStream(stream);
            return new Bitmap(source);
        }

        private void picBoldGlam_Click(object sender, EventArgs e)
        {
            FilterSelected?.Invoke("BunnyFace");
        }

        private void picCuteHeart_Click(object sender, EventArgs e)
        {
            FilterSelected?.Invoke("CuteHeart");
        }

        private void picDogLens_Click(object sender, EventArgs e)
        {
            FilterSelected?.Invoke("DogLens");
        }
        private void picSoftGlam_Click(object sender, EventArgs e)
        {
            FilterSelected?.Invoke("PrincessCrown");
        }

        private void picRetroStory_Click(object sender, EventArgs e)
        {
            FilterSelected?.Invoke("AngelHalo");
        }
        private void picFlowerCrown_Click(object sender, EventArgs e)
        {
            FilterSelected?.Invoke("FlowerCrown");
        }

        private void picKBeauty_Click(object sender, EventArgs e)
        {
            FilterSelected?.Invoke("DevilHorns");
        }

        private void picCatFace_Click(object sender, EventArgs e)
        {
            FilterSelected?.Invoke("CatFace");
        }

        private void btnCommunityReset_Click(object sender, EventArgs e)
        {
            ResetRequested?.Invoke();
        }

        private void trkSize_Scroll(object sender, EventArgs e)
        {
            lbSize.Text = $"크기 : {trkSize.Value}%";
            StickerSizeChanged?.Invoke(trkSize.Value);
        }

        private void trkRotation_Scroll(object sender, EventArgs e)
        {
            lblRotation.Text = $"회전 : {trkRotation.Value}°";
            RotationChanged?.Invoke(trkRotation.Value);
        }

        // 기존 카드의 위치와 Designer 이벤트 연결은 그대로 사용하고,
        // 보정 카드 4개만 새 오버레이 카드로 교체한다.
        private void ReplaceAdjustmentCardsWithOverlays()
        {
            picBoldGlam.Image = bunnyImage;
            picSoftGlam.Image = princessCrownImage;
            picRetroStory.Image = angelHaloImage;
            picRetroStory.SizeMode = PictureBoxSizeMode.Zoom;
            picKBeauty.Image = devilHornsImage;

            label2.Text = "토끼 얼굴";
            label8.Text = "공주 왕관";
            label9.Text = "천사 후광";
            label6.Text = "악마 뿔";
        }

        private void CommunityFilterControl_Disposed(object? sender, EventArgs e)
        {
            picCuteHeart.Image?.Dispose();
            picFlowerCrown.Image?.Dispose();
            dogImage?.Dispose();
            dogEarImage?.Dispose();
            dogNoseImage?.Dispose();
            catImage?.Dispose();
            catEarImage?.Dispose();
            catNoseImage?.Dispose();
            bunnyImage?.Dispose();
            bunnyEarImage?.Dispose();
            bunnyNoseImage?.Dispose();
            princessCrownImage?.Dispose();
            angelHaloImage?.Dispose();
            devilHornsImage?.Dispose();
            dogImage = null;
            dogEarImage = null;
            dogNoseImage = null;
            catImage = null;
            catEarImage = null;
            catNoseImage = null;
            bunnyImage = null;
            bunnyEarImage = null;
            bunnyNoseImage = null;
            princessCrownImage = null;
            angelHaloImage = null;
            devilHornsImage = null;
        }

        private static Bitmap CreateBunnyFaceImage()
        {
            Bitmap bmp = new Bitmap(1000, 700);
            using Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using Pen outline = new Pen(Color.FromArgb(245, 245, 245), 22) { LineJoin = LineJoin.Round };
            using Brush earBrush = new SolidBrush(Color.FromArgb(255, 250, 252));
            using Brush innerBrush = new SolidBrush(Color.FromArgb(255, 185, 210));
            using Brush noseBrush = new SolidBrush(Color.FromArgb(255, 128, 170));
            using Pen whiskerPen = new Pen(Color.FromArgb(235, 245, 245, 245), 10) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            using GraphicsPath leftEar = new GraphicsPath();
            leftEar.AddBezier(205, 330, 120, 185, 125, 35, 270, 40);
            leftEar.AddBezier(270, 40, 335, 150, 340, 275, 295, 350);
            leftEar.CloseFigure();
            g.FillPath(earBrush, leftEar);
            g.DrawPath(outline, leftEar);

            using GraphicsPath rightEar = new GraphicsPath();
            rightEar.AddBezier(795, 330, 880, 185, 875, 35, 730, 40);
            rightEar.AddBezier(730, 40, 665, 150, 660, 275, 705, 350);
            rightEar.CloseFigure();
            g.FillPath(earBrush, rightEar);
            g.DrawPath(outline, rightEar);

            g.FillEllipse(innerBrush, 190, 95, 90, 190);
            g.FillEllipse(innerBrush, 720, 95, 90, 190);

            Point[] nose = { new Point(500, 420), new Point(455, 455), new Point(500, 495), new Point(545, 455) };
            g.FillPolygon(noseBrush, nose);
            using Pen smile = new Pen(Color.White, 9);
            g.DrawArc(smile, 466, 472, 68, 55, 15, 150);

            g.DrawLine(whiskerPen, 420, 480, 210, 430);
            g.DrawLine(whiskerPen, 420, 515, 185, 520);
            g.DrawLine(whiskerPen, 580, 480, 790, 430);
            g.DrawLine(whiskerPen, 580, 515, 815, 520);

            using Brush blush = new SolidBrush(Color.FromArgb(115, 255, 150, 185));
            g.FillEllipse(blush, 315, 470, 95, 45);
            g.FillEllipse(blush, 590, 470, 95, 45);
            return bmp;
        }

        private static Bitmap CreatePrincessCrownImage()
        {
            Bitmap bmp = new Bitmap(1000, 650);
            using Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using Brush gold = new SolidBrush(Color.FromArgb(255, 215, 70));
            using Brush goldLight = new SolidBrush(Color.FromArgb(255, 238, 125));
            using Pen outline = new Pen(Color.FromArgb(255, 184, 48), 12) { LineJoin = LineJoin.Round };
            using Brush pinkGem = new SolidBrush(Color.FromArgb(255, 105, 160));
            using Brush blueGem = new SolidBrush(Color.FromArgb(102, 190, 255));
            using Brush sparkle = new SolidBrush(Color.White);

            Point[] crown = {
                new Point(230, 315), new Point(310, 120), new Point(430, 270),
                new Point(500, 80), new Point(570, 270), new Point(690, 120),
                new Point(770, 315), new Point(735, 420), new Point(265, 420)
            };
            g.FillPolygon(gold, crown);
            g.DrawPolygon(outline, crown);
            g.FillRectangle(goldLight, 270, 355, 460, 70);
            g.DrawRectangle(outline, 270, 355, 460, 70);

            g.FillEllipse(pinkGem, 455, 325, 90, 90);
            g.FillEllipse(blueGem, 325, 348, 56, 56);
            g.FillEllipse(blueGem, 619, 348, 56, 56);

            DrawSparkle(g, sparkle, 180, 170, 34);
            DrawSparkle(g, sparkle, 820, 195, 30);
            DrawSparkle(g, sparkle, 785, 70, 22);
            DrawSparkle(g, sparkle, 210, 455, 23);
            return bmp;
        }

        private static Bitmap CreateAngelHaloImage()
        {
            Bitmap bmp = new Bitmap(1000, 650);
            using Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using Pen haloGlow = new Pen(Color.FromArgb(110, 255, 240, 135), 42);
            using Pen halo = new Pen(Color.FromArgb(255, 238, 105), 18);
            using Brush sparkle = new SolidBrush(Color.FromArgb(245, 255, 255, 240));

            Rectangle haloRect = new Rectangle(245, 120, 510, 165);
            g.DrawEllipse(haloGlow, haloRect);
            g.DrawEllipse(halo, haloRect);

            DrawSparkle(g, sparkle, 205, 170, 28);
            DrawSparkle(g, sparkle, 795, 200, 24);
            DrawSparkle(g, sparkle, 735, 85, 18);
            DrawSparkle(g, sparkle, 290, 75, 16);

            using Pen ray = new Pen(Color.FromArgb(190, 255, 244, 160), 9)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawLine(ray, 500, 52, 500, 12);
            g.DrawLine(ray, 185, 235, 145, 252);
            g.DrawLine(ray, 815, 235, 855, 252);
            return bmp;
        }

        private static Bitmap CreateDevilHornsImage()
        {
            Bitmap bmp = new Bitmap(1000, 700);
            using Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using Brush hornBrush = new LinearGradientBrush(
                new Rectangle(0, 0, 1000, 500),
                Color.FromArgb(255, 85, 95),
                Color.FromArgb(165, 0, 35),
                90f);
            using Pen outline = new Pen(Color.FromArgb(120, 0, 25), 16) { LineJoin = LineJoin.Round };
            using Brush sparkle = new SolidBrush(Color.FromArgb(235, 255, 145, 160));

            using GraphicsPath leftHorn = new GraphicsPath();
            leftHorn.AddBezier(305, 365, 170, 310, 145, 120, 225, 42);
            leftHorn.AddBezier(225, 42, 245, 165, 345, 195, 385, 335);
            leftHorn.CloseFigure();
            g.FillPath(hornBrush, leftHorn);
            g.DrawPath(outline, leftHorn);

            using GraphicsPath rightHorn = new GraphicsPath();
            rightHorn.AddBezier(695, 365, 830, 310, 855, 120, 775, 42);
            rightHorn.AddBezier(775, 42, 755, 165, 655, 195, 615, 335);
            rightHorn.CloseFigure();
            g.FillPath(hornBrush, rightHorn);
            g.DrawPath(outline, rightHorn);

            using Pen brow = new Pen(Color.FromArgb(205, 110, 15, 35), 14)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            g.DrawArc(brow, 320, 350, 150, 80, 205, 105);
            g.DrawArc(brow, 530, 350, 150, 80, 230, 105);

            DrawSparkle(g, sparkle, 160, 365, 20);
            DrawSparkle(g, sparkle, 840, 365, 20);
            return bmp;
        }

        private static void DrawSparkle(Graphics graphics, Brush brush, float x, float y, float radius)
        {
            PointF[] points = {
                new PointF(x, y - radius), new PointF(x + radius * 0.22f, y - radius * 0.22f),
                new PointF(x + radius, y), new PointF(x + radius * 0.22f, y + radius * 0.22f),
                new PointF(x, y + radius), new PointF(x - radius * 0.22f, y + radius * 0.22f),
                new PointF(x - radius, y), new PointF(x - radius * 0.22f, y - radius * 0.22f)
            };
            graphics.FillPolygon(brush, points);
        }
    }
}
