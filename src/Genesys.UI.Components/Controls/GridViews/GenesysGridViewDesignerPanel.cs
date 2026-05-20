using Syncfusion.WinForms.DataGrid;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public class GenesysGridViewDesignerPanel : UserControl
    {
        private GenesysGridViewManager manager;
        private bool loading;

        private ComboBox cboColumn;
        private CheckBox chkVisible;
        private TextBox txtHeader;
        private ComboBox cboDecimals;
        private ComboBox cboFormat;
        private ComboBox cboAlignment;
        private ComboBox cboSummary;
        private CheckBox chkGrouped;
        private CheckBox chkFrozen;
        private NumericUpDown nudWidth;
        private Button btnApply;
        private Button btnSave;
        private Button btnClose;

        public event EventHandler CloseRequested;

        public GenesysGridViewDesignerPanel()
        {
            BuildUi();
        }

        public void Attach(GenesysGridViewManager manager)
        {
            this.manager = manager;
            ReloadColumns();
        }

        public void ReloadColumns()
        {
            if (manager == null)
                return;

            loading = true;
            try
            {
                cboColumn.Items.Clear();

                foreach (var profile in manager.GetColumnProfiles())
                {
                    cboColumn.Items.Add(new ColumnItem(profile.ColumnName, profile.HeaderText));
                }

                if (cboColumn.Items.Count > 0 && cboColumn.SelectedIndex < 0)
                    cboColumn.SelectedIndex = 0;
            }
            finally
            {
                loading = false;
            }

            LoadSelectedColumn();
        }

        private void BuildUi()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            Padding = new Padding(12);
            Font = new Font("Segoe UI", 9F);

            var title = new Label
            {
                Text = "Diseñador de vista",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 35, 35)
            };

            btnClose = new Button
            {
                Text = "×",
                Width = 28,
                Height = 24,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(Width - 40, 8)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += delegate { OnCloseRequested(); };

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 11,
                Padding = new Padding(0, 8, 0, 0)
            };

            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            cboColumn = NewCombo();
            chkVisible = new CheckBox { Checked = true, Dock = DockStyle.Left };
            txtHeader = new TextBox { Dock = DockStyle.Fill };
            cboDecimals = NewCombo();
            cboFormat = NewCombo();
            cboAlignment = NewCombo();
            cboSummary = NewCombo();
            chkGrouped = new CheckBox { Dock = DockStyle.Left };
            chkFrozen = new CheckBox { Dock = DockStyle.Left };
            nudWidth = new NumericUpDown { Dock = DockStyle.Left, Minimum = 20, Maximum = 2000, Width = 90 };

            cboDecimals.Items.AddRange(new object[] { "0", "1", "2", "3", "4", "5", "6" });
            cboFormat.Items.AddRange(new object[] { "Número", "Moneda", "Porcentaje", "Sin formato" });
            cboAlignment.Items.AddRange(new object[] { "Izquierda", "Centro", "Derecha" });
            cboSummary.Items.AddRange(new object[] { "Ninguno", "Suma", "Promedio", "Conteo", "Mínimo", "Máximo" });

            cboColumn.SelectedIndexChanged += delegate { if (!loading) LoadSelectedColumn(); };

            AddRow(panel, 0, "Columna", cboColumn);
            AddRow(panel, 1, "Visible", chkVisible);
            AddRow(panel, 2, "Título", txtHeader);
            AddRow(panel, 3, "Decimales", cboDecimals);
            AddRow(panel, 4, "Formato", cboFormat);
            AddRow(panel, 5, "Alineación", cboAlignment);
            AddRow(panel, 6, "Summary", cboSummary);
            AddRow(panel, 7, "Agrupar", chkGrouped);
            AddRow(panel, 8, "Congelar", chkFrozen);
            AddRow(panel, 9, "Ancho", nudWidth);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 38
            };

            btnSave = new Button { Text = "Guardar vista", Width = 100, Height = 28 };
            btnApply = new Button { Text = "Aplicar", Width = 75, Height = 28 };
            var btnCloseBottom = new Button { Text = "Cerrar", Width = 75, Height = 28 };

            btnApply.Click += delegate { ApplyProfile(false); };
            btnSave.Click += delegate { ApplyProfile(true); };
            btnCloseBottom.Click += delegate { OnCloseRequested(); };

            buttons.Controls.Add(btnCloseBottom);
            buttons.Controls.Add(btnSave);
            buttons.Controls.Add(btnApply);

            panel.Controls.Add(buttons, 0, 10);
            panel.SetColumnSpan(buttons, 2);

            Controls.Add(panel);
            Controls.Add(title);
            Controls.Add(btnClose);

            cboDecimals.SelectedItem = "2";
            cboFormat.SelectedItem = "Número";
            cboAlignment.SelectedItem = "Derecha";
            cboSummary.SelectedItem = "Ninguno";
        }

        private ComboBox NewCombo()
        {
            return new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        private void AddRow(TableLayoutPanel panel, int row, string labelText, Control editor)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

            var label = new Label
            {
                Text = labelText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            panel.Controls.Add(label, 0, row);
            panel.Controls.Add(editor, 1, row);
        }

        private void LoadSelectedColumn()
        {
            if (manager == null || cboColumn.SelectedItem == null)
                return;

            var item = (ColumnItem)cboColumn.SelectedItem;
            var profile = manager.GetColumnProfile(item.ColumnName);

            if (profile == null)
                return;

            loading = true;
            try
            {
                chkVisible.Checked = profile.Visible;
                txtHeader.Text = profile.HeaderText;
                cboDecimals.SelectedItem = Math.Max(0, Math.Min(6, profile.Decimals)).ToString();
                cboFormat.SelectedItem = ToFormatDisplay(profile.Format);
                cboAlignment.SelectedItem = ToAlignmentDisplay(profile.Alignment);
                cboSummary.SelectedItem = ToSummaryDisplay(profile.SummaryType);
                chkGrouped.Checked = profile.Grouped;
                chkFrozen.Checked = profile.Frozen;
                nudWidth.Value = SafeDecimalForNumericUpDown(profile.Width, nudWidth.Minimum, nudWidth.Maximum, 120M);
            }
            finally
            {
                loading = false;
            }
        }

        private void ApplyProfile(bool save)
        {
            if (manager == null || cboColumn.SelectedItem == null)
                return;

            var item = (ColumnItem)cboColumn.SelectedItem;
            int decimals = 2;
            int.TryParse(Convert.ToString(cboDecimals.SelectedItem), out decimals);

            var profile = new GenesysGridColumnProfile
            {
                ColumnName = item.ColumnName,
                HeaderText = txtHeader.Text,
                Visible = chkVisible.Checked,
                Width = Convert.ToDouble(nudWidth.Value),
                Decimals = decimals,
                Format = BuildFormat(Convert.ToString(cboFormat.SelectedItem), decimals),
                Alignment = FromAlignmentDisplay(Convert.ToString(cboAlignment.SelectedItem)),
                SummaryType = FromSummaryDisplay(Convert.ToString(cboSummary.SelectedItem)),
                Grouped = chkGrouped.Checked,
                Frozen = chkFrozen.Checked
            };

            manager.ApplyColumnProfile(profile);

            if (save)
                manager.SaveCurrentOrAsk();
        }

        private string BuildFormat(string display, int decimals)
        {
            if (string.Equals(display, "Moneda", StringComparison.OrdinalIgnoreCase))
                return "C" + decimals;

            if (string.Equals(display, "Porcentaje", StringComparison.OrdinalIgnoreCase))
                return "P" + decimals;

            if (string.Equals(display, "Sin formato", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return "N" + decimals;
        }

        private string ToFormatDisplay(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
                return "Sin formato";

            if (format.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                return "Moneda";

            if (format.StartsWith("P", StringComparison.OrdinalIgnoreCase))
                return "Porcentaje";

            return "Número";
        }

        private string ToAlignmentDisplay(string alignment)
        {
            if (string.Equals(alignment, "Center", StringComparison.OrdinalIgnoreCase))
                return "Centro";

            if (string.Equals(alignment, "Right", StringComparison.OrdinalIgnoreCase))
                return "Derecha";

            return "Izquierda";
        }

        private string FromAlignmentDisplay(string display)
        {
            if (string.Equals(display, "Centro", StringComparison.OrdinalIgnoreCase))
                return "Center";

            if (string.Equals(display, "Derecha", StringComparison.OrdinalIgnoreCase))
                return "Right";

            return "Left";
        }


        private decimal SafeDecimalForNumericUpDown(double value, decimal minimum, decimal maximum, decimal defaultValue)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return ClampDecimal(defaultValue, minimum, maximum);

            if (value <= 0)
                return ClampDecimal(defaultValue, minimum, maximum);

            if (value > (double)maximum)
                return maximum;

            if (value < (double)minimum)
                return minimum;

            try
            {
                return ClampDecimal(Convert.ToDecimal(value), minimum, maximum);
            }
            catch (OverflowException)
            {
                return ClampDecimal(defaultValue, minimum, maximum);
            }
        }

        private decimal ClampDecimal(decimal value, decimal minimum, decimal maximum)
        {
            if (value < minimum)
                return minimum;

            if (value > maximum)
                return maximum;

            return value;
        }

        private string ToSummaryDisplay(string summary)
        {
            if (string.Equals(summary, "Sum", StringComparison.OrdinalIgnoreCase)) return "Suma";
            if (string.Equals(summary, "Average", StringComparison.OrdinalIgnoreCase)) return "Promedio";
            if (string.Equals(summary, "Count", StringComparison.OrdinalIgnoreCase)) return "Conteo";
            if (string.Equals(summary, "Minimum", StringComparison.OrdinalIgnoreCase)) return "Mínimo";
            if (string.Equals(summary, "Maximum", StringComparison.OrdinalIgnoreCase)) return "Máximo";
            return "Ninguno";
        }

        private string FromSummaryDisplay(string display)
        {
            if (string.Equals(display, "Suma", StringComparison.OrdinalIgnoreCase)) return "Sum";
            if (string.Equals(display, "Promedio", StringComparison.OrdinalIgnoreCase)) return "Average";
            if (string.Equals(display, "Conteo", StringComparison.OrdinalIgnoreCase)) return "Count";
            if (string.Equals(display, "Mínimo", StringComparison.OrdinalIgnoreCase)) return "Minimum";
            if (string.Equals(display, "Máximo", StringComparison.OrdinalIgnoreCase)) return "Maximum";
            return "None";
        }

        private void OnCloseRequested()
        {
            var handler = CloseRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private class ColumnItem
        {
            public string ColumnName { get; private set; }
            private readonly string headerText;

            public ColumnItem(string columnName, string headerText)
            {
                ColumnName = columnName;
                this.headerText = headerText;
            }

            public override string ToString()
            {
                if (string.IsNullOrWhiteSpace(headerText))
                    return ColumnName;

                return headerText;
            }
        }
    }
}
