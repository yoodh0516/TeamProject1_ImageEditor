using System.Drawing;

namespace ImageEditor.Core
{
    public class Layer
    {
        public string Name { get; set; }

        public Bitmap Image { get; set; }

        public bool Visible { get; set; }

        public float Opacity { get; set; }

        public bool Locked { get; set; }


        public Layer(
            string name,
            Bitmap image)
        {
            Name = name;

            Image = image;

            Visible = true;

            Opacity = 1.0f;

            Locked = false;
        }
    }
}
