using System.Runtime.InteropServices;
using static CtrlCV.NativeMethods;

namespace CtrlCV
{
    internal class FloatingWidgetForm : Form
    {
        private const int GripSize = 6;
        private const int NormalCellSize = 48;
        private const int CompactCellSize = 24;
        private const int CellMargin = 2;
        private const int DragDeadZone = 4;
        private const int RefreshDebounceMs = 50;

        private static readonly Color TextSlotColor = Color.FromArgb(60, 120, 200);
        private static readonly Color ImageSlotColor = Color.FromArgb(60, 170, 80);
        private static readonly Color EmptySlotColor = Color.FromArgb(140, 140, 140);
        private static readonly Color PinnedBorderColor = Color.FromArgb(220, 180, 40);
        private static readonly Color BackgroundColor = Color.FromArgb(45, 45, 48);
        private static readonly Color GripColor = Color.FromArgb(80, 80, 85);

        private readonly ClipboardManager _clipboardManager;
        private readonly PasteService _pasteService;
        private readonly AppSettings _settings;
        private readonly PreviewPopupForm _previewPopup;
        private readonly System.Windows.Forms.Timer _hoverTimer;
        private readonly System.Windows.Forms.Timer _autoHideTimer;
        private readonly System.Windows.Forms.Timer _fadeTimer;
        private readonly System.Windows.Forms.Timer _refreshDebounceTimer;

        private Image?[] _thumbCache = Array.Empty<Image?>();
        private int _hoveredSlotIndex = -1;
        private Point _mouseDownPoint;
        private bool _mouseIsDown;
        private bool _refreshPending;
        private double _targetOpacity;
        private bool _isFadingOut;

        private readonly StringFormat _centerFormat = new()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };

        public FloatingWidgetForm(ClipboardManager clipboardManager, PasteService pasteService, AppSettings settings)
        {
            _clipboardManager = clipboardManager;
            _pasteService = pasteService;
            _settings = settings;

            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            BackColor = BackgroundColor;

            _previewPopup = new PreviewPopupForm();

            _hoverTimer = new System.Windows.Forms.Timer { Interval = 400 };
            _hoverTimer.Tick += HoverTimer_Tick;

            _autoHideTimer = new System.Windows.Forms.Timer();
            _autoHideTimer.Tick += AutoHideTimer_Tick;

            _fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _fadeTimer.Tick += FadeTimer_Tick;

            _refreshDebounceTimer = new System.Windows.Forms.Timer { Interval = RefreshDebounceMs };
            _refreshDebounceTimer.Tick += RefreshDebounceTimer_Tick;

            ApplySettings();
            RebuildThumbnailCache();
            CalculateAndResize();
            RestorePosition();
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

        #region Public API

        public void RefreshSlots()
        {
            _previewPopup.HidePreview();
            _hoveredSlotIndex = -1;
            _hoverTimer.Stop();

            if (_refreshPending)
                return;
            _refreshPending = true;
            _refreshDebounceTimer.Start();
        }

        public void ApplySettings()
        {
            _targetOpacity = _settings.WidgetOpacity;
            if (!_isFadingOut)
            {
                try { Opacity = _targetOpacity; }
                catch { }
            }

            _autoHideTimer.Interval = Math.Max(500, _settings.WidgetAutoHideDelayMs);
        }

        #endregion

        #region Rendering

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            DrawGrip(g);

            var slots = _clipboardManager.Slots;
            int maxSlots = _settings.MaxSlots;
            int cellSize = _settings.WidgetCompactMode ? CompactCellSize : NormalCellSize;
            bool vertical = _settings.WidgetVertical;

            if (slots.Count == 0)
            {
                DrawEmptyState(g, cellSize, vertical);
                return;
            }

            for (int i = 0; i < slots.Count && i < maxSlots; i++)
            {
                var rect = GetSlotRect(i, cellSize, vertical);
                DrawSlot(g, slots[i], i, rect, _settings.WidgetCompactMode);
            }
        }

