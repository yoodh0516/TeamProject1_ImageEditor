namespace ImageEditor
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        //test1

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pnlHeader = new Panel();
            menuStrip1 = new MenuStrip();
            toolStripMenuItem1 = new ToolStripMenuItem();
            menuOpen = new ToolStripMenuItem();
            menuSave = new ToolStripMenuItem();
            menuSaveAs = new ToolStripMenuItem();
            menuExport = new ToolStripMenuItem();
            menuExportJpeg = new ToolStripMenuItem();
            menuJpeg100 = new ToolStripMenuItem();
            menuJpeg85 = new ToolStripMenuItem();
            menuJpeg60 = new ToolStripMenuItem();
            menuExportPng = new ToolStripMenuItem();
            menuExportSns = new ToolStripMenuItem();
            menuInstagramSquareExport = new ToolStripMenuItem();
            menuInstagramPortraitExport = new ToolStripMenuItem();
            menuVerticalExport = new ToolStripMenuItem();
            menuYoutubeThumbnailExport = new ToolStripMenuItem();
            menuWebThumbnailExport = new ToolStripMenuItem();
            menuExit = new ToolStripMenuItem();
            toolStripMenuItem2 = new ToolStripMenuItem();
            menuUndo = new ToolStripMenuItem();
            menuRedo = new ToolStripMenuItem();
            menuResetOriginal = new ToolStripMenuItem();
            toolStripMenuItem3 = new ToolStripMenuItem();
            menuResize = new ToolStripMenuItem();
            menuCrop = new ToolStripMenuItem();
            menuRotateLeft = new ToolStripMenuItem();
            menuRotateRight = new ToolStripMenuItem();
            menuFlipHorizontal = new ToolStripMenuItem();
            menuFlipVertical = new ToolStripMenuItem();
            menuSnsRatio = new ToolStripMenuItem();
            toolStripMenuItem4 = new ToolStripMenuItem();
            menuBrightness = new ToolStripMenuItem();
            toolStripMenuItem5 = new ToolStripMenuItem();
            menuBasicFilter = new ToolStripMenuItem();
            menuTemplate = new ToolStripMenuItem();
            menuCommunityFilter = new ToolStripMenuItem();
            menuMyStyle = new ToolStripMenuItem();
            toolStripMenuItem6 = new ToolStripMenuItem();
            menuAddText = new ToolStripMenuItem();
            menuEditText = new ToolStripMenuItem();
            toolStripMenuItem7 = new ToolStripMenuItem();
            menuDrawingTool = new ToolStripMenuItem();
            menuMask = new ToolStripMenuItem();
            menuAIRemoveObject = new ToolStripMenuItem();
            toolStripMenuItem23 = new ToolStripMenuItem();
            menuAICaption = new ToolStripMenuItem();
            menuAITextPosition = new ToolStripMenuItem();
            menuAIMusicRecommend = new ToolStripMenuItem();
            toolStripMenuItem9 = new ToolStripMenuItem();
            menuZoom = new ToolStripMenuItem();
            menuZoomIn = new ToolStripMenuItem();
            menuZoomOut = new ToolStripMenuItem();
            menuFitToScreen = new ToolStripMenuItem();
            menuBeforeAfter = new ToolStripMenuItem();
            menuPreview = new ToolStripMenuItem();
            menuContrast = new ToolStripMenuItem();
            menuSaturation = new ToolStripMenuItem();
            menuTemperature = new ToolStripMenuItem();
            pnlLeft = new Panel();
            tabControl1 = new TabControl();
            tabTimeLine = new TabPage();
            tabLayer = new TabPage();
            pnlCanvas = new Panel();
            picCanvas = new PictureBox();
            pnlToolOptions = new Panel();
            pnlBottom = new Panel();
            label1 = new Label();
            pnlHeader.SuspendLayout();
            menuStrip1.SuspendLayout();
            pnlLeft.SuspendLayout();
            tabControl1.SuspendLayout();
            pnlCanvas.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picCanvas).BeginInit();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.Controls.Add(menuStrip1);
            pnlHeader.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlHeader.Location = new Point(1, 1);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1277, 30);
            pnlHeader.TabIndex = 0;
            // 
            // menuStrip1
            // 
            menuStrip1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            menuStrip1.Items.AddRange(new ToolStripItem[] { toolStripMenuItem1, toolStripMenuItem2, toolStripMenuItem3, toolStripMenuItem4, toolStripMenuItem5, toolStripMenuItem6, toolStripMenuItem7, menuAIRemoveObject, toolStripMenuItem9 });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.ShowItemToolTips = true;
            menuStrip1.Size = new Size(1277, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.DropDownItems.AddRange(new ToolStripItem[] { menuOpen, menuSave, menuSaveAs, menuExport, menuExit });
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new Size(43, 20);
            toolStripMenuItem1.Text = "파일";
            // 
            // menuOpen
            // 
            menuOpen.Name = "menuOpen";
            menuOpen.Size = new Size(180, 22);
            menuOpen.Text = "이미지 열기";
            menuOpen.ToolTipText = "편집할 이미지를 불러옵니다.";
            menuOpen.Click += menuOpen_Click;
            // 
            // menuSave
            // 
            menuSave.Name = "menuSave";
            menuSave.Size = new Size(180, 22);
            menuSave.Text = "저장";
            menuSave.ToolTipText = "현재 편집 중인 이미지를 저장합니다.";
            menuSave.Click += menuSave_Click;
            // 
            // menuSaveAs
            // 
            menuSaveAs.Name = "menuSaveAs";
            menuSaveAs.Size = new Size(180, 22);
            menuSaveAs.Text = "다른 이름으로 저장";
            menuSaveAs.ToolTipText = "파일 이름과 저장 위치를 지정하여 저장합니다.";
            menuSaveAs.Click += menuSaveAs_Click;
            // 
            // menuExport
            // 
            menuExport.DropDownItems.AddRange(new ToolStripItem[] { menuInstagramSquareExport, menuInstagramPortraitExport, menuVerticalExport, menuYoutubeThumbnailExport, menuWebThumbnailExport });
            menuExport.Name = "menuExport";
            menuExport.Size = new Size(180, 22);
            menuExport.Text = "SNS용 내보내기";
            // 
            // menuExportJpeg
            // 
            menuExportJpeg.DropDownItems.AddRange(new ToolStripItem[] { menuJpeg100, menuJpeg85, menuJpeg60 });
            menuExportJpeg.Name = "menuExportJpeg";
            menuExportJpeg.Size = new Size(180, 22);
            menuExportJpeg.Text = "JPG";
            menuExportJpeg.ToolTipText = "JPEG 형식과 지정된 품질로 이미지를 저장합니다.";
            // 
            // menuJpeg100
            // 
            menuJpeg100.Name = "menuJpeg100";
            menuJpeg100.Size = new Size(180, 22);
            menuJpeg100.Text = "최고 품질 (100%)";
            menuJpeg100.Click += menuJpeg100_Click;
            // 
            // menuJpeg85
            // 
            menuJpeg85.Name = "menuJpeg85";
            menuJpeg85.Size = new Size(180, 22);
            menuJpeg85.Text = "높은 품질 (85%)";
            menuJpeg85.Click += menuJpeg85_Click;
            // 
            // menuJpeg60
            // 
            menuJpeg60.Name = "menuJpeg60";
            menuJpeg60.Size = new Size(180, 22);
            menuJpeg60.Text = "용량 절약 (60%)";
            menuJpeg60.Click += menuJpeg60_Click;
            // 
            // menuExportPng
            // 
            menuExportPng.Name = "menuExportPng";
            menuExportPng.Size = new Size(180, 22);
            menuExportPng.Text = "PNG";
            menuExportPng.ToolTipText = "화질 손실 없이 PNG 형식으로 저장합니다.";
            menuExportPng.Click += menuExportPng_Click;
            // 
            // menuExportSns
            // 
            menuExportSns.Name = "menuExportSns";
            menuExportSns.Size = new Size(180, 22);
            menuExportSns.Text = "SNS 최적화";
            menuExportSns.ToolTipText = "선택한 SNS 또는 웹 규격에 맞춰 이미지를 저장합니다.";
            // 
            // menuInstagramSquareExport
            // 
            menuInstagramSquareExport.Name = "menuInstagramSquareExport";
            menuInstagramSquareExport.Size = new Size(195, 22);
            menuInstagramSquareExport.Text = "Instagram 정사각형";
            menuInstagramSquareExport.Click += menuInstagramSquareExport_Click;
            // 
            // menuInstagramPortraitExport
            // 
            menuInstagramPortraitExport.Name = "menuInstagramPortraitExport";
            menuInstagramPortraitExport.Size = new Size(195, 22);
            menuInstagramPortraitExport.Text = "Instagram 세로형";
            menuInstagramPortraitExport.Click += menuInstagramPortraitExport_Click;
            // 
            // menuVerticalExport
            // 
            menuVerticalExport.Name = "menuVerticalExport";
            menuVerticalExport.Size = new Size(195, 22);
            menuVerticalExport.Text = "세로 스토리/TikTok";
            menuVerticalExport.Click += menuVerticalExport_Click;
            // 
            // menuYoutubeThumbnailExport
            // 
            menuYoutubeThumbnailExport.Name = "menuYoutubeThumbnailExport";
            menuYoutubeThumbnailExport.Size = new Size(195, 22);
            menuYoutubeThumbnailExport.Text = "YouTube 썸네일";
            menuYoutubeThumbnailExport.Click += menuYoutubeThumbnailExport_Click;
            // 
            // menuWebThumbnailExport
            // 
            menuWebThumbnailExport.Name = "menuWebThumbnailExport";
            menuWebThumbnailExport.Size = new Size(195, 22);
            menuWebThumbnailExport.Text = "웹/블로그 대표 이미지";
            menuWebThumbnailExport.Click += menuWebThumbnailExport_Click;
            // 
            // menuExit
            // 
            menuExit.Name = "menuExit";
            menuExit.Size = new Size(180, 22);
            menuExit.Text = "종료";
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.DropDownItems.AddRange(new ToolStripItem[] { menuUndo, menuRedo, menuResetOriginal });
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new Size(43, 20);
            toolStripMenuItem2.Text = "편집";
            // 
            // menuUndo
            // 
            menuUndo.Name = "menuUndo";
            menuUndo.Size = new Size(180, 22);
            menuUndo.Text = "실행 취소";
            menuUndo.ToolTipText = "바로 이전 편집 작업으로 되돌립니다.";
            // 
            // menuRedo
            // 
            menuRedo.Name = "menuRedo";
            menuRedo.Size = new Size(180, 22);
            menuRedo.Text = "다시 실행";
            menuRedo.ToolTipText = "취소했던 편집 작업을 다시 적용합니다.";
            // 
            // menuResetOriginal
            // 
            menuResetOriginal.Name = "menuResetOriginal";
            menuResetOriginal.Size = new Size(174, 22);
            menuResetOriginal.Text = "원본으로 되돌리기";
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.DropDownItems.AddRange(new ToolStripItem[] { menuResize, menuCrop, menuRotateLeft, menuRotateRight, menuFlipHorizontal, menuFlipVertical, menuSnsRatio });
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.Size = new Size(55, 20);
            toolStripMenuItem3.Text = "이미지";
            // 
            // menuResize
            // 
            menuResize.Name = "menuResize";
            menuResize.Size = new Size(180, 22);
            menuResize.Text = "이미지 크기 조절";
            menuResize.ToolTipText = "이미지의 가로와 세로 크기를 변경합니다.";
            menuResize.Click += menuResize_Click;
            // 
            // menuCrop
            // 
            menuCrop.Name = "menuCrop";
            menuCrop.Size = new Size(180, 22);
            menuCrop.Text = "이미지 자르기";
            menuCrop.ToolTipText = "원하는 영역만 남기고 이미지를 자릅니다.";
            menuCrop.Click += menuCrop_Click;
            // 
            // menuRotateLeft
            // 
            menuRotateLeft.Name = "menuRotateLeft";
            menuRotateLeft.Size = new Size(180, 22);
            menuRotateLeft.Text = "왼쪽 회전";
            menuRotateLeft.ToolTipText = "이미지를 왼쪽으로 90도 회전합니다.";
            menuRotateLeft.Click += menuRotateLeft_Click;
            // 
            // menuRotateRight
            // 
            menuRotateRight.Name = "menuRotateRight";
            menuRotateRight.Size = new Size(180, 22);
            menuRotateRight.Text = "오른쪽 회전";
            menuRotateRight.ToolTipText = "이미지를 오른쪽으로 90도 회전합니다.";
            menuRotateRight.Click += menuRotateRight_Click;
            // 
            // menuFlipHorizontal
            // 
            menuFlipHorizontal.Name = "menuFlipHorizontal";
            menuFlipHorizontal.Size = new Size(180, 22);
            menuFlipHorizontal.Text = "좌우 반전";
            menuFlipHorizontal.ToolTipText = "이미지를 좌우 방향으로 뒤집습니다.";
            menuFlipHorizontal.Click += menuFlipHorizontal_Click;
            // 
            // menuFlipVertical
            // 
            menuFlipVertical.Name = "menuFlipVertical";
            menuFlipVertical.Size = new Size(180, 22);
            menuFlipVertical.Text = "상하 반전";
            menuFlipVertical.ToolTipText = "이미지를 위아래 방향으로 뒤집습니다.";
            menuFlipVertical.Click += menuFlipVertical_Click;
            // 
            // menuSnsRatio
            // 
            menuSnsRatio.Name = "menuSnsRatio";
            menuSnsRatio.Size = new Size(180, 22);
            menuSnsRatio.Text = "비율 변환";
            menuSnsRatio.ToolTipText = "원하는 화면 비율에 맞게 이미지를 변경합니다.";
            menuSnsRatio.Click += menuSnsRatio_Click;
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.DropDownItems.AddRange(new ToolStripItem[] { menuBrightness });
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            toolStripMenuItem4.Size = new Size(43, 20);
            toolStripMenuItem4.Text = "보정";
            // 
            // menuBrightness
            // 
            menuBrightness.Name = "menuBrightness";
            menuBrightness.Size = new Size(180, 22);
            menuBrightness.Text = "보정 설정";
            menuBrightness.ToolTipText = "밝기, 대비, 채도 등 이미지의 색감을 직접 조절합니다.";
            // 
            // toolStripMenuItem5
            // 
            toolStripMenuItem5.DropDownItems.AddRange(new ToolStripItem[] { menuBasicFilter, menuTemplate, menuCommunityFilter, menuMyStyle });
            toolStripMenuItem5.Name = "toolStripMenuItem5";
            toolStripMenuItem5.Size = new Size(43, 20);
            toolStripMenuItem5.Text = "필터";
            // 
            // menuBasicFilter
            // 
            menuBasicFilter.Name = "menuBasicFilter";
            menuBasicFilter.Size = new Size(180, 22);
            menuBasicFilter.Text = "기본 필터";
            menuBasicFilter.ToolTipText = "이미지에 다양한 분위기의 필터 효과를 적용합니다.";
            // 
            // menuTemplate
            // 
            menuTemplate.Name = "menuTemplate";
            menuTemplate.Size = new Size(178, 22);
            menuTemplate.Text = "템플릿";
            // 
            // menuCommunityFilter
            // 
            menuCommunityFilter.Name = "menuCommunityFilter";
            menuCommunityFilter.Size = new Size(178, 22);
            menuCommunityFilter.Text = "스티커";
            menuCommunityFilter.ToolTipText = "스티커를 직접 배치하거나 얼굴에 자동으로 맞춥니다.";
            // 
            // menuMyStyle
            // 
            menuMyStyle.Name = "menuMyStyle";
            menuMyStyle.Size = new Size(178, 22);
            menuMyStyle.Text = "나만의 편집 스타일";
            // 
            // toolStripMenuItem6
            // 
            toolStripMenuItem6.DropDownItems.AddRange(new ToolStripItem[] { menuAddText, menuEditText });
            toolStripMenuItem6.Name = "toolStripMenuItem6";
            toolStripMenuItem6.Size = new Size(55, 20);
            toolStripMenuItem6.Text = "텍스트";
            // 
            // menuAddText
            // 
            menuAddText.Name = "menuAddText";
            menuAddText.Size = new Size(138, 22);
            menuAddText.Text = "텍스트 추가";
            // 
            // menuEditText
            // 
            menuEditText.Name = "menuEditText";
            menuEditText.Size = new Size(138, 22);
            menuEditText.Text = "텍스트 편집";
            // 
            // toolStripMenuItem7
            // 
            toolStripMenuItem7.DropDownItems.AddRange(new ToolStripItem[] { menuDrawingTool, menuMask });
            toolStripMenuItem7.Name = "toolStripMenuItem7";
            toolStripMenuItem7.Size = new Size(55, 20);
            toolStripMenuItem7.Text = "그리기";
            // 
            // menuDrawingTool
            // 
            menuDrawingTool.Name = "menuDrawingTool";
            menuDrawingTool.Size = new Size(110, 22);
            menuDrawingTool.Text = "그리기";
            // 
            // menuMask
            // 
            menuMask.Name = "menuMask";
            menuMask.Size = new Size(110, 22);
            menuMask.Text = "마스크";
            // 
            // menuAIRemoveObject
            // 
            menuAIRemoveObject.DropDownItems.AddRange(new ToolStripItem[] { toolStripMenuItem23, menuAICaption, menuAITextPosition, menuAIMusicRecommend });
            menuAIRemoveObject.Name = "menuAIRemoveObject";
            menuAIRemoveObject.Size = new Size(30, 20);
            menuAIRemoveObject.Text = "AI";
            // 
            // toolStripMenuItem23
            // 
            toolStripMenuItem23.Name = "toolStripMenuItem23";
            toolStripMenuItem23.Size = new Size(180, 22);
            toolStripMenuItem23.Text = "AI 물체 제거";
            toolStripMenuItem23.ToolTipText = "선택한 영역의 물체를 AI를 이용해 제거합니다.";
            // 
            // menuAICaption
            // 
            menuAICaption.Name = "menuAICaption";
            menuAICaption.Size = new Size(154, 22);
            menuAICaption.Text = "유행어·밈 추천";
            // 
            // menuAITextPosition
            // 
            menuAITextPosition.Name = "menuAITextPosition";
            menuAITextPosition.Size = new Size(154, 22);
            menuAITextPosition.Text = "글씨 위치 추천";
            // 
            // menuAIMusicRecommend
            // 
            menuAIMusicRecommend.Name = "menuAIMusicRecommend";
            menuAIMusicRecommend.Size = new Size(180, 22);
            menuAIMusicRecommend.Text = "음악 추천";
            menuAIMusicRecommend.ToolTipText = "이미지의 분위기를 분석하여 어울리는 음악을 추천합니다.";
            // 
            // toolStripMenuItem9
            // 
            toolStripMenuItem9.DropDownItems.AddRange(new ToolStripItem[] { menuZoom, menuBeforeAfter, menuPreview });
            toolStripMenuItem9.Name = "toolStripMenuItem9";
            toolStripMenuItem9.Size = new Size(43, 20);
            toolStripMenuItem9.Text = "보기";
            // 
            // menuZoom
            // 
            menuZoom.DropDownItems.AddRange(new ToolStripItem[] { menuZoomIn, menuZoomOut, menuFitToScreen });
            menuZoom.Name = "menuZoom";
            menuZoom.Size = new Size(180, 22);
            menuZoom.Text = "확대/ 축소";
            // 
            // menuZoomIn
            // 
            menuZoomIn.Name = "menuZoomIn";
            menuZoomIn.Size = new Size(180, 22);
            menuZoomIn.Text = "확대";
            menuZoomIn.ToolTipText = "이미지를 화면에서 확대합니다.";
            menuZoomIn.Click += menuZoomIn_Click;
            // 
            // menuZoomOut
            // 
            menuZoomOut.Name = "menuZoomOut";
            menuZoomOut.Size = new Size(180, 22);
            menuZoomOut.Text = "축소";
            menuZoomOut.ToolTipText = "이미지를 화면에서 축소합니다.";
            menuZoomOut.Click += menuZoomOut_Click;
            // 
            // menuFitToScreen
            // 
            menuFitToScreen.Name = "menuFitToScreen";
            menuFitToScreen.Size = new Size(180, 22);
            menuFitToScreen.Text = "화면에 맞추기";
            menuFitToScreen.ToolTipText = "이미지 전체가 작업 영역 안에 보이도록 맞춥니다.";
            menuFitToScreen.Click += menuFitToScreen_Click;
            // 
            // menuBeforeAfter
            // 
            menuBeforeAfter.Name = "menuBeforeAfter";
            menuBeforeAfter.Size = new Size(180, 22);
            menuBeforeAfter.Text = "Before / After 비교";
            menuBeforeAfter.ToolTipText = "편집 전 원본과 현재 편집 결과를 비교합니다.";
            menuBeforeAfter.Click += menuBeforeAfter_Click;
            // 
            // menuPreview
            // 
            menuPreview.Name = "menuPreview";
            menuPreview.Size = new Size(180, 22);
            menuPreview.Text = "결과 미리보기";
            menuPreview.ToolTipText = "SNS별 비율로 최종 결과를 미리 확인합니다.";
            menuPreview.Click += menuPreview_Click;
            // 
            // menuContrast
            // 
            menuContrast.Name = "menuContrast";
            menuContrast.Size = new Size(122, 22);
            menuContrast.Text = "대비";
            // 
            // menuSaturation
            // 
            menuSaturation.Name = "menuSaturation";
            menuSaturation.Size = new Size(122, 22);
            menuSaturation.Text = "채도";
            // 
            // menuTemperature
            // 
            menuTemperature.Name = "menuTemperature";
            menuTemperature.Size = new Size(122, 22);
            menuTemperature.Text = "색온도";
            // 
            // pnlLeft
            // 
            pnlLeft.Controls.Add(tabControl1);
            pnlLeft.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            pnlLeft.Location = new Point(1, 37);
            pnlLeft.Name = "pnlLeft";
            pnlLeft.Size = new Size(220, 620);
            pnlLeft.TabIndex = 1;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabTimeLine);
            tabControl1.Controls.Add(tabLayer);
            tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl1.Location = new Point(6, 3);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(214, 617);
            tabControl1.TabIndex = 0;
            // 
            // tabTimeLine
            // 
            tabTimeLine.Location = new Point(4, 24);
            tabTimeLine.Name = "tabTimeLine";
            tabTimeLine.Padding = new Padding(3);
            tabTimeLine.Size = new Size(206, 589);
            tabTimeLine.TabIndex = 0;
            tabTimeLine.Text = "타임라인";
            tabTimeLine.UseVisualStyleBackColor = true;
            // 
            // tabLayer
            // 
            tabLayer.Location = new Point(4, 24);
            tabLayer.Name = "tabLayer";
            tabLayer.Padding = new Padding(3);
            tabLayer.Size = new Size(206, 589);
            tabLayer.TabIndex = 1;
            tabLayer.Text = "레이어";
            tabLayer.UseVisualStyleBackColor = true;
            // 
            // pnlCanvas
            // 
            pnlCanvas.AutoScroll = true;
            pnlCanvas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlCanvas.Controls.Add(picCanvas);
            pnlCanvas.Location = new Point(227, 37);
            pnlCanvas.Name = "pnlCanvas";
            pnlCanvas.Size = new Size(745, 620);
            pnlCanvas.TabIndex = 2;
            // 
            // picCanvas
            // 
            picCanvas.Anchor = AnchorStyles.None;
            picCanvas.Location = new Point(14, 14);
            picCanvas.Name = "picCanvas";
            picCanvas.Size = new Size(543, 488);
            picCanvas.SizeMode = PictureBoxSizeMode.StretchImage;
            picCanvas.TabIndex = 0;
            picCanvas.TabStop = false;
            // 
            // pnlToolOptions
            // 
            pnlToolOptions.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            pnlToolOptions.Location = new Point(978, 37);
            pnlToolOptions.Name = "pnlToolOptions";
            pnlToolOptions.Size = new Size(300, 620);
            pnlToolOptions.TabIndex = 3;
            // 
            // pnlBottom
            // 
            pnlBottom.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlBottom.Location = new Point(1, 663);
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Size = new Size(1277, 115);
            pnlBottom.TabIndex = 4;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label1.Location = new Point(1170, 9);
            label1.Name = "label1";
            label1.Size = new Size(43, 15);
            label1.TabIndex = 5;
            label1.Text = "로그인";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = false;
            ClientSize = new Size(1280, 780);
            Controls.Add(label1);
            Controls.Add(pnlBottom);
            Controls.Add(pnlToolOptions);
            Controls.Add(pnlCanvas);
            Controls.Add(pnlLeft);
            Controls.Add(pnlHeader);
            MainMenuStrip = menuStrip1;
            // 좌측 도구, 캔버스, 우측 상세 패널이 동시에 보이는 실제 최소 폭입니다.
            // 이보다 작게 줄이면 우측 상세 패널이 잘립니다.
            MinimumSize = new Size(1120, 720);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ImageEditor";
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            pnlLeft.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            pnlCanvas.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picCanvas).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel pnlHeader;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem toolStripMenuItem1;
        private Panel pnlLeft;
        private Panel pnlCanvas;
        private Panel pnlToolOptions;
        private Panel pnlBottom;
        private TabControl tabControl1;
        private TabPage tabTimeLine;
        private TabPage tabLayer;
        private PictureBox picCanvas;
        private Label label1;
        private ToolStripMenuItem menuOpen;
        private ToolStripMenuItem menuSave;
        private ToolStripMenuItem menuSaveAs;
        private ToolStripMenuItem menuExport;
        private ToolStripMenuItem toolStripMenuItem2;
        private ToolStripMenuItem menuUndo;
        private ToolStripMenuItem menuRedo;
        private ToolStripMenuItem menuResetOriginal;
        private ToolStripMenuItem toolStripMenuItem3;
        private ToolStripMenuItem menuResize;
        private ToolStripMenuItem menuCrop;
        private ToolStripMenuItem menuRotateLeft;
        private ToolStripMenuItem menuRotateRight;
        private ToolStripMenuItem menuFlipHorizontal;
        private ToolStripMenuItem menuFlipVertical;
        private ToolStripMenuItem menuSnsRatio;
        private ToolStripMenuItem toolStripMenuItem4;
        private ToolStripMenuItem toolStripMenuItem5;
        private ToolStripMenuItem toolStripMenuItem6;
        private ToolStripMenuItem toolStripMenuItem7;
        private ToolStripMenuItem menuAIRemoveObject;
        private ToolStripMenuItem menuBrightness;
        private ToolStripMenuItem menuContrast;
        private ToolStripMenuItem menuSaturation;
        private ToolStripMenuItem menuTemperature;
        private ToolStripMenuItem menuBasicFilter;
        private ToolStripMenuItem menuTemplate;
        private ToolStripMenuItem menuCommunityFilter;
        private ToolStripMenuItem menuMyStyle;
        private ToolStripMenuItem menuAddText;
        private ToolStripMenuItem menuEditText;
        private ToolStripMenuItem toolStripMenuItem9;
        private ToolStripMenuItem menuZoom;
        private ToolStripMenuItem menuBeforeAfter;
        private ToolStripMenuItem menuPreview;
        private ToolStripMenuItem menuDrawingTool;
        private ToolStripMenuItem menuMask;
        private ToolStripMenuItem toolStripMenuItem23;
        private ToolStripMenuItem menuAICaption;
        private ToolStripMenuItem menuAITextPosition;
        private ToolStripMenuItem menuAIMusicRecommend;
        private ToolStripMenuItem menuZoomIn;
        private ToolStripMenuItem menuZoomOut;
        private ToolStripMenuItem menuFitToScreen;
        private ToolStripMenuItem menuExit;
        private ToolStripMenuItem menuExportJpeg;
        private ToolStripMenuItem menuJpeg100;
        private ToolStripMenuItem menuJpeg85;
        private ToolStripMenuItem menuJpeg60;
        private ToolStripMenuItem menuExportPng;
        private ToolStripMenuItem menuExportSns;
        private ToolStripMenuItem menuInstagramSquareExport;
        private ToolStripMenuItem menuInstagramPortraitExport;
        private ToolStripMenuItem menuVerticalExport;
        private ToolStripMenuItem menuYoutubeThumbnailExport;
        private ToolStripMenuItem menuWebThumbnailExport;
    }
}
