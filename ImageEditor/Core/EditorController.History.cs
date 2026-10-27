using System;
using System.Collections.Generic;
using System.Drawing;

namespace ImageEditor.Core
{
    public partial class EditorController
    {
        private const int MaxUndoHistory = 20;
        private readonly List<EditorHistorySnapshot> undoHistory = new();
        private readonly List<EditorHistorySnapshot> redoHistory = new();

        public bool CanUndo => undoHistory.Count > 0;
        public bool CanRedo => redoHistory.Count > 0;

        public void PushUndoSnapshot()
        {
            if (State.CurrentImage == null)
                return;

            undoHistory.Add(CaptureSnapshot());
            ClearSnapshotList(redoHistory);
            State.MarkModified();

            if (undoHistory.Count > MaxUndoHistory)
            {
                undoHistory[0].Dispose();
                undoHistory.RemoveAt(0);
            }
        }

        public bool Undo()
        {
            if (undoHistory.Count == 0 || State.CurrentImage == null)
                return false;

            redoHistory.Add(CaptureSnapshot());

            int index = undoHistory.Count - 1;
            EditorHistorySnapshot snapshot = undoHistory[index];
            undoHistory.RemoveAt(index);

            RestoreSnapshot(snapshot);
            snapshot.Dispose();
            ResetMaskState();
            return true;
        }

        public bool Redo()
        {
            if (redoHistory.Count == 0 || State.CurrentImage == null)
                return false;

            undoHistory.Add(CaptureSnapshot());

            int index = redoHistory.Count - 1;
            EditorHistorySnapshot snapshot = redoHistory[index];
            redoHistory.RemoveAt(index);

            RestoreSnapshot(snapshot);
            snapshot.Dispose();
            ResetMaskState();
            return true;
        }

        public void ClearUndoHistory()
        {
            ClearSnapshotList(undoHistory);
            ClearSnapshotList(redoHistory);
        }

        public void DiscardLastUndoSnapshot()
        {
            if (undoHistory.Count == 0)
                return;

            int index = undoHistory.Count - 1;
            undoHistory[index].Dispose();
            undoHistory.RemoveAt(index);
        }

        // 현재 도구가 만든 첫 Undo 스냅샷을 되돌리되, Redo 기록으로 남기지 않는다.
        // 미리보기 취소에만 사용한다.
        public bool RevertLastUndoSnapshot()
        {
            if (undoHistory.Count == 0 || State.CurrentImage == null)
                return false;

            int index = undoHistory.Count - 1;
            EditorHistorySnapshot snapshot = undoHistory[index];
            undoHistory.RemoveAt(index);

            RestoreSnapshot(snapshot);
            snapshot.Dispose();
            ClearSnapshotList(redoHistory);
            ResetMaskState();
            return true;
        }

        private EditorHistorySnapshot CaptureSnapshot()
        {
            List<Layer> layers = CloneLayers(State.Layers);
            Bitmap? flatImage = layers.Count == 0 && State.CurrentImage != null
                ? new Bitmap(State.CurrentImage)
                : null;

            return new EditorHistorySnapshot(
                layers,
                flatImage,
                State.SelectedLayerIndex);
        }

        private void RestoreSnapshot(EditorHistorySnapshot snapshot)
        {
            DisposeLayers(State.Layers);
            State.Layers = CloneLayers(snapshot.Layers);

            if (State.Layers.Count > 0)
            {
                State.SelectedLayerIndex = Math.Max(
                    0,
                    Math.Min(snapshot.SelectedLayerIndex, State.Layers.Count - 1));

                RenderLayers();
                return;
            }

            if (snapshot.FlatImage != null)
                State.ReplaceCurrentImage(new Bitmap(snapshot.FlatImage));

            State.SelectedLayerIndex = -1;
        }

        private static List<Layer> CloneLayers(IEnumerable<Layer> source)
        {
            List<Layer> result = new();

            foreach (Layer layer in source)
            {
                Layer clone = new Layer(layer.Name, new Bitmap(layer.Image))
                {
                    Visible = layer.Visible,
                    Opacity = layer.Opacity,
                    Locked = layer.Locked
                };

                result.Add(clone);
            }

            return result;
        }

        private static void DisposeLayers(IEnumerable<Layer> layers)
        {
            foreach (Layer layer in layers)
            {
                layer.Image.Dispose();
            }
        }

        private static void ClearSnapshotList(List<EditorHistorySnapshot> snapshots)
        {
            foreach (EditorHistorySnapshot snapshot in snapshots)
                snapshot.Dispose();

            snapshots.Clear();
        }

        private sealed class EditorHistorySnapshot : IDisposable
        {
            public List<Layer> Layers { get; }
            public Bitmap? FlatImage { get; }
            public int SelectedLayerIndex { get; }

            public EditorHistorySnapshot(
                List<Layer> layers,
                Bitmap? flatImage,
                int selectedLayerIndex)
            {
                Layers = layers;
                FlatImage = flatImage;
                SelectedLayerIndex = selectedLayerIndex;
            }

            public void Dispose()
            {
                DisposeLayers(Layers);
                FlatImage?.Dispose();
            }
        }
    }
}
