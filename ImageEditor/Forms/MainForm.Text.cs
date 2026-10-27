using ImageEditor.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor
{
    public partial class MainForm : Form
    {
        private TextToolPanel? textToolPanel;

        private void InitializeTextFeatures()
        {
            menuAddText.Click -= MenuAddText_Click;
            menuAddText.Click += MenuAddText_Click;

            menuAddText.ToolTipText = "텍스트를 만들어 새 레이어로 추가합니다.";
            toolStripMenuItem6.DropDownItems.Remove(menuEditText);
        }

        private void MenuAddText_Click(object? sender, EventArgs e)
        {
            if (editor.State.CurrentImage == null)
            {
                MessageBox.Show("먼저 이미지를 불러와주세요.");
                return;
            }

            StopInteractiveTools();

            OpenTextTool();
        }

        private void OpenTextTool(
            string? suggestedText = null,
            string placement = "bottom-left",
            string effect = "none",
            float anchorX = -1f,
            float anchorY = -1f,
            string alignment = "left",
            string textColor = "black",
            float fontSizeRatio = 0.058f,
            string fontStyle = "clean",
            string effectColor = "black",
            float effectIntensity = 0.55f)
        {
            if (editor.State.CurrentImage == null)
                return;

            textToolPanel = new TextToolPanel(picCanvas)
            {
                Dock = DockStyle.Fill
            };

            textToolPanel.ApplyRequested += TextToolPanel_ApplyRequested;
            textToolPanel.CancelRequested += TextToolPanel_CancelRequested;
            if (!string.IsNullOrWhiteSpace(suggestedText))
            {
                textToolPanel.SetSuggestedText(
                    suggestedText,
                    placement,
                    effect,
                    anchorX,
                    anchorY,
                    alignment,
                    textColor,
                    fontSizeRatio,
                    fontStyle,
                    effectColor,
                    effectIntensity);
            }
            ShowToolControl(textToolPanel);
            picCanvas.Invalidate();
        }

        private void TextToolPanel_ApplyRequested()
        {
            if (textToolPanel == null || editor.State.CurrentImage == null)
                return;

            if (!textToolPanel.HasText)
            {
                MessageBox.Show("추가할 텍스트를 입력해주세요.");
                return;
            }

            using Bitmap textLayer =
                textToolPanel.CreateTextLayer(editor.State.CurrentImage.Size);

            editor.PushUndoSnapshot();

            if (!editor.AddImageLayer(textLayer, "텍스트"))
            {
                MessageBox.Show("텍스트 레이어를 추가하지 못했습니다.");
                return;
            }

            CloseTextTool();
            RefreshCanvas();
            RefreshLayerUI();
            RecordTimelineSnapshot();
            UpdateUndoRedoMenuState();
        }

        private void TextToolPanel_CancelRequested()
        {
            CloseTextTool();
        }

        private void CloseTextTool()
        {
            if (textToolPanel == null)
                return;

            TextToolPanel panel = textToolPanel;
            textToolPanel = null;

            pnlToolOptions.Controls.Remove(panel);
            panel.Dispose();
            picCanvas.Invalidate();
        }
    }
}
