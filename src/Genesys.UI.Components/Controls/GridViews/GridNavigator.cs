using Syncfusion.WinForms.DataGrid;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public enum NavigatorAlignment
    {
        Left,
        Center,
        Right
    }

    [DebuggerStepThrough]
    public class GridNavigator : UserControl
    {
        private enum NavigationIconKind
        {
            First,
            Previous,
            Next,
            Last
        }

        private const int NavigatorHeight = 32;
        private const int ButtonSize = 23;
        private const int IconSize = 14;
        private const int PositionWidth = 42;
        private const int TotalWidth = 38;

        private readonly Color backgroundColor = Color.FromArgb(244, 248, 252);
        private readonly Color stripBackColor = Color.FromArgb(244, 248, 252);
        private readonly Color buttonNormalBackColor = Color.FromArgb(250, 252, 255);
        private readonly Color buttonHoverBackColor = Color.FromArgb(229, 240, 252);
        private readonly Color buttonPressedBackColor = Color.FromArgb(213, 229, 247);
        private readonly Color buttonBorderColor = Color.FromArgb(184, 205, 228);
        private readonly Color iconColor = Color.FromArgb(45, 78, 112);
        private readonly Color textColor = Color.FromArgb(45, 78, 112);

        private SfDataGrid grid;

        private ToolStrip navigatorStrip;
        private ToolStripButton btnFirst;
        private ToolStripButton btnPrevious;
        private ToolStripButton btnNext;
        private ToolStripButton btnLast;
        private ToolStripTextBox txtPosition;
        private ToolStripTextBox txtTotal;
        private Panel hostPanel;

        private Image imgFirst;
        private Image imgPrevious;
        private Image imgNext;
        private Image imgLast;

        public NavigatorAlignment Alignment { get; set; } = NavigatorAlignment.Center;

        public bool MoveFirstAfterBind { get; set; } = true;
        public bool ShowSelectionStatus { get; set; } = true;

        public GridNavigator()
        {
            Build();
            UpdateState();
        }

        private void Build()
        {
            Name = "GridNavigator";
            Dock = DockStyle.Fill;
            Height = NavigatorHeight;
            BackColor = backgroundColor;
            TabStop = false;
            DoubleBuffered = true;

            imgFirst = CreateNavigationImage(NavigationIconKind.First, iconColor, IconSize, IconSize);
            imgPrevious = CreateNavigationImage(NavigationIconKind.Previous, iconColor, IconSize, IconSize);
            imgNext = CreateNavigationImage(NavigationIconKind.Next, iconColor, IconSize, IconSize);
            imgLast = CreateNavigationImage(NavigationIconKind.Last, iconColor, IconSize, IconSize);

            navigatorStrip = new ToolStrip
            {
                AutoSize = true,
                Dock = DockStyle.None,
                GripStyle = ToolStripGripStyle.Hidden,
                LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow,
                RenderMode = ToolStripRenderMode.ManagerRenderMode,
                Renderer = new NavigatorToolStripRenderer(
                    stripBackColor,
                    buttonNormalBackColor,
                    buttonHoverBackColor,
                    buttonPressedBackColor,
                    buttonBorderColor),
                BackColor = stripBackColor,
                ForeColor = textColor,
                Padding = new Padding(4, 3, 4, 2),
                ImageScalingSize = new Size(IconSize, IconSize),
                TabStop = false
            };

            btnFirst = CreateButton(imgFirst, "Primer registro");
            btnPrevious = CreateButton(imgPrevious, "Registro anterior");
            btnNext = CreateButton(imgNext, "Registro siguiente");
            btnLast = CreateButton(imgLast, "Último registro");

            txtPosition = new ToolStripTextBox
            {
                Name = "txtPosition",
                AutoSize = false,
                Width = PositionWidth,
                Text = "0",
                TextBoxTextAlign = HorizontalAlignment.Center,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                BorderStyle = BorderStyle.None,
                Margin = new Padding(0, 0, 0, 0),
                BackColor = Color.White,
                ForeColor = textColor
            };

            txtTotal = new ToolStripTextBox
            {
                Name = "txtTotal",
                AutoSize = false,
                Width = TotalWidth,
                Text = "/ 0",
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                TextBoxTextAlign = HorizontalAlignment.Left,
                BackColor = stripBackColor,
                ForeColor = textColor,
                Margin = new Padding(2, 2, 0, 0)
            };

            txtTotal.TextBox.Cursor = Cursors.Default;
            txtTotal.TextBox.TabStop = false;

            btnFirst.Click += (s, e) => MoveFirst();
            btnPrevious.Click += (s, e) => MovePrevious();
            btnNext.Click += (s, e) => MoveNext();
            btnLast.Click += (s, e) => MoveLast();

            txtPosition.KeyDown += TxtPosition_KeyDown;
            txtPosition.Leave += (s, e) => UpdateState();

            navigatorStrip.Items.Add(btnFirst);
            navigatorStrip.Items.Add(btnPrevious);
            navigatorStrip.Items.Add(txtPosition);
            navigatorStrip.Items.Add(txtTotal);
            navigatorStrip.Items.Add(btnNext);
            navigatorStrip.Items.Add(btnLast);

            hostPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = backgroundColor
            };

            hostPanel.Controls.Add(navigatorStrip);
            Controls.Add(hostPanel);

            hostPanel.Resize += (s, e) => RepositionNavigator();

            RepositionNavigator();
        }

        private void RepositionNavigator()
        {
            if (hostPanel == null || navigatorStrip == null)
                return;

            int x;

            switch (Alignment)
            {
                case NavigatorAlignment.Left:
                    x = 10;
                    break;

                case NavigatorAlignment.Right:
                    x = hostPanel.ClientSize.Width - navigatorStrip.Width - 10;
                    break;

                default:
                    x = (hostPanel.ClientSize.Width - navigatorStrip.Width) / 2;
                    break;
            }

            if (x < 0)
                x = 0;

            navigatorStrip.Left = x;
            navigatorStrip.Top = Math.Max(0, (hostPanel.ClientSize.Height - navigatorStrip.Height) / 2);
        }

        private ToolStripButton CreateButton(Image image, string tooltip)
        {
            return new ToolStripButton
            {
                Image = image,
                ToolTipText = tooltip,
                DisplayStyle = ToolStripItemDisplayStyle.Image,
                AutoSize = false,
                Width = ButtonSize,
                Height = ButtonSize,
                ImageAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.Overlay,
                Margin = new Padding(1, -3, 1, 0),
                Padding = new Padding(0)
            };
        }

        private static Image CreateNavigationImage(
            NavigationIconKind kind,
            Color color,
            int width,
            int height)
        {
            Bitmap bitmap = new Bitmap(width, height);
            bitmap.SetResolution(96F, 96F);

            using (Graphics g = Graphics.FromImage(bitmap))
            using (Brush brush = new SolidBrush(color))
            using (Pen pen = new Pen(color, 1.8F))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                switch (kind)
                {
                    case NavigationIconKind.First:
                        DrawRightTriangle(g, brush, width - 6.5F, 3.5F, width - 13.5F, height / 2F, width - 6.5F, height - 3.5F);
                        DrawBar(g, pen, width - 3.5F, 3.5F, height - 3.5F);
                        break;

                    case NavigationIconKind.Previous:
                        DrawLeftTriangle(g, brush, 5F, height / 2F, 12F, 3.5F, 12F, height - 3.5F);
                        break;

                    case NavigationIconKind.Next:
                        DrawRightTriangle(g, brush, width - 5F, height / 2F, width - 12F, 3.5F, width - 12F, height - 3.5F);
                        break;

                    case NavigationIconKind.Last:
                        DrawBar(g, pen, 3.5F, 3.5F, height - 3.5F);
                        DrawLeftTriangle(g, brush, 6.5F, 3.5F, 13.5F, height / 2F, 6.5F, height - 3.5F);
                        break;
                }
            }

            return bitmap;
        }

        private static void DrawBar(Graphics g, Pen pen, float x, float top, float bottom)
        {
            g.DrawLine(pen, x, top, x, bottom);
        }

        private static void DrawLeftTriangle(
            Graphics g,
            Brush brush,
            float tipX,
            float tipY,
            float rightTopX,
            float rightTopY,
            float rightBottomX,
            float rightBottomY)
        {
            PointF[] points =
            {
                new PointF(tipX, tipY),
                new PointF(rightTopX, rightTopY),
                new PointF(rightBottomX, rightBottomY)
            };

            g.FillPolygon(brush, points);
        }

        private static void DrawRightTriangle(
            Graphics g,
            Brush brush,
            float tipX,
            float tipY,
            float leftTopX,
            float leftTopY,
            float leftBottomX,
            float leftBottomY)
        {
            PointF[] points =
            {
                new PointF(tipX, tipY),
                new PointF(leftTopX, leftTopY),
                new PointF(leftBottomX, leftBottomY)
            };

            g.FillPolygon(brush, points);
        }

        public GridNavigator Attach(SfDataGrid sfGrid)
        {
            if (grid != null)
                grid.SelectionChanged -= Grid_SelectionChanged;

            grid = sfGrid;

            if (grid != null)
                grid.SelectionChanged += Grid_SelectionChanged;

            UpdateState();
            return this;
        }

        public GridNavigator EnableMoveFirstAfterBind(bool enabled = true)
        {
            MoveFirstAfterBind = enabled;
            return this;
        }

        public GridNavigator EnableSelectionStatus(bool enabled = true)
        {
            ShowSelectionStatus = enabled;
            UpdateState();
            return this;
        }

        public void NotifyDataBound()
        {
            UpdateState();

            if (MoveFirstAfterBind && RowCount > 0)
                MoveFirst();
        }

        private void Grid_SelectionChanged(object sender, EventArgs e)
        {
            UpdateState();
        }

        private void TxtPosition_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.SuppressKeyPress = true;

            int position;

            if (!int.TryParse(txtPosition.Text, out position))
            {
                UpdateState();
                return;
            }

            MoveTo(position - 1);
        }

        private int RowCount
        {
            get { return grid?.View?.Records?.Count ?? 0; }
        }

        private int SelectedCount
        {
            get { return grid?.SelectedItems?.Count ?? 0; }
        }

        private int CurrentIndex
        {
            get
            {
                if (grid?.SelectedItem == null || grid.View?.Records == null)
                    return -1;

                return grid.View.Records.IndexOfRecord(grid.SelectedItem);
            }
        }

        public void MoveFirst()
        {
            MoveTo(0);
        }

        public void MovePrevious()
        {
            MoveTo(CurrentIndex - 1);
        }

        public void MoveNext()
        {
            MoveTo(CurrentIndex + 1);
        }

        public void MoveLast()
        {
            MoveTo(RowCount - 1);
        }

        public void MoveTo(int index)
        {
            int count = RowCount;

            if (grid == null || count == 0)
                return;

            index = Math.Max(0, Math.Min(index, count - 1));

            var record = grid.View.Records[index].Data;

            grid.SelectedItem = record;
            grid.CurrentItem = record;
            grid.SelectedIndex = index;
            grid.Focus();

            UpdateState();
        }

        public void UpdateState()
        {
            int count = RowCount;
            int index = CurrentIndex;

            txtPosition.Text = count == 0 || index < 0
                ? "0"
                : (index + 1).ToString();

            txtTotal.Text = "/ " + count;

            bool hasRows = count > 0;

            btnFirst.Enabled = hasRows && index > 0;
            btnPrevious.Enabled = hasRows && index > 0;
            btnNext.Enabled = hasRows && index >= 0 && index < count - 1;
            btnLast.Enabled = hasRows && index >= 0 && index < count - 1;
            txtPosition.Enabled = hasRows;

            RepositionNavigator();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (grid != null)
                {
                    grid.SelectionChanged -= Grid_SelectionChanged;
                    grid = null;
                }

                DisposeImage(ref imgFirst);
                DisposeImage(ref imgPrevious);
                DisposeImage(ref imgNext);
                DisposeImage(ref imgLast);
            }

            base.Dispose(disposing);
        }

        private static void DisposeImage(ref Image image)
        {
            if (image != null)
            {
                image.Dispose();
                image = null;
            }
        }

        private sealed class NavigatorToolStripRenderer : ToolStripProfessionalRenderer
        {
            private readonly Color backgroundColor;
            private readonly Color buttonNormalBackColor;
            private readonly Color buttonHoverBackColor;
            private readonly Color buttonPressedBackColor;
            private readonly Color buttonBorderColor;

            public NavigatorToolStripRenderer(
                Color backgroundColor,
                Color buttonNormalBackColor,
                Color buttonHoverBackColor,
                Color buttonPressedBackColor,
                Color buttonBorderColor)
                : base(new ProfessionalColorTable())
            {
                this.backgroundColor = backgroundColor;
                this.buttonNormalBackColor = buttonNormalBackColor;
                this.buttonHoverBackColor = buttonHoverBackColor;
                this.buttonPressedBackColor = buttonPressedBackColor;
                this.buttonBorderColor = buttonBorderColor;
                RoundedEdges = true;
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using (Brush brush = new SolidBrush(backgroundColor))
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }

            protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
            {
                ToolStripButton button = e.Item as ToolStripButton;
                if (button == null)
                {
                    base.OnRenderButtonBackground(e);
                    return;
                }

                Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
                bounds.Inflate(-1, -2);

                Color fill = buttonNormalBackColor;
                Color border = buttonBorderColor;

                if (!button.Enabled)
                {
                    fill = Color.FromArgb(246, 248, 250);
                    border = Color.FromArgb(222, 228, 235);
                }
                else if (button.Pressed)
                {
                    fill = buttonPressedBackColor;
                }
                else if (button.Selected)
                {
                    fill = buttonHoverBackColor;
                }

                using (GraphicsPath path = RoundedRectangle(bounds, 4))
                using (Brush brush = new SolidBrush(fill))
                using (Pen pen = new Pen(border))
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                // Sin separadores visibles; el layout compacto ya da estructura.
            }

            private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
            {
                int diameter = radius * 2;
                GraphicsPath path = new GraphicsPath();

                if (diameter <= 0)
                {
                    path.AddRectangle(bounds);
                    path.CloseFigure();
                    return path;
                }

                Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

                path.AddArc(arc, 180, 90);
                arc.X = bounds.Right - diameter;
                path.AddArc(arc, 270, 90);
                arc.Y = bounds.Bottom - diameter;
                path.AddArc(arc, 0, 90);
                arc.X = bounds.Left;
                path.AddArc(arc, 90, 90);
                path.CloseFigure();

                return path;
            }
        }
    }
}
