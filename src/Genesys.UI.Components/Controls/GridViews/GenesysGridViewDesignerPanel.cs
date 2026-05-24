using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public class GenesysGridViewDesignerPanel : UserControl
    {
        private const string RowVisible = "Visible";
        private const string RowHeader = "Título";
        private const string RowOrder = "Orden";
        private const string RowWidth = "Ancho";
        private const string RowFormat = "Formato";
        private const string RowDecimals = "Decimales";
        private const string RowAlignment = "Alineación";
        private const string RowSummary = "Summary";
        private const string RowGrouped = "Agrupar";
        private const string RowFrozen = "Congelar";

        private GenesysGridViewManager manager;
        private bool loading;

        private Label titleLabel;
        private DataGridView grid;
        private FlowLayoutPanel buttonsPanel;
        private Button btnApply;
        private Button btnSave;
        private Button btnClose;
        private Button btnReload;

        private readonly List<GenesysGridColumnProfile> profiles =
            new List<GenesysGridColumnProfile>();

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
                profiles.Clear();

                IList<GenesysGridColumnProfile> source =
                    manager.GetColumnProfiles();

                if (source != null)
                    profiles.AddRange(source);

                BuildDesignerGrid();
            }
            finally
            {
                loading = false;
            }
        }

        private void BuildUi()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.White;
            Padding = new Padding(10);
            Font = new Font("Segoe UI", 9F);

            titleLabel = new Label
            {
                Text = "Diseñador de vista",
                Dock = DockStyle.Top,
                Height = 30,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 35, 35),
                TextAlign = ContentAlignment.MiddleLeft
            };

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 30,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(230, 230, 230),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                EditMode = DataGridViewEditMode.EditOnEnter,
                ScrollBars = ScrollBars.Both
            };

            grid.CellFormatting += Grid_CellFormatting;
            grid.CellParsing += Grid_CellParsing;
            grid.DataError += delegate { };
            grid.CellEndEdit += Grid_CellEndEdit;

            buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 38,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = Color.White
            };

            btnClose = CreateButton("Cerrar", 78);
            btnSave = CreateButton("Guardar vista", 105);
            btnApply = CreateButton("Aplicar", 78);
            btnReload = CreateButton("Recargar", 82);

            btnClose.Click += delegate { OnCloseRequested(); };
            btnApply.Click += delegate { ApplyAll(false); };
            btnSave.Click += delegate { ApplyAll(true); };
            btnReload.Click += delegate { ReloadColumns(); };

            buttonsPanel.Controls.Add(btnClose);
            buttonsPanel.Controls.Add(btnSave);
            buttonsPanel.Controls.Add(btnApply);
            buttonsPanel.Controls.Add(btnReload);

            Controls.Add(grid);
            Controls.Add(buttonsPanel);
            Controls.Add(titleLabel);
        }

        private Button CreateButton(string text, int width)
        {
            return new Button
            {
                Text = text,
                Width = width,
                Height = 28,
                FlatStyle = FlatStyle.System,
                Margin = new Padding(6, 0, 0, 0)
            };
        }

        private void BuildDesignerGrid()
        {
            grid.SuspendLayout();

            try
            {
                grid.Columns.Clear();
                grid.Rows.Clear();

                var propertyColumn = new DataGridViewTextBoxColumn
                {
                    Name = "Property",
                    HeaderText = "Propiedad",
                    Width = 110,
                    ReadOnly = true,
                    Frozen = true,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                };

                grid.Columns.Add(propertyColumn);

                foreach (GenesysGridColumnProfile profile in profiles)
                {
                    string header = string.IsNullOrWhiteSpace(profile.HeaderText)
                        ? profile.ColumnName
                        : profile.HeaderText;

                    var column = new DataGridViewTextBoxColumn
                    {
                        Name = profile.ColumnName,
                        HeaderText = header,
                        Tag = profile.ColumnName,
                        Width = 115,
                        SortMode = DataGridViewColumnSortMode.NotSortable
                    };

                    grid.Columns.Add(column);
                }

                AddRow(RowVisible);
                AddRow(RowHeader);
                AddRow(RowOrder);
                AddRow(RowWidth);
                AddRow(RowFormat);
                AddRow(RowDecimals);
                AddRow(RowAlignment);
                AddRow(RowSummary);
                AddRow(RowGrouped);
                AddRow(RowFrozen);

                LoadProfileValues();

                grid.ClearSelection();
            }
            finally
            {
                grid.ResumeLayout(true);
            }
        }

        private void AddRow(string propertyName)
        {
            int rowIndex = grid.Rows.Add();
            DataGridViewRow row = grid.Rows[rowIndex];
            row.Tag = propertyName;
            row.Cells[0].Value = propertyName;
            row.Height = 26;
        }

        private void LoadProfileValues()
        {
            for (int c = 1; c < grid.Columns.Count; c++)
            {
                string columnName = Convert.ToString(grid.Columns[c].Tag);
                GenesysGridColumnProfile profile =
                    profiles.FirstOrDefault(x =>
                        string.Equals(x.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));

                if (profile == null)
                    continue;

                SetValue(RowVisible, c, profile.Visible ? "Sí" : "No");
                SetValue(RowHeader, c, profile.HeaderText);
                SetValue(RowOrder, c, c);
                SetValue(RowWidth, c, SafeInt(profile.Width, 120));
                SetValue(RowFormat, c, ToFormatDisplay(profile.Format));
                SetValue(RowDecimals, c, Math.Max(0, Math.Min(6, profile.Decimals)));
                SetValue(RowAlignment, c, ToAlignmentDisplay(profile.Alignment));
                SetValue(RowSummary, c, ToSummaryDisplay(profile.SummaryType));
                SetValue(RowGrouped, c, profile.Grouped ? "Sí" : "No");
                SetValue(RowFrozen, c, profile.Frozen ? "Sí" : "No");
            }
        }

        private void SetValue(string rowName, int columnIndex, object value)
        {
            DataGridViewRow row = FindRow(rowName);
            if (row == null)
                return;

            row.Cells[columnIndex].Value = value;
        }

        private DataGridViewRow FindRow(string rowName)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (string.Equals(Convert.ToString(row.Tag), rowName, StringComparison.OrdinalIgnoreCase))
                    return row;
            }

            return null;
        }

        private object GetValue(string rowName, int columnIndex)
        {
            DataGridViewRow row = FindRow(rowName);
            if (row == null)
                return null;

            return row.Cells[columnIndex].Value;
        }

        private void ApplyAll(bool save)
        {
            if (manager == null)
                return;

            if (!ValidateOrderValues())
                return;

            for (int c = 1; c < grid.Columns.Count; c++)
            {
                string columnName = Convert.ToString(grid.Columns[c].Tag);

                GenesysGridColumnProfile profile =
                    profiles.FirstOrDefault(x =>
                        string.Equals(x.ColumnName, columnName, StringComparison.OrdinalIgnoreCase));

                if (profile == null)
                    continue;

                int decimals = SafeInt(GetValue(RowDecimals, c), profile.Decimals);
                string formatDisplay = Convert.ToString(GetValue(RowFormat, c));
                string alignmentDisplay = Convert.ToString(GetValue(RowAlignment, c));
                string summaryDisplay = Convert.ToString(GetValue(RowSummary, c));

                var updated = new GenesysGridColumnProfile
                {
                    ColumnName = profile.ColumnName,
                    HeaderText = Convert.ToString(GetValue(RowHeader, c)),
                    Visible = ToBool(GetValue(RowVisible, c)),
                    Width = SafeDouble(GetValue(RowWidth, c), profile.Width),
                    Decimals = decimals,
                    Format = BuildFormat(formatDisplay, decimals),
                    Alignment = FromAlignmentDisplay(alignmentDisplay),
                    SummaryType = FromSummaryDisplay(summaryDisplay),
                    Grouped = ToBool(GetValue(RowGrouped, c)),
                    Frozen = ToBool(GetValue(RowFrozen, c))
                };

                manager.ApplyColumnProfile(updated);
            }

            ApplyColumnOrderFromDesigner();

            if (save)
                manager.SaveCurrentOrAsk();

            ReloadColumns();
        }

        private bool ValidateOrderValues()
        {
            var used = new HashSet<int>();

            for (int c = 1; c < grid.Columns.Count; c++)
            {
                int order = SafeInt(GetValue(RowOrder, c), c);

                if (order <= 0)
                {
                    MessageBox.Show(
                        this,
                        "El orden debe ser mayor que cero.",
                        "Diseñador de vista",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return false;
                }

                if (used.Contains(order))
                {
                    MessageBox.Show(
                        this,
                        "Hay valores de orden duplicados.",
                        "Diseñador de vista",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return false;
                }

                used.Add(order);
            }

            return true;
        }

        private void ApplyColumnOrderFromDesigner()
        {
            // El orden visual real se aplica intercambiando perfiles en el grid
            // mediante DisplayIndex del SfDataGrid al guardar/capturar.
            // Aquí solo reordenamos las columnas del diseñador para reflejarlo.
            var orderedColumns = new List<Tuple<int, DataGridViewColumn>>();

            for (int c = 1; c < grid.Columns.Count; c++)
            {
                int order = SafeInt(GetValue(RowOrder, c), c);
                orderedColumns.Add(new Tuple<int, DataGridViewColumn>(order, grid.Columns[c]));
            }

            orderedColumns.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            int displayIndex = 1;
            foreach (var item in orderedColumns)
            {
                item.Item2.DisplayIndex = displayIndex;
                displayIndex++;
            }
        }

        private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            DataGridViewRow row = grid.Rows[e.RowIndex];
            string propertyName = Convert.ToString(row.Tag);

            if (e.ColumnIndex == 0)
            {
                e.CellStyle.BackColor = Color.FromArgb(245, 247, 250);
                e.CellStyle.ForeColor = Color.FromArgb(60, 60, 60);
                e.CellStyle.Font = new Font(grid.Font, FontStyle.Bold);
                return;
            }

            if (string.Equals(propertyName, RowVisible, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(propertyName, RowGrouped, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(propertyName, RowFrozen, StringComparison.OrdinalIgnoreCase))
            {
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            if (string.Equals(propertyName, RowOrder, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(propertyName, RowWidth, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(propertyName, RowDecimals, StringComparison.OrdinalIgnoreCase))
            {
                e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }

            if (string.Equals(propertyName, RowSummary, StringComparison.OrdinalIgnoreCase))
                e.CellStyle.BackColor = Color.FromArgb(250, 252, 255);

            if (string.Equals(propertyName, RowFormat, StringComparison.OrdinalIgnoreCase))
                e.CellStyle.BackColor = Color.FromArgb(252, 250, 245);
        }

        private void Grid_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex <= 0)
                return;

            string rowName = Convert.ToString(grid.Rows[e.RowIndex].Tag);

            if (IsBooleanRow(rowName))
            {
                e.Value = NormalizeBooleanText(e.Value);
                e.ParsingApplied = true;
            }
        }

        private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (loading)
                return;

            if (e.RowIndex < 0 || e.ColumnIndex <= 0)
                return;

            string rowName = Convert.ToString(grid.Rows[e.RowIndex].Tag);

            if (IsBooleanRow(rowName))
                grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value =
                    NormalizeBooleanText(grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);

            if (string.Equals(rowName, RowAlignment, StringComparison.OrdinalIgnoreCase))
                grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value =
                    NormalizeOneOf(grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value, new[] { "Izquierda", "Centro", "Derecha" }, "Izquierda");

            if (string.Equals(rowName, RowFormat, StringComparison.OrdinalIgnoreCase))
                grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value =
                    NormalizeOneOf(grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value, new[] { "Número", "Moneda", "Porcentaje", "Sin formato" }, "Número");

            if (string.Equals(rowName, RowSummary, StringComparison.OrdinalIgnoreCase))
                grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value =
                    NormalizeOneOf(grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value, new[] { "Ninguno", "Suma", "Promedio", "Conteo", "Mínimo", "Máximo" }, "Ninguno");
        }

        private bool IsBooleanRow(string rowName)
        {
            return string.Equals(rowName, RowVisible, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rowName, RowGrouped, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rowName, RowFrozen, StringComparison.OrdinalIgnoreCase);
        }

        private string NormalizeBooleanText(object value)
        {
            string text = Convert.ToString(value);

            if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "si", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "sí", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase))
            {
                return "Sí";
            }

            return "No";
        }

        private string NormalizeOneOf(object value, string[] allowed, string fallback)
        {
            string text = Convert.ToString(value);

            foreach (string item in allowed)
            {
                if (string.Equals(text, item, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            return fallback;
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

        private bool ToBool(object value)
        {
            string text = Convert.ToString(value);

            return string.Equals(text, "Sí", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "Si", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "True", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "1", StringComparison.OrdinalIgnoreCase);
        }

        private int SafeInt(object value, int defaultValue)
        {
            int result;

            if (int.TryParse(Convert.ToString(value), out result))
                return result;

            return defaultValue;
        }

        private int SafeInt(double value, int defaultValue)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return defaultValue;

            if (value <= 0)
                return defaultValue;

            return Convert.ToInt32(Math.Round(value));
        }

        private double SafeDouble(object value, double defaultValue)
        {
            double result;

            if (double.TryParse(Convert.ToString(value), out result))
                return result;

            return defaultValue;
        }

        private void OnCloseRequested()
        {
            var handler = CloseRequested;

            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}
