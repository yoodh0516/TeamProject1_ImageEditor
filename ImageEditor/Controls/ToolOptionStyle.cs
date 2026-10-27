using System.Drawing;
using System.Windows.Forms;

namespace ImageEditor.Controls
{
    internal static class ToolOptionStyle
    {
        internal static readonly Color PanelBackground = Color.FromArgb(247, 248, 252);
        internal static readonly Color Accent = Color.FromArgb(79, 103, 246);
        internal static readonly Color AccentSoft = Color.FromArgb(238, 242, 255);
        internal static readonly Color Border = Color.FromArgb(211, 218, 232);
        internal static readonly Color Title = Color.FromArgb(15, 35, 65);
        internal static readonly Color Description = Color.FromArgb(100, 116, 139);

        internal static Panel CreateCard(int height)
        {
            return new Panel
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Top,
                Height = height,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(14, 12, 14, 12)
            };
        }

        internal static Button CreateSecondaryButton(string text)
        {
            Button button = new Button
            {
                Text = text,
                BackColor = Color.White,
                ForeColor = Title,
                FlatStyle = FlatStyle.Flat,
                Height = 38,
                AutoSize = false,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderColor = Border;
            return button;
        }

        internal static void StylePrimaryButton(Button button)
        {
            button.BackColor = Accent;
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.FlatAppearance.BorderSize = 0;
            button.Height = 38;
        }

        internal static Label CreateLabel(string text, float size, FontStyle style, Color color)
        {
            return new Label
            {
                AutoSize = true,
                Text = text,
                Font = new Font("맑은 고딕", size, style),
                ForeColor = color
            };
        }

        internal static Label CreateWrappedLabel(
            string text,
            float size,
            FontStyle style,
            Color color,
            int height)
        {
            return new Label
            {
                AutoSize = false,
                Height = height,
                Text = text,
                Font = new Font("맑은 고딕", size, style),
                ForeColor = color,
                UseCompatibleTextRendering = true
            };
        }

        internal static void ConfigureResponsiveFlow(FlowLayoutPanel root)
        {
            bool updating = false;
            void UpdateWidths()
            {
                if (updating || root.IsDisposed)
                    return;

                updating = true;
                try
                {
                    int width = root.ClientSize.Width - root.Padding.Horizontal;
                    bool needsVerticalScroll = root.VerticalScroll.Visible ||
                        root.DisplayRectangle.Height > root.ClientSize.Height;
                    if (needsVerticalScroll)
                        width -= SystemInformation.VerticalScrollBarWidth;

                    foreach (Control child in root.Controls)
                    {
                        child.Width = Math.Max(1, width - child.Margin.Horizontal);
                    }
                }
                finally
                {
                    updating = false;
                }
            }

            root.SizeChanged += (_, _) => UpdateWidths();
            root.Layout += (_, _) => UpdateWidths();
        }

        internal static void FillCardControl(
            Control card,
            Control child,
            int left = 14,
            int right = 14)
        {
            void UpdateWidth() =>
                child.Width = Math.Max(1, card.ClientSize.Width - left - right);

            child.Left = left;
            card.Resize += (_, _) => UpdateWidth();
            UpdateWidth();
        }
    }
}