        private void DrawGrip(Graphics g)
        {
            bool vertical = _settings.WidgetVertical;
            Rectangle gripRect;

            if (vertical)
                gripRect = new Rectangle(0, 0, Width, GripSize);
            else
                gripRect = new Rectangle(0, 0, GripSize, Height);

            using var brush = new SolidBrush(GripColor);
            g.FillRectangle(brush, gripRect);

            using var dotBrush = new SolidBrush(Color.FromArgb(120, 120, 125));
            if (vertical)
            {
                int cx = Width / 2;
                g.FillEllipse(dotBrush, cx - 6, 2, 3, 3);
                g.FillEllipse(dotBrush, cx, 2, 3, 3);
                g.FillEllipse(dotBrush, cx + 6, 2, 3, 3);
            }
            else
            {
                int cy = Height / 2;
                g.FillEllipse(dotBrush, 2, cy - 6, 3, 3);
                g.FillEllipse(dotBrush, 2, cy, 3, 3);
                g.FillEllipse(dotBrush, 2, cy + 6, 3, 3);
            }
        }

        private void DrawEmptyState(Graphics g, int cellSize, bool vertical)
        {
            var rect = vertical
                ? new Rectangle(0, GripSize, Width, Height - GripSize)
                : new Rectangle(GripSize, 0, Width - GripSize, Height);

            using var brush = new SolidBrush(Color.FromArgb(160, 160, 165));
            using var font = new Font("Segoe UI", 7f);
            g.DrawString("Empty", font, brush, rect, _centerFormat);
        }

        private void DrawSlot(Graphics g, ClipboardItem slot, int index, Rectangle rect, bool compact)
        {
            Color borderColor = slot.IsPinned ? PinnedBorderColor
                : slot.ItemType == ClipboardItemType.Text ? TextSlotColor
                : ImageSlotColor;

            bool isHovered = index == _hoveredSlotIndex;

            if (compact)
                DrawCompactSlot(g, slot, index, rect, borderColor, isHovered);
            else
                DrawNormalSlot(g, slot, index, rect, borderColor, isHovered);
        }

        private void DrawCompactSlot(Graphics g, ClipboardItem slot, int index, Rectangle rect, Color color, bool hovered)
        {
            Color fill = hovered ? Color.FromArgb(Math.Min(255, color.R + 40), Math.Min(255, color.G + 40), Math.Min(255, color.B + 40)) : color;

            using var brush = new SolidBrush(fill);
            g.FillEllipse(brush, rect);

            using var textBrush = new SolidBrush(Color.White);
            using var font = new Font("Segoe UI", 8f, FontStyle.Bold);
            string label = (index + 1) == 10 ? "0" : (index + 1).ToString();
            g.DrawString(label, font, textBrush, rect, _centerFormat);

            if (slot.IsPinned)
            {
                using var pinPen = new Pen(PinnedBorderColor, 2);
                g.DrawEllipse(pinPen, rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2);
            }
        }

        private void DrawNormalSlot(Graphics g, ClipboardItem slot, int index, Rectangle rect, Color borderColor, bool hovered)
        {
            Color bgColor = hovered ? Color.FromArgb(60, 60, 65) : Color.FromArgb(50, 50, 55);
            using var bgBrush = new SolidBrush(bgColor);
            g.FillRectangle(bgBrush, rect);

            using var borderPen = new Pen(borderColor, slot.IsPinned ? 2 : 1);
            g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);

