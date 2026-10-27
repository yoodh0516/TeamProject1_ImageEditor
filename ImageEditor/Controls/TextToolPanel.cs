using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ImageEditor.Controls
{
    public partial class TextToolPanel : UserControl
    {
        private readonly PictureBox canvas;

        public event Action? ApplyRequested;
        public event Action? CancelRequested;

        // 텍스트 내용
        private string text = "여기에 텍스트 입력";

        // 텍스트 기본 설정
        private Font textFont =
            new Font("맑은 고딕", 24f, FontStyle.Regular);
        private static readonly PrivateFontCollection BundledFonts = new PrivateFontCollection();
        private static bool bundledFontsLoaded;
        private bool canvasEventsDetached;

        private Color textColor = Color.Black;
        private Color textBackgroundColor = Color.Transparent;
        private Color effectColor = Color.Black;
        private float effectIntensity = 0.55f;

        // 현재 효과
        private string currentEffect = "없음";

        // 텍스트의 시작 위치
        private PointF textPosition = new PointF(50, 50);

        // 드래그 상태
        private bool isDraggingText = false;
        private Point lastMousePoint;

        // 선택 상태
        private bool isTextSelected = false;
        private enum TextInteractionMode { None, Move, Resize, Rotate }
        private TextInteractionMode interactionMode;
        private float textRotationDegrees;
        private float dragStartFontSize;
        private float dragStartAngle;
        private TextBox? inlineEditor;
        private string? inlineOriginalText;
        private System.Windows.Forms.Timer? caretBlinkTimer;
        private bool showInlineCaret;
        private Rectangle lastTextInvalidationBounds = Rectangle.Empty;

        // UI 컨트롤
        private ComboBox fontComboBox;
        private NumericUpDown fontSizeUpDown;
        private ComboBox effectComboBox;
        private TrackBar textOpacityTrackBar;
        private Label textOpacityValueLabel;

        public TextToolPanel(PictureBox canvas)
        {
            InitializeComponent();

            this.canvas = canvas ??
                throw new ArgumentNullException(nameof(canvas));

            caretBlinkTimer = new System.Windows.Forms.Timer { Interval = 500 };
            caretBlinkTimer.Tick += CaretBlinkTimer_Tick;

            EnsureBundledFontsLoaded();
            InitializeUI();

            canvas.Paint += Canvas_Paint;
            canvas.MouseDown += Canvas_MouseDown;
            canvas.MouseMove += Canvas_MouseMove;
            canvas.MouseUp += Canvas_MouseUp;
            canvas.MouseDoubleClick += Canvas_MouseDoubleClick;
            canvas.Resize += Canvas_Resize;

            Disposed += (_, _) => DetachCanvasEvents();
        }

        private void InitializeUI()
        {
            Dock = DockStyle.Fill;

            FlowLayoutPanel mainLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(14, 16, 14, 10),
                BackColor = ToolOptionStyle.PanelBackground
            };

            mainLayout.Controls.Add(new Label
            {
                Text = "텍스트",
                AutoSize = true,
                Font = new Font("맑은 고딕", 15F, FontStyle.Bold),
                ForeColor = ToolOptionStyle.Title,
                Margin = new Padding(5, 0, 5, 2)
            });

            // 문구는 캔버스에서 직접 편집한다.
            mainLayout.Controls.Add(new Label
            {
                Text = "캔버스의 글자를 더블클릭하여 바로 입력하세요.",
                AutoSize = false,
                Height = 38,
                ForeColor = ToolOptionStyle.Description,
                Margin = new Padding(5, 2, 5, 10)
            });

            // 글자색
            mainLayout.Controls.Add(new Label
            {
                Text = "글자색 선택",
                AutoSize = true
            });

            FlowLayoutPanel colorPalettePanel =
                CreateColorPalettePanel(color =>
                {
                    textColor = Color.FromArgb(
                        textColor.A,
                        color.R,
                        color.G,
                        color.B);

                    UpdateOpacityControls();
                    RefreshText();
                });
            mainLayout.Controls.Add(colorPalettePanel);

            // 글꼴
            mainLayout.Controls.Add(new Label
            {
                Text = "글꼴 선택",
                AutoSize = true
            });

            fontComboBox = new ComboBox
            {
                Name = "cmbTextFont",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Margin = new Padding(5)
            };

            foreach (FontFamily font in FontFamily.Families)
                fontComboBox.Items.Add(font.Name);
            foreach (FontFamily font in BundledFonts.Families)
            {
                if (!fontComboBox.Items.Contains(font.Name))
                    fontComboBox.Items.Add(font.Name);
            }

            int defaultFontIndex =
                fontComboBox.Items.IndexOf(textFont.FontFamily.Name);

            if (defaultFontIndex >= 0)
                fontComboBox.SelectedIndex = defaultFontIndex;
            else if (fontComboBox.Items.Count > 0)
                fontComboBox.SelectedIndex = 0;

            fontComboBox.SelectedIndexChanged += (s, e) =>
            {
                if (fontComboBox.SelectedItem == null)
                    return;

                try
                {
                    ReplaceTextFont(CreateTextFont(
                        fontComboBox.SelectedItem.ToString() ?? "맑은 고딕",
                        textFont.Size,
                        textFont.Style));

                    RefreshText();
                }
                catch
                {
                    MessageBox.Show("이 글꼴은 사용할 수 없습니다.");
                }
            };

            mainLayout.Controls.Add(fontComboBox);

            // 글꼴 크기
            mainLayout.Controls.Add(new Label
            {
                Text = "글꼴 크기",
                AutoSize = true
            });

            fontSizeUpDown = new NumericUpDown
            {
                Name = "numTextFontSize",
                Minimum = 8,
                Maximum = 200,
                Value = (decimal)textFont.Size,
                Width = 80,
                Margin = new Padding(5)
            };

            fontSizeUpDown.ValueChanged += (s, e) =>
            {
                ReplaceTextFont(new Font(
                    textFont.FontFamily,
                    (float)fontSizeUpDown.Value,
                    textFont.Style));

                RefreshText();
            };

            mainLayout.Controls.Add(fontSizeUpDown);


            // 텍스트 투명도
            mainLayout.Controls.Add(new Label
            {
                Text = "텍스트 투명도",
                AutoSize = true,
                Margin = new Padding(5, 8, 5, 2)
            });

            FlowLayoutPanel opacityPanel = new FlowLayoutPanel
            {
                Width = 250,
                Height = 50,
                WrapContents = false,
                Margin = new Padding(0)
            };

            textOpacityTrackBar = new TrackBar
            {
                Name = "trkTextOpacity",
                Minimum = 0,
                Maximum = 100,
                Value = (int)Math.Round(textColor.A / 255f * 100f),
                TickFrequency = 10,
                SmallChange = 1,
                LargeChange = 10,
                Width = 180,
                Height = 40,
                Margin = new Padding(3)
            };

            textOpacityValueLabel = new Label
            {
                Text = $"{textOpacityTrackBar.Value}%",
                AutoSize = false,
                Width = 45,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(3, 8, 3, 3)
            };

            textOpacityTrackBar.ValueChanged += (s, e) =>
            {
                int alpha = (int)Math.Round(
                    textOpacityTrackBar.Value / 100f * 255f);

                textColor = Color.FromArgb(
                    alpha,
                    textColor.R,
                    textColor.G,
                    textColor.B);

                if (textOpacityValueLabel != null)
                    textOpacityValueLabel.Text =
                        $"{textOpacityTrackBar.Value}%";

                RefreshText();
            };

            opacityPanel.Controls.Add(textOpacityTrackBar);
            opacityPanel.Controls.Add(textOpacityValueLabel);
            mainLayout.Controls.Add(opacityPanel);

            // 글꼴 스타일
            mainLayout.Controls.Add(new Label
            {
                Text = "글꼴 스타일",
                AutoSize = true
            });

            FlowLayoutPanel stylePanel = new FlowLayoutPanel
            {
                Width = 220,
                Height = 40,
                WrapContents = false
            };

            Button btnBold = new Button
            {
                Name = "btnTextBold",
                Text = "B",
                Width = 40,
                Height = 30,
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold),
                Margin = new Padding(3)
            };

            Button btnItalic = new Button
            {
                Name = "btnTextItalic",
                Text = "I",
                Width = 40,
                Height = 30,
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Italic),
                Margin = new Padding(3)
            };

            Button btnUnderline = new Button
            {
                Name = "btnTextUnderline",
                Text = "U",
                Width = 40,
                Height = 30,
                Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Underline),
                Margin = new Padding(3)
            };

            btnBold.Click += (s, e) => ToggleFontStyle(FontStyle.Bold);
            btnItalic.Click += (s, e) => ToggleFontStyle(FontStyle.Italic);
            btnUnderline.Click += (s, e) => ToggleFontStyle(FontStyle.Underline);

            stylePanel.Controls.Add(btnBold);
            stylePanel.Controls.Add(btnItalic);
            stylePanel.Controls.Add(btnUnderline);
            mainLayout.Controls.Add(stylePanel);

            // 효과
            mainLayout.Controls.Add(new Label
            {
                Text = "효과 선택",
                AutoSize = true
            });

            effectComboBox = new ComboBox
            {
                Name = "cmbTextEffect",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 180,
                Margin = new Padding(5)
            };

            effectComboBox.Items.AddRange(new string[]
            {
                "없음",
                "그라데이션",
                "그림자",
                "윤곽선",
                "네온"
            });

            effectComboBox.SelectedIndex = 0;

            effectComboBox.SelectedIndexChanged += (s, e) =>
            {
                currentEffect =
                    effectComboBox.SelectedItem?.ToString() ?? "없음";

                RefreshText();
            };

            mainLayout.Controls.Add(effectComboBox);

            FlowLayoutPanel actionPanel = new FlowLayoutPanel
            {
                Width = 240,
                Height = 48,
                WrapContents = false,
                Margin = new Padding(5, 12, 5, 8)
            };

            Button applyButton = new Button
            {
                Text = "적용",
                Width = 108,
                Height = 36,
                BackColor = Color.FromArgb(70, 110, 230),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            applyButton.FlatAppearance.BorderSize = 0;
            applyButton.Click += (_, _) => ApplyRequested?.Invoke();

            Button cancelButton = new Button
            {
                Text = "취소",
                Width = 92,
                Height = 36,
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cancelButton.Click += (_, _) => CancelRequested?.Invoke();

            actionPanel.Controls.Add(cancelButton);
            actionPanel.Controls.Add(applyButton);
            mainLayout.Controls.Add(actionPanel);

            void UpdateResponsiveLayout()
            {
                int contentWidth = Math.Max(
                    180,
                    mainLayout.ClientSize.Width - mainLayout.Padding.Horizontal -
                    ((mainLayout.VerticalScroll.Visible ||
                      mainLayout.DisplayRectangle.Height > mainLayout.ClientSize.Height)
                        ? SystemInformation.VerticalScrollBarWidth
                        : 0) - 10);

                foreach (Control child in mainLayout.Controls)
                {
                    if (child is Label label && label.AutoSize)
                        continue;
                    child.Width = contentWidth;
                }

                fontComboBox.Width = contentWidth;
                effectComboBox.Width = contentWidth;
                colorPalettePanel.Width = contentWidth;
                foreach (Control row in colorPalettePanel.Controls)
                    row.Width = contentWidth;

                opacityPanel.Width = contentWidth;
                textOpacityTrackBar.Width = Math.Max(90, contentWidth - textOpacityValueLabel.Width - 18);
                stylePanel.Width = contentWidth;
                actionPanel.Width = contentWidth;

                int actionWidth = Math.Max(72, (contentWidth - 8) / 2);
                cancelButton.Width = actionWidth;
                applyButton.Width = actionWidth;
            }

            mainLayout.SizeChanged += (_, _) => UpdateResponsiveLayout();

            Controls.Add(mainLayout);
            UpdateResponsiveLayout();
        }

        public bool HasText => !string.IsNullOrWhiteSpace(text);

        /// <summary>
        /// AI 추천은 레이어를 바로 만들지 않고 현재 텍스트 도구의 임시 미리보기에만 반영합니다.
        /// 실제 레이어·Undo·타임라인 기록은 기존 적용 버튼 경로가 담당합니다.
        /// </summary>
        public void SetSuggestedText(
            string suggestedText,
            string placement,
            string effect,
            float anchorX = -1f,
            float anchorY = -1f,
            string alignment = "left",
            string suggestedTextColor = "black",
            float fontSizeRatio = 0.058f,
            string suggestedFontStyle = "clean",
            string suggestedEffectColor = "black",
            float suggestedEffectIntensity = 0.55f)
        {
            if (string.IsNullOrWhiteSpace(suggestedText))
                return;

            EndInlineTextEdit(false);
            text = suggestedText.Trim();
            ApplySuggestedFont(suggestedFontStyle);
            int canvasWidth = Math.Max(1, canvas.ClientSize.Width);
            int canvasHeight = Math.Max(1, canvas.ClientSize.Height);
            using (Graphics sizeGraphics = canvas.CreateGraphics())
            {
                float targetPixelHeight = Math.Min(canvasWidth, canvasHeight) *
                    Math.Max(0.03f, Math.Min(0.10f, fontSizeRatio));
                float targetPointSize = targetPixelHeight * 72f /
                    Math.Max(1f, sizeGraphics.DpiY);
                decimal clampedSize = ClampDecimal(
                    (decimal)targetPointSize,
                    fontSizeUpDown.Minimum,
                    fontSizeUpDown.Maximum);
                if (fontSizeUpDown.Value != clampedSize)
                    fontSizeUpDown.Value = clampedSize;
            }
            if (anchorX < 0f || anchorY < 0f)
                (anchorX, anchorY, alignment) = GetPlacementFallback(placement);

            anchorX = Math.Max(0f, Math.Min(1f, anchorX));
            anchorY = Math.Max(0f, Math.Min(1f, anchorY));
            using (Graphics graphics = canvas.CreateGraphics())
            {
                RectangleF measuredBounds = GetTextBounds(graphics);
                float proposedX = anchorX * canvasWidth;
                if (string.Equals(alignment, "center", StringComparison.OrdinalIgnoreCase))
                    proposedX -= measuredBounds.Width / 2f;
                else if (string.Equals(alignment, "right", StringComparison.OrdinalIgnoreCase))
                    proposedX -= measuredBounds.Width;

                float proposedY = anchorY * canvasHeight - measuredBounds.Height / 2f;
                const float margin = 8f;
                textPosition = new PointF(
                    Math.Max(margin, Math.Min(canvasWidth - measuredBounds.Width - margin, proposedX)),
                    Math.Max(margin, Math.Min(canvasHeight - measuredBounds.Height - margin, proposedY)));
            }

            Color recommendedColor = ParseSuggestedColor(
                suggestedTextColor,
                Color.Black);
            textColor = Color.FromArgb(
                textColor.A,
                recommendedColor.R,
                recommendedColor.G,
                recommendedColor.B);
            effectColor = ParseSuggestedColor(
                suggestedEffectColor,
                recommendedColor.GetBrightness() > 0.5f
                    ? Color.Black
                    : Color.White);
            effectIntensity = Math.Max(
                0f,
                Math.Min(1f, suggestedEffectIntensity));
            UpdateOpacityControls();

            currentEffect = effect?.ToLowerInvariant() switch
            {
                "gradient" => "그라데이션",
                "outline" => "윤곽선",
                "neon" => "네온",
                "shadow" => "그림자",
                _ => "없음"
            };
            effectComboBox.SelectedItem = currentEffect;
            isTextSelected = true;
            RefreshText();
        }

        private static Color ParseSuggestedColor(string value, Color fallback)
        {
            if (string.Equals(value, "white", StringComparison.OrdinalIgnoreCase))
                return Color.White;
            if (string.Equals(value, "black", StringComparison.OrdinalIgnoreCase))
                return Color.Black;

            if (!string.IsNullOrWhiteSpace(value) &&
                value.Length == 7 &&
                value[0] == '#' &&
                int.TryParse(
                    value.Substring(1),
                    System.Globalization.NumberStyles.HexNumber,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out int rgb))
            {
                return Color.FromArgb(
                    (rgb >> 16) & 0xFF,
                    (rgb >> 8) & 0xFF,
                    rgb & 0xFF);
            }

            return fallback;
        }

        private void ApplySuggestedFont(string suggestedFontStyle)
        {
            string[] candidates = suggestedFontStyle switch
            {
                "playful" => new[]
                {
                    "Jua", "배달의민족 주아", "여기어때 잘난체", "휴먼둥근헤드라인", "맑은 고딕"
                },
                "emotional" => new[]
                {
                    "나눔명조", "Noto Serif KR", "바탕", "맑은 고딕"
                },
                "bold" => new[]
                {
                    "Pretendard", "Noto Sans KR", "맑은 고딕"
                },
                _ => new[]
                {
                    "Pretendard", "Noto Sans KR", "맑은 고딕"
                }
            };

            string selectedFamily = textFont.FontFamily.Name;
            foreach (string candidate in candidates)
            {
                if (fontComboBox.Items.Contains(candidate))
                {
                    selectedFamily = candidate;
                    break;
                }
            }

            fontComboBox.SelectedItem = selectedFamily;
            FontStyle style = suggestedFontStyle is "playful" or "bold"
                ? FontStyle.Bold
                : FontStyle.Regular;
            try
            {
                ReplaceTextFont(CreateTextFont(selectedFamily, textFont.Size, style));
            }
            catch (ArgumentException)
            {
                ReplaceTextFont(new Font("맑은 고딕", textFont.Size, style));
                fontComboBox.SelectedItem = "맑은 고딕";
            }
        }

        private static void EnsureBundledFontsLoaded()
        {
            if (bundledFontsLoaded)
                return;

            bundledFontsLoaded = true;
            string fontRoot = Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "Fonts");
            if (!Directory.Exists(fontRoot))
                return;

            foreach (string fontPath in Directory.EnumerateFiles(
                         fontRoot,
                         "*.ttf",
                         SearchOption.AllDirectories))
            {
                try
                {
                    BundledFonts.AddFontFile(fontPath);
                }
                catch (ArgumentException)
                {
                    // 손상되었거나 GDI+가 지원하지 않는 글꼴만 건너뛴다.
                }
            }
        }

        private static Font CreateTextFont(
            string familyName,
            float size,
            FontStyle style)
        {
            FontFamily? bundledFamily = BundledFonts.Families.FirstOrDefault(
                family => string.Equals(
                    family.Name,
                    familyName,
                    StringComparison.OrdinalIgnoreCase));
            return bundledFamily != null
                ? new Font(bundledFamily, size, style)
                : new Font(familyName, size, style);
        }

        private void ReplaceTextFont(Font replacement)
        {
            Font previous = textFont;
            textFont = replacement;
            previous.Dispose();
        }

        private static (float X, float Y, string Alignment) GetPlacementFallback(
            string placement)
        {
            return placement switch
            {
                "top-center" => (0.5f, 0.18f, "center"),
                "top-right" => (0.94f, 0.18f, "right"),
                "middle-left" => (0.06f, 0.5f, "left"),
                "center" => (0.5f, 0.5f, "center"),
                "middle-right" => (0.94f, 0.5f, "right"),
                "bottom-center" => (0.5f, 0.82f, "center"),
                "bottom-right" => (0.94f, 0.82f, "right"),
                "top-left" => (0.06f, 0.18f, "left"),
                _ => (0.06f, 0.82f, "left")
            };
        }

        public Bitmap CreateTextLayer(Size imageSize)
        {
            if (imageSize.Width <= 0 || imageSize.Height <= 0)
                throw new ArgumentOutOfRangeException(nameof(imageSize));

            Bitmap result = new Bitmap(
                imageSize.Width,
                imageSize.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using Graphics graphics = Graphics.FromImage(result);
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            float canvasWidth = Math.Max(1, canvas.ClientSize.Width);
            float canvasHeight = Math.Max(1, canvas.ClientSize.Height);
            graphics.ScaleTransform(
                imageSize.Width / canvasWidth,
                imageSize.Height / canvasHeight);

            using GraphicsPath path = CreateMultilineTextPath(graphics);
            ApplyRotation(path);
            RectangleF bounds = path.GetBounds();
            DrawTextBackground(graphics, bounds);
            DrawTextEffect(graphics, path, bounds);

            return result;
        }

        private void DetachCanvasEvents()
        {
            if (canvasEventsDetached)
                return;

            canvasEventsDetached = true;
            canvas.Paint -= Canvas_Paint;
            canvas.MouseDown -= Canvas_MouseDown;
            canvas.MouseMove -= Canvas_MouseMove;
            canvas.MouseUp -= Canvas_MouseUp;
            canvas.MouseDoubleClick -= Canvas_MouseDoubleClick;
            canvas.Resize -= Canvas_Resize;
            caretBlinkTimer?.Stop();
            caretBlinkTimer?.Dispose();
            caretBlinkTimer = null;
            EndInlineTextEdit(false);
            textFont.Dispose();
            canvas.Cursor = Cursors.Default;
        }

        private void ToggleFontStyle(FontStyle style)
        {
            FontStyle newStyle;

            if ((textFont.Style & style) == style)
                newStyle = textFont.Style & ~style;
            else
                newStyle = textFont.Style | style;

            ReplaceTextFont(new Font(
                textFont.FontFamily,
                textFont.Size,
                newStyle));

            RefreshText();
        }

        public void RefreshText()
        {
            if (canvas.IsDisposed || !canvas.IsHandleCreated)
                return;

            Rectangle currentBounds = GetTextInvalidationBounds();
            Rectangle dirtyBounds = lastTextInvalidationBounds.IsEmpty
                ? currentBounds
                : Rectangle.Union(lastTextInvalidationBounds, currentBounds);

            canvas.Invalidate(dirtyBounds);
            lastTextInvalidationBounds = currentBounds;
        }

        private Rectangle GetTextInvalidationBounds()
        {
            using Graphics graphics = canvas.CreateGraphics();
            RectangleF bounds = string.IsNullOrEmpty(text)
                ? new RectangleF(textPosition.X, textPosition.Y, 2f, textFont.GetHeight(graphics))
                : GetTextBounds(graphics);

            // 선택 핸들, 회전 핸들, 그림자·네온 효과까지 함께 갱신한다.
            bounds.Inflate(48f, 52f);
            return Rectangle.Ceiling(bounds);
        }

        private void CaretBlinkTimer_Tick(object? sender, EventArgs e)
        {
            if (inlineEditor == null)
            {
                caretBlinkTimer?.Stop();
                return;
            }

            showInlineCaret = !showInlineCaret;
            RefreshText();
        }

        private void Canvas_Resize(object sender, EventArgs e)
        {
            RefreshText();
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            if (string.IsNullOrEmpty(text))
                return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            using (GraphicsPath path = CreateMultilineTextPath(g))
            {
                ApplyRotation(path);
                RectangleF bounds = path.GetBounds();

                DrawTextBackground(g, bounds);
                DrawTextEffect(g, path, bounds);
            }

            if (isTextSelected)
            {
                RectangleF bounds = GetTextBounds(g);
                DrawSelectionHandles(g, bounds);
                if (inlineEditor != null)
                    DrawInlineCaret(g, bounds);
            }
        }

        // 핵심: 줄바꿈을 기준으로 각각의 줄을 같은 GraphicsPath에 추가한다.
        // 따라서 그림자/윤곽선/네온/그라데이션이 여러 줄 전체에 적용된다.
        private GraphicsPath CreateMultilineTextPath(Graphics g)
        {
            GraphicsPath path = new GraphicsPath();

            float emSize = Math.Max(
                8f,
                textFont.Size * (g.DpiY / 72f));

            string[] lines = text.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.None);

            float lineHeight = textFont.GetHeight(g);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // 빈 줄은 내용은 없지만 줄 간격은 유지된다.
                if (line.Length == 0)
                    continue;

                PointF linePosition = new PointF(
                    textPosition.X,
                    textPosition.Y + i * lineHeight);

                path.AddString(
                    line,
                    textFont.FontFamily,
                    (int)textFont.Style,
                    emSize,
                    linePosition,
                    StringFormat.GenericDefault);
            }

            return path;
        }

        private RectangleF GetTextBounds(Graphics g)
        {
            using (GraphicsPath path = CreateMultilineTextPath(g))
            {
                ApplyRotation(path);
                RectangleF bounds = path.GetBounds();
                bounds.Inflate(2, 2);
                return bounds;
            }
        }

        private void DrawTextBackground(Graphics g, RectangleF bounds)
        {
            if (textBackgroundColor == Color.Transparent)
                return;

            RectangleF background = bounds;
            background.Inflate(5, 3);

            using (SolidBrush brush = new SolidBrush(textBackgroundColor))
            {
                g.FillRectangle(brush, background);
            }
        }

        private void DrawTextEffect(
            Graphics g,
            GraphicsPath path,
            RectangleF bounds)
        {
            switch (currentEffect)
            {
                case "그라데이션":
                    DrawGradient(g, path, bounds);
                    break;

                case "그림자":
                    DrawShadow(g, path);
                    DrawNormalText(g, path);
                    break;

                case "윤곽선":
                    DrawOutline(g, path);
                    DrawNormalText(g, path);
                    break;

                case "네온":
                    DrawNeon(g, path);
                    break;

                default:
                    DrawNormalText(g, path);
                    break;
            }
        }

        private void DrawNormalText(Graphics g, GraphicsPath path)
        {
            using (SolidBrush brush = new SolidBrush(textColor))
            {
                g.FillPath(brush, path);
            }
        }

        // 여러 줄 전체를 하나의 bounds로 사용해서 연속된 그라데이션을 만든다.
        private void DrawGradient(
            Graphics g,
            GraphicsPath path,
            RectangleF bounds)
        {
            Color secondColor = GetGradientColor(textColor);

            using (LinearGradientBrush brush = new LinearGradientBrush(
                bounds,
                textColor,
                secondColor,
                LinearGradientMode.Horizontal))
            {
                g.FillPath(brush, path);
            }
        }

        private void DrawShadow(Graphics g, GraphicsPath path)
        {
            float fontPixels = textFont.Size * g.DpiY / 72f;
            float offset = Math.Max(
                1.2f,
                Math.Min(5f, fontPixels * (0.025f + effectIntensity * 0.055f)));
            int alpha = (int)Math.Round(45f + effectIntensity * 90f);
            using (GraphicsPath shadow = (GraphicsPath)path.Clone())
            using (Matrix matrix = new Matrix())
            {
                matrix.Translate(offset, offset);
                shadow.Transform(matrix);

                using (SolidBrush brush = new SolidBrush(
                    Color.FromArgb(alpha, effectColor)))
                {
                    g.FillPath(brush, shadow);
                }
            }
        }

        private void DrawOutline(Graphics g, GraphicsPath path)
        {
            float fontPixels = textFont.Size * g.DpiY / 72f;
            float width = Math.Max(
                1f,
                Math.Min(5f, fontPixels * (0.025f + effectIntensity * 0.05f)));
            using (Pen outline = new Pen(effectColor, width))
            {
                outline.LineJoin = LineJoin.Round;
                g.DrawPath(outline, path);
            }
        }

        private void DrawNeon(Graphics g, GraphicsPath path)
        {
            // 여러 겹의 glow를 그려 네온 번짐을 표현한다.
            for (int size = 14; size >= 2; size -= 2)
            {
                int alpha = Math.Max(10, 75 - size * 4);

                using (Pen glow = new Pen(
                    Color.FromArgb(alpha, textColor),
                    size))
                {
                    glow.LineJoin = LineJoin.Round;
                    g.DrawPath(glow, path);
                }
            }

            using (SolidBrush brush = new SolidBrush(
                Color.FromArgb(textColor.A, Color.White)))
            {
                g.FillPath(brush, path);
            }
        }

        private Color GetGradientColor(Color color)
        {
            return Color.FromArgb(
                color.A,
                Math.Min(255, color.R + 100),
                Math.Min(255, color.G + 100),
                Math.Min(255, color.B + 100));
        }

        private bool IsPointInsideText(Point point)
        {
            // 화면의 실제 DPI와 동일한 Graphics로 범위를 계산해야, 보이는 글자를
            // 더블클릭했을 때 편집 모드가 정확히 열린다.
            using Graphics graphics = canvas.CreateGraphics();
            RectangleF bounds = GetTextBounds(graphics);
            bounds.Inflate(12, 12);
            return bounds.Contains(point);
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            if (inlineEditor != null)
                return;

            using Graphics graphics = canvas.CreateGraphics();
            RectangleF bounds = GetTextBounds(graphics);
            if (HitTestRotationHandle(e.Location, bounds))
            {
                isTextSelected = true;
                interactionMode = TextInteractionMode.Rotate;
                dragStartAngle = GetAngle(bounds, e.Location);
                canvas.Cursor = Cursors.Hand;
                return;
            }

            if (HitTestResizeHandle(e.Location, bounds))
            {
                isTextSelected = true;
                interactionMode = TextInteractionMode.Resize;
                dragStartFontSize = textFont.Size;
                lastMousePoint = e.Location;
                canvas.Cursor = Cursors.SizeNWSE;
                return;
            }

            if (bounds.Contains(e.Location))
            {
                isTextSelected = true;
                isDraggingText = true;
                interactionMode = TextInteractionMode.Move;
                lastMousePoint = e.Location;
                canvas.Cursor = Cursors.SizeAll;
                RefreshText();
            }
            else
            {
                isTextSelected = false;
                isDraggingText = false;
                interactionMode = TextInteractionMode.None;
                canvas.Cursor = Cursors.Default;
                RefreshText();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (interactionMode == TextInteractionMode.None)
                return;

            int dx = e.X - lastMousePoint.X;
            int dy = e.Y - lastMousePoint.Y;

            if (interactionMode == TextInteractionMode.Move)
            {
                textPosition.X += dx;
                textPosition.Y += dy;
            }
            else if (interactionMode == TextInteractionMode.Resize)
            {
                float newSize = Math.Max(8f, Math.Min(200f, dragStartFontSize + Math.Max(dx, dy) * 0.45f));
                if (Math.Abs(newSize - textFont.Size) > 0.1f)
                {
                    ReplaceTextFont(new Font(textFont.FontFamily, newSize, textFont.Style));
                    fontSizeUpDown.Value = ClampDecimal((decimal)newSize, fontSizeUpDown.Minimum, fontSizeUpDown.Maximum);
                }
            }
            else if (interactionMode == TextInteractionMode.Rotate)
            {
                using Graphics graphics = canvas.CreateGraphics();
                RectangleF bounds = GetTextBounds(graphics);
                float currentAngle = GetAngle(bounds, e.Location);
                textRotationDegrees += NormalizeAngle(currentAngle - dragStartAngle);
                dragStartAngle = currentAngle;
            }

            lastMousePoint = e.Location;
            RefreshText();
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            isDraggingText = false;
            interactionMode = TextInteractionMode.None;
            canvas.Cursor = Cursors.Default;
        }

        private void Canvas_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || inlineEditor != null || !IsPointInsideText(e.Location))
                return;

            isTextSelected = true;
            // WinForms TextBox는 사진 위의 실제 투명 배경을 지원하지 않는다. 아주 작은
            // 입력 컨트롤은 캔버스 안에서 포커스와 한글 IME 입력만 담당하고, 화면 표시는
            // Canvas_Paint가 직접 담당한다. 따라서 입력은 유지하면서 흰색 배경은 보이지 않는다.
            inlineEditor = new TextBox
            {
                Location = new Point(0, 0),
                Size = new Size(1, 1),
                Multiline = true,
                BorderStyle = BorderStyle.None,
                BackColor = canvas.BackColor == Color.Transparent ? Color.White : canvas.BackColor,
                ForeColor = canvas.BackColor == Color.Transparent ? Color.White : canvas.BackColor,
                Font = new Font(textFont.FontFamily, Math.Max(8f, textFont.Size), textFont.Style),
                Text = text
            };
            inlineOriginalText = text;
            inlineEditor.TextChanged += (_, _) =>
            {
                text = inlineEditor.Text;
                RefreshText();
            };
            inlineEditor.KeyDown += InlineEditor_KeyDown;
            inlineEditor.KeyUp += (_, _) => RefreshText();
            inlineEditor.LostFocus += (_, _) => EndInlineTextEdit(true);
            canvas.Controls.Add(inlineEditor);
            inlineEditor.BringToFront();
            inlineEditor.Focus();
            using Graphics graphics = canvas.CreateGraphics();
            inlineEditor.SelectionStart = GetInsertionIndex(graphics, e.Location);
            inlineEditor.SelectionLength = 0;
            showInlineCaret = true;
            caretBlinkTimer?.Start();
            RefreshText();
        }

        private void InlineEditor_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                EndInlineTextEdit(false);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter && e.Control)
            {
                EndInlineTextEdit(true);
                e.SuppressKeyPress = true;
            }
        }

        private void EndInlineTextEdit(bool keepText)
        {
            if (inlineEditor == null) return;
            TextBox editor = inlineEditor;
            inlineEditor = null;
            caretBlinkTimer?.Stop();
            showInlineCaret = false;
            text = keepText ? editor.Text : inlineOriginalText ?? text;
            inlineOriginalText = null;
            editor.Dispose();
            RefreshText();
        }

        private void ApplyRotation(GraphicsPath path)
        {
            if (Math.Abs(textRotationDegrees) < 0.01f || path.PointCount == 0) return;
            RectangleF bounds = path.GetBounds();
            using Matrix matrix = new Matrix();
            matrix.RotateAt(textRotationDegrees, new PointF(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f));
            path.Transform(matrix);
        }

        private void DrawInlineCaret(Graphics graphics, RectangleF bounds)
        {
            // 입력 중임을 알리는 최소한의 커서만 캔버스에 표시한다.
            if (!showInlineCaret)
                return;

            using Pen caretPen = new Pen(Color.FromArgb(30, 100, 220), 2f);
            PointF caret = GetCaretLocation(graphics, inlineEditor?.SelectionStart ?? text.Length);
            float height = Math.Max(16f, textFont.GetHeight(graphics));
            graphics.DrawLine(caretPen, caret.X, caret.Y, caret.X, caret.Y + height);
        }

        private int GetInsertionIndex(Graphics graphics, Point point)
        {
            if (Math.Abs(textRotationDegrees) > 0.01f)
                return text.Length;

            string[] lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            float lineHeight = textFont.GetHeight(graphics);
            int lineIndex = Math.Max(0, Math.Min(lines.Length - 1, (int)((point.Y - textPosition.Y) / lineHeight)));
            string line = lines[lineIndex];
            float localX = point.X - textPosition.X;
            int column = 0;
            float previousWidth = 0f;

            for (int i = 1; i <= line.Length; i++)
            {
                float width = graphics.MeasureString(line.Substring(0, i), textFont).Width;
                if (localX < (previousWidth + width) / 2f)
                    break;

                column = i;
                previousWidth = width;
            }

            int index = 0;
            for (int i = 0; i < lineIndex; i++)
            {
                index += lines[i].Length;
                if (index < text.Length && text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                    index += 2;
                else if (index < text.Length)
                    index++;
            }

            return Math.Min(text.Length, index + column);
        }

        private PointF GetCaretLocation(Graphics graphics, int index)
        {
            if (Math.Abs(textRotationDegrees) > 0.01f)
                return textPosition;

            index = Math.Max(0, Math.Min(text.Length, index));
            string beforeCaret = text.Substring(0, index);
            string[] lines = beforeCaret.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            string currentLine = lines[lines.Length - 1];
            float x = textPosition.X + graphics.MeasureString(currentLine, textFont).Width;
            float y = textPosition.Y + (lines.Length - 1) * textFont.GetHeight(graphics);
            return new PointF(x, y);
        }

        private void DrawSelectionHandles(Graphics graphics, RectangleF bounds)
        {
            Rectangle selection = Rectangle.Round(bounds);
            selection.Inflate(5, 5);
            using Pen pen = new Pen(Color.FromArgb(78, 121, 222), 1f);
            using SolidBrush brush = new SolidBrush(Color.FromArgb(96, 132, 229));
            graphics.DrawRectangle(pen, selection);
            Point[] points =
            {
                new Point(selection.Left, selection.Top), new Point(selection.Left + selection.Width / 2, selection.Top), new Point(selection.Right, selection.Top),
                new Point(selection.Left, selection.Top + selection.Height / 2), new Point(selection.Right, selection.Top + selection.Height / 2),
                new Point(selection.Left, selection.Bottom), new Point(selection.Left + selection.Width / 2, selection.Bottom), new Point(selection.Right, selection.Bottom)
            };
            foreach (Point point in points) graphics.FillRectangle(brush, point.X - 4, point.Y - 4, 8, 8);
            Point centerTop = new Point(selection.Left + selection.Width / 2, selection.Top);
            Point rotate = new Point(centerTop.X, centerTop.Y - 26);
            graphics.DrawLine(pen, centerTop, rotate);
            graphics.FillEllipse(brush, rotate.X - 5, rotate.Y - 5, 10, 10);
        }

        private static bool HitTestResizeHandle(Point point, RectangleF bounds)
        {
            Rectangle selection = Rectangle.Round(bounds);
            selection.Inflate(8, 8);
            Point[] handles =
            {
                new Point(selection.Left, selection.Top),
                new Point(selection.Left + selection.Width / 2, selection.Top),
                new Point(selection.Right, selection.Top),
                new Point(selection.Left, selection.Top + selection.Height / 2),
                new Point(selection.Right, selection.Top + selection.Height / 2),
                new Point(selection.Left, selection.Bottom),
                new Point(selection.Left + selection.Width / 2, selection.Bottom),
                new Point(selection.Right, selection.Bottom)
            };
            return Array.Exists(handles, handle => Math.Abs(handle.X - point.X) <= 9 && Math.Abs(handle.Y - point.Y) <= 9);
        }

        private static bool HitTestRotationHandle(Point point, RectangleF bounds)
        {
            Rectangle selection = Rectangle.Round(bounds);
            selection.Inflate(5, 5);
            Point handle = new Point(selection.Left + selection.Width / 2, selection.Top - 26);
            return Math.Abs(handle.X - point.X) <= 10 && Math.Abs(handle.Y - point.Y) <= 10;
        }

        private static float GetAngle(RectangleF bounds, Point point)
        {
            PointF center = new PointF(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
            return (float)(Math.Atan2(point.Y - center.Y, point.X - center.X) * 180d / Math.PI);
        }

        private static float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        private decimal ClampDecimal(
            decimal value,
            decimal minimum,
            decimal maximum)
        {
            if (value < minimum)
                return minimum;

            if (value > maximum)
                return maximum;

            return value;
        }

        private void UpdateOpacityControls()
        {
            if (textOpacityTrackBar == null)
                return;

            int opacity = (int)Math.Round(
                textColor.A / 255f * 100f);

            opacity = Math.Max(
                textOpacityTrackBar.Minimum,
                Math.Min(textOpacityTrackBar.Maximum, opacity));

            textOpacityTrackBar.Value = opacity;

            if (textOpacityValueLabel != null)
                textOpacityValueLabel.Text = $"{opacity}%";
        }

        private FlowLayoutPanel CreateColorPalettePanel(Action<Color> onColorSelected)
        {
            const int panelWidth = 220;
            const int btnSize = 17;
            const int btnMargin = 2;

            FlowLayoutPanel parent = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                Width = panelWidth,
                Height = btnSize + btnMargin * 2 + 36,
                Margin = new Padding(5)
            };

            Color[] colors =
            {
                Color.Red,
                Color.Orange,
                Color.Yellow,
                Color.Green,
                Color.Blue,
                Color.Indigo,
                Color.Violet,
                Color.Black,
                Color.White
            };

            FlowLayoutPanel colorRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Width = panelWidth,
                Height = btnSize + btnMargin * 2,
                AutoScroll = true,
                Margin = new Padding(0)
            };

            foreach (Color color in colors)
            {
                Button button = new Button
                {
                    BackColor = color,
                    Width = btnSize,
                    Height = btnSize,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(btnMargin)
                };

                button.FlatAppearance.BorderSize = 1;
                button.Click += (s, e) => onColorSelected(color);
                colorRow.Controls.Add(button);
            }

            FlowLayoutPanel moreRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Width = panelWidth,
                Height = 32,
                Margin = new Padding(0)
            };

            Button moreButton = new Button
            {
                Text = "더보기...",
                Width = 80,
                Height = 28,
                Margin = new Padding(4)
            };

            moreButton.Click += (s, e) =>
            {
                using (ColorDialog dialog = new ColorDialog
                {
                    FullOpen = true
                })
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        onColorSelected(dialog.Color);
                    }
                }
            };

            moreRow.Controls.Add(moreButton);
            parent.Controls.Add(colorRow);
            parent.Controls.Add(moreRow);

            return parent;
        }
    }
}
