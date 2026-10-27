using System;
using System.Windows.Forms;

namespace ImageEditor
{
    public partial class MainForm : Form
    {
        private void InitializeUndoRedoFeatures()
        {
            menuUndo.ShortcutKeys = Keys.Control | Keys.Z;
            menuRedo.ShortcutKeys = Keys.Control | Keys.Y;

            menuUndo.Click -= MenuUndo_Click;
            menuUndo.Click += MenuUndo_Click;

            menuRedo.Click -= MenuRedo_Click;
            menuRedo.Click += MenuRedo_Click;

            menuResetOriginal.Click -= MenuResetOriginal_Click;
            menuResetOriginal.Click += MenuResetOriginal_Click;

            menuExit.Click -= MenuExit_Click;
            menuExit.Click += MenuExit_Click;

            toolStripMenuItem2.DropDownOpening += (_, _) => UpdateUndoRedoMenuState();
            UpdateUndoRedoMenuState();
        }

        private void MenuUndo_Click(object? sender, EventArgs e)
        {
            StopInteractiveTools();

            Size previousSize = editor.State.CurrentImage?.Size ?? Size.Empty;

            if (!editor.Undo())
                return;

            RestoreCanvasAfterHistory(previousSize);
            RefreshLayerUI();
            RecordTimelineSnapshot();
            UpdateUndoRedoMenuState();
        }

        private void MenuRedo_Click(object? sender, EventArgs e)
        {
            StopInteractiveTools();

            Size previousSize = editor.State.CurrentImage?.Size ?? Size.Empty;

            if (!editor.Redo())
                return;

            RestoreCanvasAfterHistory(previousSize);
            RefreshLayerUI();
            RecordTimelineSnapshot();
            UpdateUndoRedoMenuState();
        }

        private void RestoreCanvasAfterHistory(Size previousSize)
        {
            Size restoredSize = editor.State.CurrentImage?.Size ?? Size.Empty;
            if (restoredSize != Size.Empty && restoredSize != previousSize)
            {
                editor.FitToPanel(GetCanvasViewportSize());
                pnlCanvas.AutoScrollPosition = Point.Empty;
            }

            RefreshCanvas();
        }

        private void MenuResetOriginal_Click(object? sender, EventArgs e)
        {
            if (editor.State.OriginalImage == null)
            {
                MessageBox.Show("먼저 이미지를 불러와주세요.");
                return;
            }

            DialogResult result = MessageBox.Show(
                "현재 이미지의 모든 편집 결과를 원본 상태로 되돌릴까요?\r\nUndo로 다시 되돌릴 수 있습니다.",
                "원본으로 되돌리기",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            StopInteractiveTools();
            editor.PushUndoSnapshot();
            editor.ResetToOriginal();
            RefreshAfterImageChange();
            UpdateUndoRedoMenuState();
        }

        private void MenuExit_Click(object? sender, EventArgs e)
        {
            Close();
        }

        private void UpdateUndoRedoMenuState()
        {
            menuUndo.Enabled = editor.CanUndo;
            menuRedo.Enabled = editor.CanRedo;
            if (btnQuickUndo != null)
                btnQuickUndo.Enabled = editor.CanUndo;
            if (btnQuickRedo != null)
                btnQuickRedo.Enabled = editor.CanRedo;
        }
    }
}