            var contentRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);

            if (slot.ItemType == ClipboardItemType.Image && index < _thumbCache.Length && _thumbCache[index] != null)
            {
                try
                {
                    g.DrawImage(_thumbCache[index]!, contentRect);
                }
                catch
                {
                }
            }
            else if (slot.ItemType == ClipboardItemType.Text && slot.Text != null)
            {
                using var textBrush = new SolidBrush(Color.FromArgb(200, 200, 200));
                using var font = new Font("Segoe UI", 6.5f);
                string preview = slot.Text.Length > 30 ? slot.Text[..30] : slot.Text;
                preview = preview.ReplaceLineEndings(" ");
                g.DrawString(preview, font, textBrush, contentRect, _centerFormat);
            }

            using var numberBrush = new SolidBrush(Color.FromArgb(180, borderColor.R, borderColor.G, borderColor.B));
            using var numFont = new Font("Segoe UI", 6f, FontStyle.Bold);
            string num = (index + 1) == 10 ? "0" : (index + 1).ToString();
            g.DrawString(num, numFont, numberBrush, rect.X + 2, rect.Y + 1);
        }

        #endregion

        #region Layout

        private void CalculateAndResize()
        {
            var slots = _clipboardManager.Slots;
            int cellSize = _settings.WidgetCompactMode ? CompactCellSize : NormalCellSize;
            bool vertical = _settings.WidgetVertical;

            if (slots.Count == 0)
            {
                int emptyLen = Math.Max(cellSize * 2, 60) + CellMargin * 2;
                if (vertical)
                    Size = new Size(cellSize + CellMargin * 2, GripSize + emptyLen);
                else
                    Size = new Size(GripSize + emptyLen, cellSize + CellMargin * 2);
                return;
            }

            int contentLength = slots.Count * (cellSize + CellMargin) + CellMargin;

            if (vertical)
                Size = new Size(cellSize + CellMargin * 2, GripSize + contentLength);
            else
                Size = new Size(GripSize + contentLength, cellSize + CellMargin * 2);
        }

        private Rectangle GetSlotRect(int index, int cellSize, bool vertical)
        {
            int offset = index * (cellSize + CellMargin) + CellMargin;

            if (vertical)
                return new Rectangle(CellMargin, GripSize + offset, cellSize, cellSize);
            else
                return new Rectangle(GripSize + offset, CellMargin, cellSize, cellSize);
        }

        private int HitTestSlot(Point p)
        {
            var slots = _clipboardManager.Slots;
            int cellSize = _settings.WidgetCompactMode ? CompactCellSize : NormalCellSize;
            bool vertical = _settings.WidgetVertical;

            for (int i = 0; i < slots.Count; i++)
            {
                var rect = GetSlotRect(i, cellSize, vertical);
                if (rect.Contains(p))
                    return i;
            }
            return -1;
        }

        private void RestorePosition()
        {
            if (_settings.WidgetLeft >= 0 && _settings.WidgetTop >= 0)
            {
                var proposed = new Point(_settings.WidgetLeft, _settings.WidgetTop);
                if (IsPositionOnScreen(proposed))
                {
                    Location = proposed;
                    return;
                }
            }

            CenterOnPrimaryScreen();
        }

        private bool IsPositionOnScreen(Point p)
        {
            var testRect = new Rectangle(p, Size);
            foreach (var screen in Screen.AllScreens)
            {
                var overlap = Rectangle.Intersect(testRect, screen.WorkingArea);
                if (overlap.Width >= 20 && overlap.Height >= 20)
                    return true;
            }
            return false;
        }

        private void CenterOnPrimaryScreen()
        {
            var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            Location = new Point(
                wa.Left + (wa.Width - Width) / 2,
                wa.Bottom - Height - 60);
        }

        private void SavePosition()
        {
            _settings.WidgetLeft = Left;
            _settings.WidgetTop = Top;
            try { _settings.Save(); }
            catch { }
        }

        #endregion

        #region Mouse Interaction

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _mouseDownPoint = e.Location;
                _mouseIsDown = true;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_mouseIsDown && e.Button == MouseButtons.Left)
            {
                _mouseIsDown = false;
                int slotIndex = HitTestSlot(e.Location);
                if (slotIndex >= 0 && IsWithinDeadZone(e.Location, _mouseDownPoint))
                {
                    _ = _pasteService.PasteFromSlotAsync(slotIndex);
                }
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int newHovered = HitTestSlot(e.Location);
            if (newHovered != _hoveredSlotIndex)
            {
                _hoveredSlotIndex = newHovered;
                _hoverTimer.Stop();
                _previewPopup.HidePreview();

                if (_hoveredSlotIndex >= 0)
                    _hoverTimer.Start();

                Invalidate();
            }

            if (_mouseIsDown && !IsWithinDeadZone(e.Location, _mouseDownPoint))
            {
                int slotIndex = HitTestSlot(_mouseDownPoint);
                if (slotIndex >= 0)
                {
                    _mouseIsDown = false;
                    StartDragDrop(slotIndex);
                }
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hoveredSlotIndex >= 0)
            {
                _hoveredSlotIndex = -1;
                _hoverTimer.Stop();
                _previewPopup.HidePreview();
                Invalidate();
            }

            if (_settings.WidgetAutoHide)
                _autoHideTimer.Start();

            base.OnMouseLeave(e);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _autoHideTimer.Stop();
            CancelFade();
            base.OnMouseEnter(e);
        }

        private static bool IsWithinDeadZone(Point a, Point b)
        {
            return Math.Abs(a.X - b.X) < DragDeadZone && Math.Abs(a.Y - b.Y) < DragDeadZone;
        }

        #endregion

        #region Drag and Drop

        private void StartDragDrop(int slotIndex)
        {
            var slots = _clipboardManager.Slots;
            if (slotIndex < 0 || slotIndex >= slots.Count)
                return;

            var item = slots[slotIndex];
            var dataObj = new DataObject();

            if (item.ItemType == ClipboardItemType.Text && item.Text != null)
            {
                dataObj.SetData(DataFormats.UnicodeText, item.Text);
            }
            else if (item.ItemType == ClipboardItemType.Image && item.ImageData != null)
            {
                dataObj.SetData(DataFormats.Bitmap, item.ImageData);
            }
            else
            {
                return;
            }

            _clipboardManager.SetSuppressMonitoring(true);
            try
            {
                DoDragDrop(dataObj, DragDropEffects.Copy);
            }
            catch (ExternalException ex)
            {
                Form1.LogError("Drag-and-drop error", ex);
            }
            finally
            {
                _clipboardManager.SetSuppressMonitoring(false);
            }
        }

        #endregion

        #region Hover Preview

        private void HoverTimer_Tick(object? sender, EventArgs e)
        {
            _hoverTimer.Stop();
            var slots = _clipboardManager.Slots;
            if (_hoveredSlotIndex < 0 || _hoveredSlotIndex >= slots.Count)
                return;

            var item = slots[_hoveredSlotIndex];
            int cellSize = _settings.WidgetCompactMode ? CompactCellSize : NormalCellSize;
            var slotRect = GetSlotRect(_hoveredSlotIndex, cellSize, _settings.WidgetVertical);

            var screenRect = RectangleToScreen(slotRect);
            _previewPopup.ShowPreview(item, screenRect);
        }

        #endregion

        #region Auto-Hide / Fade

        private void AutoHideTimer_Tick(object? sender, EventArgs e)
        {
            _autoHideTimer.Stop();

            if (ClientRectangle.Contains(PointToClient(Cursor.Position)))
                return;

            _isFadingOut = true;
            _fadeTimer.Start();
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            double newOpacity = Opacity - 0.05;
            if (newOpacity <= 0.1)
            {
                _fadeTimer.Stop();
                try { Opacity = 0.1; }
                catch { }
                return;
            }
            try { Opacity = newOpacity; }
            catch { }
        }

        private void CancelFade()
        {
            _fadeTimer.Stop();
            _isFadingOut = false;
            try { Opacity = _targetOpacity; }
            catch { }
        }

        #endregion

        #region WndProc / Move

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                var pt = PointToClient(new Point(m.LParam.ToInt32() & 0xFFFF, m.LParam.ToInt32() >> 16));
                bool vertical = _settings.WidgetVertical;

                bool inGrip = vertical ? pt.Y < GripSize : pt.X < GripSize;
                if (inGrip)
                {
                    m.Result = (IntPtr)HTCAPTION;
                    return;
                }
            }

            base.WndProc(ref m);
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            if (Visible)
                SavePosition();
        }

        #endregion

        #region Thumbnail Cache

        private void RebuildThumbnailCache()
        {
            DisposeThumbnailCache();

            var slots = _clipboardManager.Slots;
            _thumbCache = new Image?[slots.Count];
            int cellSize = _settings.WidgetCompactMode ? CompactCellSize : NormalCellSize;
            int thumbSize = cellSize - 4;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].ItemType == ClipboardItemType.Image)
                    _thumbCache[i] = slots[i].CreateThumbnail(thumbSize, thumbSize);
            }
        }

        private void DisposeThumbnailCache()
        {
            foreach (var thumb in _thumbCache)
                thumb?.Dispose();
            _thumbCache = Array.Empty<Image?>();
        }

        #endregion

        #region Refresh Debounce

        private void RefreshDebounceTimer_Tick(object? sender, EventArgs e)
        {
            _refreshDebounceTimer.Stop();
            _refreshPending = false;

            _previewPopup.HidePreview();
            _hoveredSlotIndex = -1;

            RebuildThumbnailCache();
            CalculateAndResize();
            Invalidate();
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hoverTimer.Stop();
                _hoverTimer.Dispose();
                _autoHideTimer.Stop();
                _autoHideTimer.Dispose();
                _fadeTimer.Stop();
                _fadeTimer.Dispose();
                _refreshDebounceTimer.Stop();
                _refreshDebounceTimer.Dispose();

                DisposeThumbnailCache();

                _previewPopup.Dispose();
                _centerFormat.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
