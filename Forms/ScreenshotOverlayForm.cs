using Microsoft.Win32;

namespace CtrlCV
{
    internal class ScreenshotOverlayForm : Form
    {
        public Rectangle SelectedRegion { get; private set; }

        private Point _startPoint;
        private Rectangle _currentRect;
        private bool _isDragging;

        private readonly Pen _borderPen;
        private readonly SolidBrush _fillBrush;

        public ScreenshotOverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            Cursor = Cursors.Cross;
            BackColor = Color.Black;
            Opacity = 0.3;
            KeyPreview = true;

            _borderPen = new Pen(Color.FromArgb(200, 0, 120, 215), 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
            _fillBrush = new SolidBrush(Color.FromArgb(50, 0, 120, 215));

            var virtualScreen = SystemInformation.VirtualScreen;
            Bounds = virtualScreen;

            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _startPoint = e.Location;
                _currentRect = Rectangle.Empty;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isDragging)
            {
                int x = Math.Min(_startPoint.X, e.X);
                int y = Math.Min(_startPoint.Y, e.Y);
                int w = Math.Abs(e.X - _startPoint.X);
                int h = Math.Abs(e.Y - _startPoint.Y);
                _currentRect = new Rectangle(x, y, w, h);
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_isDragging && e.Button == MouseButtons.Left)
            {
                _isDragging = false;

                if (_currentRect.Width > 5 && _currentRect.Height > 5)
                {
                    var screenOffset = SystemInformation.VirtualScreen.Location;
                    SelectedRegion = new Rectangle(
                        _currentRect.X + screenOffset.X,
                        _currentRect.Y + screenOffset.Y,
                        _currentRect.Width,
                        _currentRect.Height);

                    DialogResult = DialogResult.OK;
                }
                else
                {
                    DialogResult = DialogResult.Cancel;
                }

                Close();
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_isDragging && _currentRect.Width > 0 && _currentRect.Height > 0)
            {
                e.Graphics.FillRectangle(_fillBrush, _currentRect);
                e.Graphics.DrawRectangle(_borderPen, _currentRect);
            }
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(CancelDueToDisplayChange)); }
                catch (ObjectDisposedException) { }
            }
            else
            {
                CancelDueToDisplayChange();
            }
        }

        private void CancelDueToDisplayChange()
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
                _borderPen.Dispose();
                _fillBrush.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW - don't show in taskbar/alt-tab
                return cp;
            }
        }
    }
}
