using static CtrlCV.NativeMethods;

namespace CtrlCV
{
    internal class PreviewPopupForm : Form
    {
        private int MaxWidth => Scale(400);
        private int MaxTextHeight => Scale(300);
        private int MaxImageHeight => Scale(400);
        private int ContentPadding => Scale(8);

        private int Scale(int baseValue) => (int)Math.Round(baseValue * DeviceDpi / 96.0);

        private readonly RichTextBox _textBox;
        private readonly PictureBox _pictureBox;

        public PreviewPopupForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = SystemColors.Info;
            AutoSize = false;

            _textBox = new RichTextBox
            {
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Info,
                ForeColor = SystemColors.InfoText,
                WordWrap = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Visible = false,
                DetectUrls = false
            };
            Controls.Add(_textBox);

            _pictureBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = SystemColors.Info,
                Visible = false
            };
            Controls.Add(_pictureBox);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        public void ShowPreview(ClipboardItem item, Rectangle anchorBounds)
        {
            _textBox.Visible = false;
            _pictureBox.Visible = false;

            if (item.ItemType == ClipboardItemType.Text && item.Text != null)
            {
                SetupTextPreview(item.Text);
            }
            else if (item.ItemType == ClipboardItemType.Image && item.ImageData != null)
            {
                SetupImagePreview(item.ImageData);
            }
            else
            {
                return;
            }

            PositionRelativeTo(anchorBounds);

            if (!Visible)
                Show();
        }

        public void HidePreview()
        {
            if (Visible)
                Hide();
        }

        private void SetupTextPreview(string text)
        {
            _textBox.Text = text;
            int contentWidth = MaxWidth - ContentPadding * 2;

            using var g = CreateGraphics();
            var textSize = g.MeasureString(text, _textBox.Font, contentWidth);
            int height = Math.Min((int)textSize.Height + ContentPadding * 2 + 4, MaxTextHeight);
            int width = MaxWidth;

            if (textSize.Width < contentWidth * 0.6f && textSize.Height < MaxTextHeight * 0.3f)
                width = Math.Max((int)textSize.Width + ContentPadding * 2 + Scale(20), Scale(150));

            Size = new Size(width, height);
            _textBox.SetBounds(ContentPadding, ContentPadding, width - ContentPadding * 2, height - ContentPadding * 2);
            _textBox.Visible = true;
        }

        private void SetupImagePreview(Image image)
        {
            int imgW = image.Width;
            int imgH = image.Height;
            int availW = MaxWidth - ContentPadding * 2;
            int availH = MaxImageHeight - ContentPadding * 2;

            double scale = Math.Min(1.0, Math.Min((double)availW / imgW, (double)availH / imgH));
            int displayW = (int)(imgW * scale);
            int displayH = (int)(imgH * scale);

            Size = new Size(displayW + ContentPadding * 2, displayH + ContentPadding * 2);
            _pictureBox.SetBounds(ContentPadding, ContentPadding, displayW, displayH);
            _pictureBox.Image = image;
            _pictureBox.Visible = true;
        }

        private void PositionRelativeTo(Rectangle anchor)
        {
            var screen = Screen.FromRectangle(anchor).WorkingArea;
            int x = anchor.Left + (anchor.Width - Width) / 2;
            int y;

            int spaceAbove = anchor.Top - screen.Top;
            int spaceBelow = screen.Bottom - anchor.Bottom;

            int gap = Scale(4);
            if (spaceAbove >= Height + gap || spaceAbove >= spaceBelow)
                y = anchor.Top - Height - gap;
            else
                y = anchor.Bottom + gap;

            x = Math.Max(screen.Left, Math.Min(x, screen.Right - Width));
            y = Math.Max(screen.Top, Math.Min(y, screen.Bottom - Height));

            Location = new Point(x, y);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _pictureBox.Image = null;
                _textBox.Dispose();
                _pictureBox.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
