using Syncfusion.WinForms.DataGrid;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.GridViews
{
    public class GenesysGridSummaryPrompt : Form
    {
        private readonly ComboBox cboColumn;
        private readonly ComboBox cboSummaryType;
        private readonly ComboBox cboFormat;
        private readonly Button btnOk;
        private readonly Button btnCancel;

        public string ColumnName { get; private set; }
        public string SummaryTypeName { get; private set; }
        public string NumericFormat { get; private set; }

        public GenesysGridSummaryPrompt(SfDataGrid grid, IDictionary<string, string> numericFormats)
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

            LoadColumns(grid, numericFormats);
            LoadSummaryTypes();
            LoadFormats();

            cboColumn.SelectedIndexChanged += delegate { ApplyDefaultFormat(numericFormats); };

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

                if (string.Equals(SummaryTypeName, "Count", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(cboFormat.Text))
                    NumericFormat = "N0";
                else
                    NumericFormat = string.IsNullOrWhiteSpace(cboFormat.Text) ? "N2" : cboFormat.Text.Trim();
            };
        }

        private void LoadColumns(SfDataGrid grid, IDictionary<string, string> numericFormats)
        {
            cboColumn.Items.Clear();

            if (grid == null)
                return;

            foreach (GridColumn column in grid.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.MappingName))
                    continue;

                // Se incluyen todas las columnas para permitir Count.
                // Sum/Average/Min/Max funcionan correctamente cuando la columna es numérica.
                cboColumn.Items.Add(new ColumnItem
                {
                    MappingName = column.MappingName,
                    HeaderText = string.IsNullOrWhiteSpace(column.HeaderText) ? column.MappingName : column.HeaderText
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
            cboFormat.Text = "N2";
        }

        private void ApplyDefaultFormat(IDictionary<string, string> numericFormats)
        {
            if (numericFormats == null || cboColumn.SelectedItem == null)
                return;

            string mappingName = ((ColumnItem)cboColumn.SelectedItem).MappingName;
            string format;

            if (numericFormats.TryGetValue(mappingName, out format) && !string.IsNullOrWhiteSpace(format))
                cboFormat.Text = format;
        }

        private class ColumnItem
        {
            public string MappingName { get; set; }
            public string HeaderText { get; set; }

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
