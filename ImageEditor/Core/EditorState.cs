using System.Collections.Generic;
using System.Drawing;

namespace ImageEditor.Core
{
    public class EditorState
    {
        public Bitmap? OriginalImage { get; set; }
        public Bitmap? CurrentImage { get; set; }
        public string? CurrentFilePath { get; set; }
        public bool IsDirty { get; private set; }
        public float ZoomRatio { get; private set; } = 1.0f;

        public List<Layer> Layers { get; set; } = new List<Layer>();
        public int SelectedLayerIndex { get; set; } = -1;

        public Layer? SelectedLayer
        {
            get
            {
                if (SelectedLayerIndex < 0 || SelectedLayerIndex >= Layers.Count)
                    return null;
                return Layers[SelectedLayerIndex];
            }
        }

        public void SetZoom(float zoom)
        {
            if (zoom < 0.1f) zoom = 0.1f;
            if (zoom > 5.0f) zoom = 5.0f;
            ZoomRatio = zoom;
        }

        public void ReplaceCurrentImage(Bitmap newImage)
        {
            if (newImage == null || ReferenceEquals(CurrentImage, newImage))
                return;

            Bitmap? oldImage = CurrentImage;
            CurrentImage = newImage;
            oldImage?.Dispose();
            MarkModified();
        }

        public void MarkModified()
        {
            if (CurrentImage != null)
                IsDirty = true;
        }

        public void MarkSaved()
        {
            IsDirty = false;
        }
    }
}
