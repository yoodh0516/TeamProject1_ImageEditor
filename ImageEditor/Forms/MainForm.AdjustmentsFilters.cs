using ImageEditor.Controls;
using ImageEditor.ImageProcessing;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageEditor
{
    public partial class MainForm : Form
    {
        private AdjustmentControl? adjustmentControl;
        private Bitmap? adjustmentBaseImage;
        private Bitmap? adjustmentPreviewImage;
        private FilterControl? filterControl;
        private Bitmap? filterBaseImage;
        private Bitmap? filterPreviewImage;
        private bool adjustmentSessionActive;
        private bool adjustmentSessionDirty;
        private bool filterSessionActive;
        private bool filterSessionDirty;
        private readonly System.Windows.Forms.Timer adjustmentPreviewTimer =
            new System.Windows.Forms.Timer { Interval = 70 };
        private bool adjustmentPreviewQueued;
        private int queuedBrightness;
        private int queuedContrast;
        private int queuedSaturation;
        private int queuedTemperature;
        private int adjustmentPreviewVersion;
        private bool adjustmentPreviewProcessing;
        private Task<Bitmap>? adjustmentPreviewTask;
        private readonly System.Windows.Forms.Timer filterPreviewTimer =
            new System.Windows.Forms.Timer { Interval = 80 };
        private bool filterPreviewQueued;
        private bool filterPreviewProcessing;
        private int filterPreviewVersion;
        private int queuedFilterBrightness;
        private int queuedFilterContrast;
        private int queuedFilterSaturation;
        private int queuedFilterTemperature;
        private Task<Bitmap>? filterPreviewTask;

        private void InitializeAdjustmentFilterFeatures()
        {
            adjustmentControl = new AdjustmentControl { Dock = DockStyle.Fill };
            adjustmentControl.AdjustmentChanged += AdjustmentControl_AdjustmentChanged;
            adjustmentControl.ResetRequested += AdjustmentControl_ResetRequested;
            adjustmentControl.ApplyRequested += AdjustmentControl_ApplyRequested;
            adjustmentControl.CancelRequested += AdjustmentControl_CancelRequested;
            adjustmentPreviewTimer.Tick += AdjustmentPreviewTimer_Tick;

            filterControl = new FilterControl { Dock = DockStyle.Fill };
            filterControl.FilterSelected += FilterControl_FilterSelected;
            filterControl.ApplyRequested += FilterControl_ApplyRequested;
            filterControl.CancelRequested += FilterControl_CancelRequested;
            filterControl.ResetRequested += FilterControl_ResetRequested;
            filterPreviewTimer.Tick += FilterPreviewTimer_Tick;

            menuBrightness.Click -= menuAdjustment_Click;
            menuBrightness.Click += menuAdjustment_Click;
            menuBasicFilter.Click -= menuBasicFilter_Click;
            menuBasicFilter.Click += menuBasicFilter_Click;
            menuTemplate.Click -= menuTemplate_Click;
            menuTemplate.Click += menuTemplate_Click;
        }

        private void menuAdjustment_Click(object? sender, EventArgs e)
        {
            if (editor.State.CurrentImage == null || adjustmentControl == null)
            {
                MessageBox.Show("먼저 이미지를 열어주세요.");
                return;
            }

            StopInteractiveTools();
            BeginAdjustmentSession();
            ShowToolControl(adjustmentControl);
        }

        private void BeginAdjustmentSession()
        {
            if (editor.State.CurrentImage == null) return;
            adjustmentPreviewQueued = false;
            adjustmentPreviewTimer.Stop();
            adjustmentPreviewVersion++;
            DisposeBitmap(ref adjustmentBaseImage);
            DisposeBitmap(ref adjustmentPreviewImage);
            adjustmentBaseImage = new Bitmap(editor.State.CurrentImage);
            adjustmentSessionActive = true;
            adjustmentSessionDirty = false;
            adjustmentControl?.ResetSliders(false);
            adjustmentControl?.SetApplyEnabled(false);
        }

        private void AdjustmentControl_AdjustmentChanged(int brightness, int contrast, int saturation, int temperature)
        {
            if (!adjustmentSessionActive || adjustmentBaseImage == null) return;
            queuedBrightness = brightness;
            queuedContrast = contrast;
            queuedSaturation = saturation;
            queuedTemperature = temperature;
            adjustmentPreviewVersion++;
            adjustmentPreviewQueued = true;
            if (!adjustmentPreviewTimer.Enabled) adjustmentPreviewTimer.Start();
        }

        private async void AdjustmentPreviewTimer_Tick(object? sender, EventArgs e) =>
            await FlushAdjustmentPreviewAsync();

        private async Task FlushAdjustmentPreviewAsync(bool waitForPendingRender = false)
        {
            if (adjustmentPreviewProcessing)
            {
                if (waitForPendingRender && adjustmentPreviewTask != null)
                    await adjustmentPreviewTask;
                else
                    return;
            }

            if (!adjustmentPreviewQueued || adjustmentBaseImage == null)
            {
                adjustmentPreviewTimer.Stop();
                return;
            }

            adjustmentPreviewQueued = false;
            adjustmentPreviewTimer.Stop();

            int previewVersion = adjustmentPreviewVersion;
            int brightness = queuedBrightness;
            int contrast = queuedContrast;
            int saturation = queuedSaturation;
            int temperature = queuedTemperature;
            using Bitmap baseCopy = new Bitmap(adjustmentBaseImage);

            adjustmentPreviewProcessing = true;
            adjustmentPreviewTask = Task.Run(() => BasicImageProcessor.Adjust(
                baseCopy, brightness, contrast, saturation, temperature));

            Bitmap preview;
            try
            {
                preview = await adjustmentPreviewTask;
            }
            catch (Exception ex)
            {
                if (adjustmentSessionActive && previewVersion == adjustmentPreviewVersion)
                {
                    MessageBox.Show(
                        "보정 미리보기를 만들 수 없습니다.\r\n" + ex.Message,
                        "색상 보정",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                return;
            }
            finally
            {
                adjustmentPreviewProcessing = false;
                adjustmentPreviewTask = null;
            }

            if (!adjustmentSessionActive || previewVersion != adjustmentPreviewVersion)
            {
                preview.Dispose();
                if (adjustmentSessionActive && adjustmentPreviewQueued && !adjustmentPreviewTimer.Enabled)
                    adjustmentPreviewTimer.Start();
                return;
            }

            ReplaceBitmap(ref adjustmentPreviewImage, preview);
            adjustmentSessionDirty = true;
            adjustmentControl?.SetApplyEnabled(true);
            RefreshCanvas(adjustmentPreviewImage);

            if (adjustmentPreviewQueued && !adjustmentPreviewTimer.Enabled)
                adjustmentPreviewTimer.Start();
        }

        private void AdjustmentControl_ResetRequested()
        {
            adjustmentPreviewQueued = false;
            adjustmentPreviewTimer.Stop();
            adjustmentPreviewVersion++;
            adjustmentControl?.ResetSliders(false);
            DisposeBitmap(ref adjustmentPreviewImage);
            adjustmentSessionDirty = false;
            adjustmentControl?.SetApplyEnabled(false);
            RefreshCanvas();
        }

        private void PreviewAdjustmentValues(int brightness, int contrast, int saturation, int temperature)
        {
            if (adjustmentBaseImage == null)
                return;

            queuedBrightness = brightness;
            queuedContrast = contrast;
            queuedSaturation = saturation;
            queuedTemperature = temperature;
            adjustmentPreviewVersion++;
            adjustmentPreviewQueued = true;
            if (!adjustmentPreviewTimer.Enabled)
                adjustmentPreviewTimer.Start();
        }

        private async void AdjustmentControl_ApplyRequested()
        {
            adjustmentControl?.SetApplyEnabled(false);
            try
            {
                while (adjustmentSessionActive &&
                       (adjustmentPreviewQueued || adjustmentPreviewProcessing))
                {
                    await FlushAdjustmentPreviewAsync(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "보정 결과를 적용할 수 없습니다.\r\n" + ex.Message,
                    "색상 보정",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                adjustmentControl?.SetApplyEnabled(true);
                return;
            }
            CommitAdjustmentSession();
            HideSessionControl(adjustmentControl);
        }

        private void AdjustmentControl_CancelRequested()
        {
            CancelAdjustmentSession();
            HideSessionControl(adjustmentControl);
        }

        private void menuBasicFilter_Click(object? sender, EventArgs e)
        {
            if (editor.State.CurrentImage == null || filterControl == null)
            {
                MessageBox.Show("먼저 이미지를 열어주세요.");
                return;
            }

            StopInteractiveTools();
            DisposeBitmap(ref filterBaseImage);
            DisposeBitmap(ref filterPreviewImage);
            filterBaseImage = new Bitmap(editor.State.CurrentImage);
            filterSessionActive = true;
            filterSessionDirty = false;
            filterPreviewQueued = false;
            filterPreviewTimer.Stop();
            filterPreviewVersion++;
            filterControl.SetApplyEnabled(false);
            ShowToolControl(filterControl);
        }

        private void FilterControl_FilterSelected(string filterName)
        {
            if (!filterSessionActive || filterBaseImage == null) return;
            (int brightness, int contrast, int saturation, int temperature) = filterName switch
            {
                "화사하게" => (20, 10, 20, 5),
                "환하게" => (30, 5, 5, 0),
                "아늑하게" => (5, -5, 10, 20),
                "우아하게" => (5, 15, -10, 8),
                "흑백" => (0, 10, -100, 0),
                "시원하게" => (5, 10, 10, -20),
                _ => (0, 0, 0, 0)
            };
            queuedFilterBrightness = brightness;
            queuedFilterContrast = contrast;
            queuedFilterSaturation = saturation;
            queuedFilterTemperature = temperature;
            filterPreviewVersion++;
            filterPreviewQueued = true;
            filterControl?.SetApplyEnabled(false);
            filterPreviewTimer.Stop();
            filterPreviewTimer.Start();
        }

        private async void FilterPreviewTimer_Tick(object? sender, EventArgs e) =>
            await FlushFilterPreviewAsync();

        private async Task FlushFilterPreviewAsync(bool waitForPendingRender = false)
        {
            if (filterPreviewProcessing)
            {
                if (waitForPendingRender && filterPreviewTask != null)
                    await filterPreviewTask;
                return;
            }

            if (!filterPreviewQueued || filterBaseImage == null)
            {
                filterPreviewTimer.Stop();
                return;
            }

            filterPreviewQueued = false;
            filterPreviewTimer.Stop();
            int previewVersion = filterPreviewVersion;
            int brightness = queuedFilterBrightness;
            int contrast = queuedFilterContrast;
            int saturation = queuedFilterSaturation;
            int temperature = queuedFilterTemperature;
            using Bitmap baseCopy = new Bitmap(filterBaseImage);

            filterPreviewProcessing = true;
            filterPreviewTask = Task.Run(() => BasicImageProcessor.Adjust(
                baseCopy,
                brightness,
                contrast,
                saturation,
                temperature));

            Bitmap preview;
            try
            {
                preview = await filterPreviewTask;
            }
            catch (Exception ex)
            {
                if (filterSessionActive && previewVersion == filterPreviewVersion)
                {
                    MessageBox.Show(
                        "필터 미리보기를 만들 수 없습니다.\r\n" + ex.Message,
                        "필터",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                return;
            }
            finally
            {
                filterPreviewProcessing = false;
                filterPreviewTask = null;
            }

            if (!filterSessionActive || previewVersion != filterPreviewVersion)
            {
                preview.Dispose();
                if (filterSessionActive && filterPreviewQueued)
                    filterPreviewTimer.Start();
                return;
            }

            ReplaceBitmap(ref filterPreviewImage, preview);
            filterSessionDirty = true;
            filterControl?.SetApplyEnabled(true);
            RefreshCanvas(filterPreviewImage);

            if (filterPreviewQueued)
                filterPreviewTimer.Start();
        }

        private async void FilterControl_ApplyRequested()
        {
            filterControl?.SetApplyEnabled(false);
            try
            {
                while (filterSessionActive &&
                       (filterPreviewQueued || filterPreviewProcessing))
                {
                    await FlushFilterPreviewAsync(true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "필터 결과를 적용할 수 없습니다.\r\n" + ex.Message,
                    "필터",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                filterControl?.SetApplyEnabled(true);
                return;
            }
            CommitFilterSession();
            HideSessionControl(filterControl);
        }

        private void FilterControl_CancelRequested()
        {
            CancelFilterSession();
            HideSessionControl(filterControl);
        }

        private void FilterControl_ResetRequested()
        {
            if (!filterSessionActive) return;
            filterPreviewQueued = false;
            filterPreviewTimer.Stop();
            filterPreviewVersion++;
            DisposeBitmap(ref filterPreviewImage);
            filterSessionDirty = false;
            filterControl?.SetApplyEnabled(false);
            RefreshCanvas();
        }

        private void menuTemplate_Click(object? sender, EventArgs e) => OpenTemplatePanel();

        // true is reserved for a user pressing the explicit Apply button.
        private void FinishAdjustmentFilterSessions(bool commit)
        {
            if (commit)
            {
                CommitAdjustmentSession();
                CommitFilterSession();
            }
            else
            {
                CancelAdjustmentSession();
                CancelFilterSession();
            }
        }

        private void CommitAdjustmentSession()
        {
            if (!adjustmentSessionActive) return;
            if (adjustmentSessionDirty && adjustmentPreviewImage != null)
            {
                if (adjustmentControl != null)
                    RememberAppliedAdjustment(adjustmentControl.GetCurrentValues());

                editor.PushUndoSnapshot();
                editor.State.ReplaceCurrentImage(new Bitmap(adjustmentPreviewImage));
                editor.InitializeLayers();
                RefreshAfterImageChange();
            }
            EndAdjustmentSession();
            UpdateUndoRedoMenuState();
        }

        private void CommitFilterSession()
        {
            if (!filterSessionActive) return;
            if (filterSessionDirty && filterPreviewImage != null)
            {
                editor.PushUndoSnapshot();
                editor.State.ReplaceCurrentImage(new Bitmap(filterPreviewImage));
                editor.InitializeLayers();
                RefreshAfterImageChange();
            }
            EndFilterSession();
            UpdateUndoRedoMenuState();
        }

        private void CancelAdjustmentSession()
        {
            if (!adjustmentSessionActive) return;
            adjustmentPreviewQueued = false;
            adjustmentPreviewTimer.Stop();
            adjustmentPreviewVersion++;
            EndAdjustmentSession();
            adjustmentControl?.ResetSliders(false);
            adjustmentControl?.SetApplyEnabled(false);
            RefreshCanvas();
            UpdateUndoRedoMenuState();
        }

        private void CancelFilterSession()
        {
            if (!filterSessionActive) return;
            EndFilterSession();
            filterControl?.SetApplyEnabled(false);
            RefreshCanvas();
            UpdateUndoRedoMenuState();
        }

        private void EndAdjustmentSession()
        {
            adjustmentPreviewVersion++;
            DisposeBitmap(ref adjustmentBaseImage);
            DisposeBitmap(ref adjustmentPreviewImage);
            adjustmentSessionActive = false;
            adjustmentSessionDirty = false;
        }

        private void EndFilterSession()
        {
            filterPreviewQueued = false;
            filterPreviewTimer.Stop();
            filterPreviewVersion++;
            DisposeBitmap(ref filterBaseImage);
            DisposeBitmap(ref filterPreviewImage);
            filterSessionActive = false;
            filterSessionDirty = false;
        }

        private void HideSessionControl(UserControl? control)
        {
            if (control != null && pnlToolOptions.Controls.Contains(control))
                pnlToolOptions.Controls.Remove(control);
        }

        private static void ReplaceBitmap(ref Bitmap? target, Bitmap replacement)
        {
            target?.Dispose();
            target = replacement;
        }

        private static void DisposeBitmap(ref Bitmap? bitmap)
        {
            bitmap?.Dispose();
            bitmap = null;
        }
    }
}
