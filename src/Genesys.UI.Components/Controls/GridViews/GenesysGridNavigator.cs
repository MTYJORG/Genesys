using Syncfusion.WinForms.DataGrid;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public enum NavigatorAlignment
    {
        Left,
        Center,
        Right
    }

    public class GenesysGridNavigator : UserControl
    {
        private SfDataGrid grid;

        private ToolStrip navigatorStrip;
        private ToolStripButton btnFirst;
        private ToolStripButton btnPrevious;
        private ToolStripButton btnNext;
        private ToolStripButton btnLast;
        private ToolStripTextBox txtPosition;
        private ToolStripTextBox txtTotal;
        private ToolStripLabel lblStatus;
        
        private Panel hostPanel;

        public NavigatorAlignment Alignment { get; set; } = NavigatorAlignment.Center;

        public bool MoveFirstAfterBind { get; set; } = true;
        public bool ShowSelectionStatus { get; set; } = true;

        public GenesysGridNavigator()
        {
            Build();
            UpdateState();
        }

        private void Build()
        {
            Name = "GenesysGridNavigator";
            Dock = DockStyle.Fill;
            Height = 35;
            BackColor = Color.AliceBlue;
            TabStop = false;

            navigatorStrip = new ToolStrip
            {
                AutoSize = true,
                Dock = DockStyle.None,
                GripStyle = ToolStripGripStyle.Hidden,
                LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow,
                RenderMode = ToolStripRenderMode.System,
                BackColor = Color.AliceBlue,
                ForeColor = Color.MidnightBlue,
                Padding = new Padding(4, 3, 4, 0),
                ImageScalingSize = new Size(16, 16),
                TabStop = false
            };

            btnFirst = CreateButton("❮❮", "Primer registro");
            btnPrevious = CreateButton("❮", "Registro anterior");
            btnNext = CreateButton("❯", "Registro siguiente");
            btnLast = CreateButton("❯❯", "Último registro");

            //btnPrevious.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            //btnNext.Font = new Font("Segoe UI", 13F, FontStyle.Bold);

            txtPosition = new ToolStripTextBox
            {
                Name = "txtPosition",
                AutoSize = false,
                Width = 40,
                Text = "0",
                TextBoxTextAlign = HorizontalAlignment.Center,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 1, 0, 0) // <- truco visual
            };

            txtTotal = new ToolStripTextBox
            {
                Name = "txtTotal",
                AutoSize = false,
                Width = 40,
                Text = "/ 0",
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 8F),
                TextBoxTextAlign = HorizontalAlignment.Left,
                BackColor = Parent?.BackColor ?? Color.AliceBlue,
                ForeColor = Color.MidnightBlue
            };
            txtTotal.TextBox.Cursor = Cursors.Default;
            txtTotal.TextBox.TabStop = false;

            //lblStatus = new ToolStripLabel
            //{
            //    Text = "0 registros",
            //    AutoSize = false,
            //    Width = 130,
            //    TextAlign = ContentAlignment.MiddleLeft,
            //    Font = new Font("Segoe UI", 8F),
            //    ForeColor = Color.MidnightBlue
            //};

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
            //navigatorStrip.Items.Add(new ToolStripSeparator());
            //navigatorStrip.Items.Add(lblStatus);

            hostPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.AliceBlue
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
            navigatorStrip.Top =
                (hostPanel.ClientSize.Height - navigatorStrip.Height) / 2;
        }
        private ToolStripButton CreateButton(string text, string tooltip)
        {
            return new ToolStripButton
            {
                Text = text,
                ToolTipText = tooltip,
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                AutoSize = false,
                Width = 28,
                Height = 24,
                Font = new Font("Segoe UI Symbol", 8F),
                ForeColor = Color.MidnightBlue,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 0, 0)
            };
        }

        public GenesysGridNavigator Attach(SfDataGrid sfGrid)
        {
            if (grid != null)
                grid.SelectionChanged -= Grid_SelectionChanged;

            grid = sfGrid;

            if (grid != null)
                grid.SelectionChanged += Grid_SelectionChanged;

            UpdateState();
            return this;
        }

        public GenesysGridNavigator EnableMoveFirstAfterBind(bool enabled = true)
        {
            MoveFirstAfterBind = enabled;
            return this;
        }

        public GenesysGridNavigator EnableSelectionStatus(bool enabled = true)
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

            txtTotal.Text = $" / {count}";

            bool hasRows = count > 0;

            btnFirst.Enabled = hasRows && index > 0;
            btnPrevious.Enabled = hasRows && index > 0;
            btnNext.Enabled = hasRows && index >= 0 && index < count - 1;
            btnLast.Enabled = hasRows && index >= 0 && index < count - 1;
            txtPosition.Enabled = hasRows;

            //lblStatus.Text = BuildStatusText(count);

            RepositionNavigator();
        }

        //private string BuildStatusText(int count)
        //{
        //    if (count == 0)
        //        return "0 registros";

        //    if (ShowSelectionStatus && SelectedCount > 1)
        //        return $"{SelectedCount} seleccionados / {count} registros";

        //    if (count == 1)
        //        return "1 registro";

        //    return $"{count} registros";
        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing && grid != null)
            {
                grid.SelectionChanged -= Grid_SelectionChanged;
                grid = null;
            }

            base.Dispose(disposing);
        }
    }
}