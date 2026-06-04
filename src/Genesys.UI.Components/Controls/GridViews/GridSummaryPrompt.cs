using Syncfusion.WinForms.DataGrid;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public class GridSummaryPrompt : Form
    {
        private readonly ComboBox cboColumn;
        private readonly ComboBox cboSummaryType;
        private readonly ComboBox cboFormat;
        private readonly Button btnOk;
        private readonly Button btnCancel;

        public string ColumnName { get; private set; }
        public string SummaryTypeName { get; private set; }
        public string NumericFormat { get; private set; }

        public GridSummaryPrompt(SfDataGrid grid)
        {
            Text = "Summary row";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            Width = 390;
            Height = 245;
            Font = new Font("Segoe UI", 9F);

            var lblColumn = new Label
            {
                Text = "Columna:",
                Left = 18,
                Top = 22,
                Width = 90,
                AutoSize = false
            };

            cboColumn = new ComboBox
            {
                Left = 115,
                Top = 18,
                Width = 230,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblSummaryType = new Label
            {
                Text = "Resumen:",
                Left = 18,
                Top = 62,
                Width = 90,
                AutoSize = false
            };

            cboSummaryType = new ComboBox
            {
                Left = 115,
                Top = 58,
                Width = 230,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblFormat = new Label
            {
                Text = "Formato:",
                Left = 18,
                Top = 102,
                Width = 90,
                AutoSize = false
            };

            cboFormat = new ComboBox
            {
                Left = 115,
                Top = 98,
                Width = 230,
                DropDownStyle = ComboBoxStyle.DropDown
            };

            btnOk = new Button
            {
                Text = "Aceptar",
                Left = 170,
                Top = 150,
                Width = 82,
                DialogResult = DialogResult.OK
            };

            btnCancel = new Button
            {
                Text = "Cancelar",
                Left = 263,
                Top = 150,
                Width = 82,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(lblColumn);
            Controls.Add(cboColumn);
            Controls.Add(lblSummaryType);
            Controls.Add(cboSummaryType);
            Controls.Add(lblFormat);
            Controls.Add(cboFormat);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            LoadColumns(grid);
            LoadSummaryTypes();
            LoadFormats();
            ApplyDefaultFormatFromSelectedColumn();

            cboColumn.SelectedIndexChanged += delegate { ApplyDefaultFormatFromSelectedColumn(); };
            cboSummaryType.SelectedIndexChanged += delegate { ApplyDefaultFormatFromSelectedColumn(); };

            btnOk.Click += delegate
            {
                if (cboColumn.SelectedItem == null)
                {
                    MessageBox.Show(this, "Selecciona una columna.", "Summary row", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    DialogResult = DialogResult.None;
                    return;
                }

                ColumnName = ((ColumnItem)cboColumn.SelectedItem).MappingName;

                var selectedSummaryType = cboSummaryType.SelectedItem as SummaryTypeItem;
                SummaryTypeName = selectedSummaryType == null ? "Sum" : selectedSummaryType.Value;

                if (string.Equals(SummaryTypeName, "Count", StringComparison.OrdinalIgnoreCase))
                    NumericFormat = string.IsNullOrWhiteSpace(cboFormat.Text) ? "N0" : cboFormat.Text.Trim();
                else
                    NumericFormat = string.IsNullOrWhiteSpace(cboFormat.Text) ? "N2" : cboFormat.Text.Trim();
            };
        }

        private void LoadColumns(SfDataGrid grid)
        {
            cboColumn.Items.Clear();

            if (grid == null)
                return;

            foreach (GridColumn column in grid.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.MappingName))
                    continue;

                cboColumn.Items.Add(new ColumnItem
                {
                    MappingName = column.MappingName,
                    HeaderText = string.IsNullOrWhiteSpace(column.HeaderText) ? column.MappingName : column.HeaderText,
                    Format = column.Format ?? string.Empty
                });
            }

            if (cboColumn.Items.Count > 0)
                cboColumn.SelectedIndex = 0;
        }

        private void LoadSummaryTypes()
        {
            cboSummaryType.DisplayMember = "Text";
            cboSummaryType.ValueMember = "Value";
            cboSummaryType.Items.Add(new SummaryTypeItem("Suma", "Sum"));
            cboSummaryType.Items.Add(new SummaryTypeItem("Promedio", "Average"));
            cboSummaryType.Items.Add(new SummaryTypeItem("Conteo", "Count"));
            cboSummaryType.Items.Add(new SummaryTypeItem("Mínimo", "Minimum"));
            cboSummaryType.Items.Add(new SummaryTypeItem("Máximo", "Maximum"));
            cboSummaryType.SelectedIndex = 0;
        }

        private void LoadFormats()
        {
            cboFormat.Items.Add("N0");
            cboFormat.Items.Add("N2");
            cboFormat.Items.Add("N3");
            cboFormat.Items.Add("C2");
        }

        private void ApplyDefaultFormatFromSelectedColumn()
        {
            var selectedColumn = cboColumn.SelectedItem as ColumnItem;
            var selectedSummaryType = cboSummaryType.SelectedItem as SummaryTypeItem;
            string summaryType = selectedSummaryType == null ? "Sum" : selectedSummaryType.Value;

            if (string.Equals(summaryType, "Count", StringComparison.OrdinalIgnoreCase))
            {
                cboFormat.Text = "N0";
                return;
            }

            if (selectedColumn != null && !string.IsNullOrWhiteSpace(selectedColumn.Format))
                cboFormat.Text = selectedColumn.Format;
            else
                cboFormat.Text = "N2";
        }

        private class ColumnItem
        {
            public string MappingName { get; set; }
            public string HeaderText { get; set; }
            public string Format { get; set; }

            public override string ToString()
            {
                return HeaderText;
            }
        }

        private class SummaryTypeItem
        {
            public string Text { get; private set; }
            public string Value { get; private set; }

            public SummaryTypeItem(string text, string value)
            {
                Text = text;
                Value = value;
            }
        }
    }
}
