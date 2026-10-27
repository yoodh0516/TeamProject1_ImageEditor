using ImageEditor.Controls;

namespace ImageEditor {
    public partial class MainForm : Form {
        private MyEditStyleControl? myEditStyleControl;
        private EditStyle lastAppliedAdjustmentStyle = new EditStyle();

        private void InitializeEditStyleFeatures() {
            myEditStyleControl = new MyEditStyleControl(connectionAddress) {
                Dock = DockStyle.Fill
            };

            myEditStyleControl.GetCurrentSettings = GetLastAppliedAdjustmentStyle;
            myEditStyleControl.ApplySettings = ApplyEditStyle;

            menuMyStyle.Click -= menuMyStyle_Click;
            menuMyStyle.Click += menuMyStyle_Click;
        }

        private void UpdateMyEditStyleAvailability(bool hasImage) {
            bool loggedIn = CurrentUser.UserNo > 0;
            menuMyStyle.Enabled = loggedIn && hasImage;
            menuMyStyle.ToolTipText = loggedIn
                ? hasImage
                    ? "현재 보정 설정을 계정에 저장하거나 다시 불러옵니다."
                    : "이미지를 불러오면 나만의 편집 스타일을 사용할 수 있습니다."
                : "나만의 편집 스타일은 로그인 후 사용할 수 있습니다.";

            if (!loggedIn && myEditStyleControl != null &&
                pnlToolOptions.Controls.Contains(myEditStyleControl)) {
                FinishAdjustmentFilterSessions(false);
                ShowToolPlaceholder();
            }

            UpdateToolPlaceholderAccountState();
        }

        private void menuMyStyle_Click(object? sender, EventArgs e) {
            if (editor.State.CurrentImage == null || myEditStyleControl == null) {
                MessageBox.Show("먼저 이미지를 열어주세요.");
                return;
            }

            if (CurrentUser.UserNo <= 0) {
                MessageBox.Show(
                    "나만의 편집 스타일은 로그인 후 사용할 수 있습니다.",
                    "로그인 필요",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            StopInteractiveTools();
            BeginAdjustmentSession();
            myEditStyleControl.LoadPresetList();
            ShowToolControl(myEditStyleControl);
        }

        private EditStyle GetLastAppliedAdjustmentStyle() {
            return new EditStyle {
                Brightness = lastAppliedAdjustmentStyle.Brightness,
                Contrast = lastAppliedAdjustmentStyle.Contrast,
                Saturation = lastAppliedAdjustmentStyle.Saturation,
                Temperature = lastAppliedAdjustmentStyle.Temperature
            };
        }

        private void RememberAppliedAdjustment(EditStyle style) {
            lastAppliedAdjustmentStyle = new EditStyle {
                Brightness = style.Brightness,
                Contrast = style.Contrast,
                Saturation = style.Saturation,
                Temperature = style.Temperature
            };
        }

        private void ResetLastAppliedAdjustmentStyle() {
            lastAppliedAdjustmentStyle = new EditStyle();
        }

        private async void ApplyEditStyle(EditStyle style) {
            if (editor.State.CurrentImage == null || adjustmentControl == null)
                return;

            if (adjustmentBaseImage == null)
                BeginAdjustmentSession();

            if (adjustmentBaseImage == null)
                return;

            adjustmentControl.SetAdjustmentValues(
                style.Brightness,
                style.Contrast,
                style.Saturation,
                style.Temperature);

            PreviewAdjustmentValues(
                style.Brightness,
                style.Contrast,
                style.Saturation,
                style.Temperature);

            try {
                while (adjustmentSessionActive &&
                       (adjustmentPreviewQueued || adjustmentPreviewProcessing)) {
                    await FlushAdjustmentPreviewAsync(true);
                }

                CommitAdjustmentSession();
            } catch (Exception ex) {
                MessageBox.Show(
                    "저장된 보정 스타일을 적용할 수 없습니다.\r\n" + ex.Message,
                    "나만의 편집 스타일",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}
