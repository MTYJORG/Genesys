using Syncfusion.WinForms.DataGrid;
using Syncfusion.WinForms.DataGrid.Events;
using Syncfusion.WinForms.DataGrid.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Genesys.UI.Components.Controls.GridViews;
using Genesys.UI.Components.Controls.Toolbar;
using Genesys.UI.Components.Forms;

namespace Genesys.UI.Components.Controls.GridViews.Vistas
{
    public class VistasDesignerForm : GenesysSimpleForm
    {
        private const string RowVisible = "Visible";
        private const string RowHeader = "Título";
        private const string RowWidth = "Ancho";
        private const string RowFormat = "Formato";
        private const string RowDecimals = "Decimales";
        private const string RowAlignment = "Alineación";
        private const string RowSummary = "Summary";
        private const string RowGrouped = "Agrupar";
        private const string RowFrozen = "Congelar";
        private const string RowSort = "Ordenar";
        private const string RowFilter = "Filtro";
        private const string DisplayAutomatic = "Automatico";
        private const string DisplayNotApplicable = "N/A";

        private readonly VistasAdministrador manager;

        private ComboBox cboViews;

        private SfDataGrid designGrid;
        private DataTable designTable;

        private bool loading;
        private string loadedDesignerSignature;
        private bool appliedChangesNotSaved;

        private readonly List<GenesysGridColumnProfile> profiles =
            new List<GenesysGridColumnProfile>();

        public VistasDesignerForm(
            VistasAdministrador manager)
        {
            this.manager = manager;

            BuildUi();
            LoadViews();
            ReloadDesign();
        }

        private void BuildUi()
        {
            Text = "Diseñador de vista";
            MinimizeBox = false;
            MaximizeBox = true;
            ShowIcon = false;
            Size = new Size(1150, 540);
            MinimumSize = new Size(920, 430);

            if (ContentPanel != null)
                ContentPanel.Padding = new Padding(10);

            AddToolbarButton(
                BotonTipo.Buscar,
                "Aplicar",
                "Aplicar cambios sin guardar la vista",
                delegate { ApplyAll(false); });

            AddToolbarButton(
                BotonTipo.Guardar,
                "Guardar",
                "Guardar cambios en la vista activa",
                delegate { ApplyAll(true); });

            AddToolbarButton(
                BotonTipo.Respaldar,
                "Guardar como",
                "Guardar como una nueva vista",
                delegate { BtnSaveAs_Click(this, EventArgs.Empty); });

            AddToolbarButton(
                BotonTipo.Nuevo,
                "Nueva vista",
                "Crear una nueva vista a partir del diseño actual",
                delegate { BtnNewView_Click(this, EventArgs.Empty); });

            AddToolbarButton(
                BotonTipo.Refrescar,
                "Releer diseño",
                "Descarta cambios no aplicados del diseñador y vuelve a leer la vista activa desde el grid",
                delegate { ReloadDesign(); });

            var topPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.White,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48F));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 286F));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            topPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblView = new Label
            {
                Text = "Vista:",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.DimGray,
                Margin = new Padding(0)
            };

            cboViews = new ComboBox
            {
                Width = 280,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0)
            };

            cboViews.SelectedIndexChanged += CboViews_SelectedIndexChanged;

            var hint = new Label
            {
                Text = "Tip: arrastra encabezados para ordenar columnas. Click en Sí/No alterna; click en Formato/Alineación/Summary muestra opciones.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.Gray,
                Padding = new Padding(10, 0, 0, 0),
                Margin = new Padding(0)
            };

            topPanel.Controls.Add(lblView, 0, 0);
            topPanel.Controls.Add(cboViews, 1, 0);
            topPanel.Controls.Add(hint, 2, 0);

            designGrid = new SfDataGrid
            {
                Dock = DockStyle.Fill,
                AllowEditing = true,
                AllowDraggingColumns = true,
                AllowResizingColumns = true,
                AllowSorting = false,
                AllowFiltering = false,
                ShowGroupDropArea = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = AutoSizeColumnsMode.None,
                SelectionMode = GridSelectionMode.Single,
                NavigationMode = NavigationMode.Cell,
                BackColor = Color.White,
            };

            designGrid.CellClick += DesignGrid_CellClick;

            if (ContentPanel != null)
            {
                ContentPanel.Controls.Add(designGrid);
                ContentPanel.Controls.Add(topPanel);
            }
        }

        private void ApplyColumnHeaderStyle(GridColumn column)
        {
            if (column == null || column.HeaderStyle == null)
                return;

            SetPropertyIfExists(column.HeaderStyle, "BackColor", Color.FromArgb(242, 246, 252));
            SetPropertyIfExists(column.HeaderStyle, "TextColor", Color.MidnightBlue);
            SetPropertyIfExists(column.HeaderStyle, "ForeColor", Color.MidnightBlue);
            SetPropertyIfExists(column.HeaderStyle, "Font", new Font("Segoe UI", 9F, FontStyle.Bold));
        }

        private void SetPropertyIfExists(object instance, string propertyName, object value)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
                return;

            try
            {
                PropertyInfo property = instance.GetType().GetProperty(propertyName);

                if (property == null || !property.CanWrite)
                    return;

                property.SetValue(instance, value, null);
            }
            catch
            {
            }
        }

        private void LoadViews()
        {
            if (manager == null)
                return;

            loading = true;

            try
            {
                cboViews.Items.Clear();

                IList<string> views = manager.GetAvailableViewNames();

                if (views != null)
                {
                    foreach (string viewName in views)
                    {
                        if (!string.IsNullOrWhiteSpace(viewName))
                            cboViews.Items.Add(viewName);
                    }
                }

                string currentView =
                    manager.CurrentViewName;

                for (int i = 0; i < cboViews.Items.Count; i++)
                {
                    if (string.Equals(
                        Convert.ToString(cboViews.Items[i]),
                        currentView,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        cboViews.SelectedIndex = i;
                        return;
                    }
                }

                if (cboViews.Items.Count > 0)
                    cboViews.SelectedIndex = 0;
            }
            finally
            {
                loading = false;
            }
        }

        private void CboViews_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            if (loading ||
                manager == null ||
                cboViews.SelectedItem == null)
            {
                return;
            }

            string viewName =
                Convert.ToString(cboViews.SelectedItem);

            if (string.IsNullOrWhiteSpace(viewName))
                return;

            SelectViewAndReloadDesigner(viewName);
        }

        private void SelectViewAndReloadDesigner(string viewName)
        {
            if (manager == null || string.IsNullOrWhiteSpace(viewName))
                return;

            loading = true;

            try
            {
                manager.ApplyViewByName(viewName);
            }
            finally
            {
                loading = false;
            }

            // En especial para Predeterminada o vistas que aplican filtros superiores,
            // el manager puede provocar un refresh/bind diferido. Releer el diseñador
            // en el siguiente ciclo visual evita mostrar valores viejos del grid.
            BeginInvoke(new Action(delegate
            {
                LoadViews();
                ReloadDesign();
            }));
        }

        private void BtnNewView_Click(
            object sender,
            EventArgs e)
        {
            if (manager == null)
                return;

            if (manager.SaveAsNewViewFromDesigner())
            {
                LoadViews();
                ReloadDesign();
            }
        }

        private void BtnSaveAs_Click(
            object sender,
            EventArgs e)
        {
            ApplyAll(false);

            if (manager != null &&
                manager.SaveAsNewViewFromDesigner())
            {
                appliedChangesNotSaved = false;
                LoadViews();
                ReloadDesign();
            }
        }

        private void ReloadDesign()
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

                BuildDesignTable();
                BindDesignGrid();
            }
            finally
            {
                loading = false;
                UpdateLoadedDesignerSignature();
            }
        }

        private void BuildDesignTable()
        {
            designTable = new DataTable();

            designTable.Columns.Add("Property", typeof(string));

            foreach (GenesysGridColumnProfile profile in profiles)
            {
                if (profile == null ||
                    string.IsNullOrWhiteSpace(profile.ColumnName))
                {
                    continue;
                }

                if (!designTable.Columns.Contains(profile.ColumnName))
                    designTable.Columns.Add(profile.ColumnName, typeof(string));
            }

            AddPropertyRow(RowVisible);
            AddPropertyRow(RowHeader);
            AddPropertyRow(RowWidth);
            AddPropertyRow(RowFormat);
            AddPropertyRow(RowDecimals);
            AddPropertyRow(RowAlignment);
            AddPropertyRow(RowSummary);
            AddPropertyRow(RowGrouped);
            AddPropertyRow(RowFrozen);
            AddPropertyRow(RowSort);
            AddPropertyRow(RowFilter);

            foreach (GenesysGridColumnProfile profile in profiles)
            {
                string columnName = profile.ColumnName;

                SetValue(RowVisible, columnName, profile.Visible ? "Sí" : "No");
                SetValue(RowHeader, columnName, profile.HeaderText);
                SetValue(RowWidth, columnName, GetWidthDisplay(profile, GetColumnKind(columnName)));
                ColumnKind kind = GetColumnKind(columnName);

                SetValue(RowFormat, columnName, ToFormatDisplayByKind(profile.Format, kind));
                SetValue(RowDecimals, columnName, GetDecimalsDisplay(profile, kind));
                SetValue(RowAlignment, columnName, ToAlignmentDisplayByKind(profile.Alignment, kind));
                SetValue(RowSummary, columnName, ToSummaryDisplayByKind(profile.SummaryType, kind));
                SetValue(RowGrouped, columnName, profile.Grouped ? "Sí" : "No");
                SetValue(RowFrozen, columnName, profile.Frozen ? "Sí" : "No");
                SetValue(RowSort, columnName, ToSortDisplay(GetManagerColumnSortDirection(columnName)));
                SetValue(RowFilter, columnName, GetManagerColumnFilterValue(columnName));
            }
        }

        private void BindDesignGrid()
        {
            designGrid.SuspendLayout();

            try
            {
                designGrid.Columns.Clear();

                var propertyColumn =
                    new GridTextColumn
                    {
                        MappingName = "Property",
                        HeaderText = "Propiedad",
                        Width = 120,
                        AllowEditing = false,
                        AllowDragging = false,
                        AllowResizing = true
                    };

                propertyColumn.HeaderStyle.HorizontalAlignment =
                    HorizontalAlignment.Center;

                ApplyColumnHeaderStyle(propertyColumn);

                propertyColumn.CellStyle.BackColor =
                    Color.FromArgb(245, 247, 250);

                designGrid.Columns.Add(propertyColumn);

                foreach (GenesysGridColumnProfile profile in profiles)
                {
                    string header =
                        string.IsNullOrWhiteSpace(profile.HeaderText)
                            ? profile.ColumnName
                            : profile.HeaderText;

                    var column =
                        new GridTextColumn
                        {
                            MappingName = profile.ColumnName,
                            HeaderText = header,
                            Width = 125,
                            AllowEditing = true,
                            AllowDragging = true,
                            AllowResizing = true
                        };

                    column.HeaderStyle.HorizontalAlignment =
                        HorizontalAlignment.Center;

                    ApplyColumnHeaderStyle(column);

                    designGrid.Columns.Add(column);
                }

                SetFrozenColumnCount(1);

                designGrid.DataSource = designTable;
                designGrid.Refresh();
            }
            finally
            {
                designGrid.ResumeLayout(true);
            }
        }

        private void AddPropertyRow(
            string rowName)
        {
            System.Data.DataRow row =
                designTable.NewRow();

            row["Property"] = rowName;

            designTable.Rows.Add(row);
        }

        private System.Data.DataRow FindPropertyRow(
            string rowName)
        {
            if (designTable == null)
                return null;

            foreach (System.Data.DataRow row in designTable.Rows)
            {
                if (string.Equals(
                    Convert.ToString(row["Property"]),
                    rowName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return row;
                }
            }

            return null;
        }

        private void SetValue(
            string rowName,
            string columnName,
            string value)
        {
            System.Data.DataRow row =
                FindPropertyRow(rowName);

            if (row == null ||
                string.IsNullOrWhiteSpace(columnName) ||
                !designTable.Columns.Contains(columnName))
            {
                return;
            }

            row[columnName] = value ?? string.Empty;
        }

        private string GetValue(
            string rowName,
            string columnName)
        {
            System.Data.DataRow row =
                FindPropertyRow(rowName);

            if (row == null ||
                string.IsNullOrWhiteSpace(columnName) ||
                !designTable.Columns.Contains(columnName))
            {
                return string.Empty;
            }

            return Convert.ToString(row[columnName]);
        }

        private void DesignGrid_CellClick(
            object sender,
            CellClickEventArgs e)
        {
            if (loading ||
                e == null ||
                e.DataColumn == null ||
                e.DataColumn.GridColumn == null ||
                e.DataColumn.GridColumn.MappingName == "Property")
            {
                return;
            }

            System.Data.DataRow row =
                GetRowFromRowData(e.DataRow == null
                    ? null
                    : e.DataRow.RowData);

            if (row == null)
                return;

            string rowName =
                Convert.ToString(row["Property"]);

            string columnName =
                e.DataColumn.GridColumn.MappingName;

            if (IsBooleanRow(rowName))
            {
                ToggleBoolean(rowName, columnName);
                return;
            }

            if (string.Equals(rowName, RowSort, StringComparison.OrdinalIgnoreCase))
            {
                ShowOptionsMenu(rowName, columnName, "Vacío", "Ascendente", "Descendente");
                return;
            }

            ColumnKind kind =
                GetColumnKind(columnName);

            if (string.Equals(rowName, RowFormat, StringComparison.OrdinalIgnoreCase))
            {
                ShowFormatOptionsMenu(rowName, columnName, kind);
                return;
            }

            if (string.Equals(rowName, RowDecimals, StringComparison.OrdinalIgnoreCase))
            {
                ShowDecimalsOptionsMenu(columnName, kind);
                return;
            }

            if (string.Equals(rowName, RowAlignment, StringComparison.OrdinalIgnoreCase))
            {
                ShowOptionsMenu(rowName, columnName, GetAlignmentOptions(kind));
                return;
            }

            if (string.Equals(rowName, RowSummary, StringComparison.OrdinalIgnoreCase))
            {
                ShowOptionsMenu(rowName, columnName, GetSummaryOptions(kind));
                return;
            }
        }

        private System.Data.DataRow GetRowFromRowData(
            object rowData)
        {
            if (rowData == null)
                return null;

            DataRowView view =
                rowData as DataRowView;

            if (view != null)
                return view.Row;

            return rowData as System.Data.DataRow;
        }

        private void ToggleBoolean(
            string rowName,
            string columnName)
        {
            string current =
                GetValue(rowName, columnName);

            SetValue(
                rowName,
                columnName,
                ToBool(current) ? "No" : "Sí");

            RefreshDesignGridView();
        }

        private void ShowFormatOptionsMenu(
            string rowName,
            string columnName,
            ColumnKind kind)
        {
            ShowOptionsMenu(
                rowName,
                columnName,
                delegate(string selected)
                {
                    SetValue(rowName, columnName, selected);

                    if (kind == ColumnKind.Numeric)
                    {
                        if (IsAutomaticDisplay(selected))
                        {
                            SetValue(RowDecimals, columnName, DisplayAutomatic);
                        }
                        else if (IsIdentifierDisplay(selected))
                        {
                            // Identificador se guarda por ahora como F0: sin separador de miles
                            // y sin decimales. La fila Decimales queda en 0 para dejar
                            // explícito el formato resultante.
                            // Si en el futuro se permite F1/F2/F3, este es el punto
                            // donde debe conservarse el valor elegido por el usuario.
                            SetValue(RowDecimals, columnName, "0");
                        }
                        else
                        {
                            string currentDecimals = GetValue(RowDecimals, columnName);

                            if (string.IsNullOrWhiteSpace(currentDecimals) ||
                                string.Equals(currentDecimals, DisplayNotApplicable, StringComparison.OrdinalIgnoreCase))
                            {
                                SetValue(RowDecimals, columnName, DisplayAutomatic);
                            }
                        }
                    }

                    RefreshDesignGridView();
                },
                GetFormatOptions(kind));
        }

        private void ShowDecimalsOptionsMenu(
            string columnName,
            ColumnKind kind)
        {
            if (kind != ColumnKind.Numeric)
                return;

            string formatDisplay =
                GetValue(RowFormat, columnName);

            // Si el formato es Automatico (Format == ""), los decimales
            // pertenecen al renderer de Syncfusion y no se editan desde la vista.
            //
            // Identificador se guarda por ahora como F0. Se deja bloqueado
            // para evitar configuraciones inconsistentes en folios/IDs.
            // Preparado para futuro: si se desea soportar F1/F2/F3, quitar
            // IsIdentifierDisplay(formatDisplay) de esta condición y BuildFormat
            // ya puede tomar el valor de decimales.
            if (IsAutomaticDisplay(formatDisplay) || IsIdentifierDisplay(formatDisplay))
                return;

            ShowOptionsMenu(
                RowDecimals,
                columnName,
                delegate(string selected)
                {
                    SetValue(RowDecimals, columnName, selected);
                    RefreshDesignGridView();
                },
                GetDecimalsOptions());
        }

        private void ShowOptionsMenu(
            string rowName,
            string columnName,
            params string[] options)
        {
            ShowOptionsMenu(rowName, columnName, null, options);
        }

        private void ShowOptionsMenu(
            string rowName,
            string columnName,
            Action<string> selectedHandler,
            params string[] options)
        {
            if (string.IsNullOrWhiteSpace(rowName) ||
                string.IsNullOrWhiteSpace(columnName) ||
                options == null ||
                options.Length == 0)
            {
                return;
            }

            var menu =
                new ContextMenuStrip
                {
                    ShowImageMargin = false
                };

            string current =
                GetValue(rowName, columnName);

            foreach (string option in options)
            {
                var item =
                    new ToolStripMenuItem(option)
                    {
                        Checked =
                            string.Equals(
                                option,
                                current,
                                StringComparison.OrdinalIgnoreCase)
                    };

                item.Click += delegate
                {
                    if (selectedHandler != null)
                    {
                        selectedHandler(option);
                    }
                    else
                    {
                        SetValue(rowName, columnName, option);
                        RefreshDesignGridView();
                    }
                };

                menu.Items.Add(item);
            }

            // No se dispone manualmente aquí.
            // En algunos flujos de click/doble click, WinForms/Syncfusion
            // todavía intenta consultar el menú después de cerrarlo y dispara
            // ObjectDisposedException si lo desechamos en Closed.
            menu.Show(this, PointToClient(Cursor.Position));
        }

        private void ApplyAll(
            bool save)
        {
            if (manager == null)
                return;

            CommitDesignerEdits();

            foreach (GridColumn designerColumn in GetDesignerColumnsInVisualOrder())
            {
                string columnName =
                    designerColumn.MappingName;

                if (string.IsNullOrWhiteSpace(columnName) ||
                    string.Equals(columnName, "Property", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                GenesysGridColumnProfile original =
                    profiles.FirstOrDefault(x =>
                        string.Equals(
                            x.ColumnName,
                            columnName,
                            StringComparison.OrdinalIgnoreCase));

                if (original == null)
                    continue;

                ColumnKind kind =
                    GetColumnKind(columnName);

                int decimals =
                    GetDesignerDecimals(
                        GetValue(RowDecimals, columnName));

                var updated =
                    new GenesysGridColumnProfile
                    {
                        ColumnName = original.ColumnName,
                        HeaderText = GetValue(RowHeader, columnName),
                        Visible = ToBool(GetValue(RowVisible, columnName)),
                        Width = SafeDouble(GetValue(RowWidth, columnName), original.Width),
                        Decimals = decimals,
                        Format = ResolveDesignerFormat(original, GetValue(RowFormat, columnName), decimals, kind),
                        Alignment = FromAlignmentDisplay(GetValue(RowAlignment, columnName)),
                        SummaryType = FromSummaryDisplay(GetValue(RowSummary, columnName)),
                        Grouped = ToBool(GetValue(RowGrouped, columnName)),
                        Frozen = ToBool(GetValue(RowFrozen, columnName))
                    };

                manager.ApplyColumnProfile(updated);
                manager.SetColumnSortDirection(columnName, FromSortDisplay(GetValue(RowSort, columnName)));
                manager.SetColumnFilterValue(columnName, GetValue(RowFilter, columnName));
            }

            ApplyRealGridColumnOrder();

            if (save)
            {
                bool saved = manager.SaveCurrentOrAsk();

                if (saved)
                {
                    appliedChangesNotSaved = false;
                    LoadViews();
                }
            }
            else
            {
                appliedChangesNotSaved = true;
            }

            ReloadDesign();
        }

        private IEnumerable<GridColumn> GetDesignerColumnsInVisualOrder()
        {
            if (designGrid == null || designGrid.Columns == null)
                return Enumerable.Empty<GridColumn>();

            return designGrid.Columns
                .Cast<GridColumn>()
                .OrderBy(x => x.GetType().GetProperty("DisplayIndex") == null
                    ? designGrid.Columns.IndexOf(x)
                    : Convert.ToInt32(x.GetType().GetProperty("DisplayIndex").GetValue(x, null)))
                .ToList();
        }

        private void ApplyRealGridColumnOrder()
        {
            if (manager == null)
                return;

            List<string> orderedColumnNames =
                GetDesignerColumnsInVisualOrder()
                    .Select(x => x.MappingName)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x) &&
                        !string.Equals(x, "Property", StringComparison.OrdinalIgnoreCase))
                    .ToList();

            manager.ReorderColumnsByMappingNames(orderedColumnNames);
        }

        private void UpdateLoadedDesignerSignature()
        {
            loadedDesignerSignature = BuildDesignerSignature();
        }

        private bool HasUnappliedDesignerChanges()
        {
            string currentSignature = BuildDesignerSignature();

            return !string.Equals(
                currentSignature,
                loadedDesignerSignature ?? string.Empty,
                StringComparison.Ordinal);
        }

        private string BuildDesignerSignature()
        {
            if (designTable == null || designGrid == null)
                return string.Empty;

            var parts = new List<string>();

            foreach (GridColumn designerColumn in GetDesignerColumnsInVisualOrder())
            {
                if (designerColumn == null)
                    continue;

                string columnName = designerColumn.MappingName;

                if (string.IsNullOrWhiteSpace(columnName) ||
                    string.Equals(columnName, "Property", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                parts.Add("COLUMN=" + EscapeSignatureValue(columnName));

                foreach (System.Data.DataRow row in designTable.Rows)
                {
                    if (row == null)
                        continue;

                    string rowName = Convert.ToString(row["Property"]);

                    if (string.IsNullOrWhiteSpace(rowName))
                        continue;

                    string value = designTable.Columns.Contains(columnName)
                        ? Convert.ToString(row[columnName])
                        : string.Empty;

                    parts.Add(
                        EscapeSignatureValue(rowName) +
                        "=" +
                        EscapeSignatureValue(value));
                }
            }

            return string.Join("|", parts.ToArray());
        }

        private string EscapeSignatureValue(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("|", "\\|")
                .Replace("=", "\\=");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CommitDesignerEdits();

            if (HasUnappliedDesignerChanges())
            {
                DialogResult result = MessageBox.Show(
                    this,
                    "Hay cambios en el diseñador que no han sido aplicados.\n\n¿Deseas aplicarlos y guardarlos antes de cerrar?",
                    "Diseñador de vistas",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == DialogResult.Yes)
                {
                    ApplyAll(true);

                    if (HasUnappliedDesignerChanges() || appliedChangesNotSaved)
                    {
                        e.Cancel = true;
                        return;
                    }

                    base.OnFormClosing(e);
                    return;
                }

                // No: descarta únicamente lo editado en el diseñador que no fue aplicado.
                // Si antes ya existían cambios aplicados al grid pero no guardados,
                // se preguntará a continuación si deben guardarse.
            }

            if (appliedChangesNotSaved)
            {
                DialogResult result = MessageBox.Show(
                    this,
                    "Los cambios fueron aplicados al grid, pero no guardados en la vista.\n\n¿Deseas guardar la vista antes de cerrar?",
                    "Diseñador de vistas",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == DialogResult.Yes)
                {
                    bool saved = manager != null && manager.SaveCurrentOrAsk();

                    if (!saved)
                    {
                        e.Cancel = true;
                        return;
                    }

                    appliedChangesNotSaved = false;
                }
            }

            base.OnFormClosing(e);
        }

        private void CommitDesignerEdits()
        {
            try
            {
                Validate();
            }
            catch
            {
            }

            try
            {
                if (designGrid != null && designGrid.CurrentCell != null)
                    InvokeNoArgumentMethod(designGrid.CurrentCell, "EndEdit");
            }
            catch
            {
            }

            try
            {
                InvokeNoArgumentMethod(designGrid, "EndEdit");
            }
            catch
            {
            }

            try
            {
                InvokeNoArgumentMethod(designGrid, "CommitEdit");
            }
            catch
            {
            }

            try
            {
                BindingContext[designTable].EndCurrentEdit();
            }
            catch
            {
            }

            try
            {
                if (designTable != null)
                    designTable.AcceptChanges();
            }
            catch
            {
            }
        }

        private void InvokeNoArgumentMethod(
            object target,
            string methodName)
        {
            if (target == null ||
                string.IsNullOrWhiteSpace(methodName))
            {
                return;
            }

            try
            {
                var method =
                    target.GetType().GetMethod(
                        methodName,
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public,
                        null,
                        Type.EmptyTypes,
                        null);

                if (method != null)
                    method.Invoke(target, null);
            }
            catch
            {
            }
        }

        private void RefreshDesignGridView()
        {
            try
            {
                if (designGrid.View != null)
                    designGrid.View.Refresh();
            }
            catch
            {
            }

            try
            {
                designGrid.Refresh();
                designGrid.Invalidate();
            }
            catch
            {
            }
        }

        private bool IsBooleanRow(
            string rowName)
        {
            return string.Equals(rowName, RowVisible, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rowName, RowGrouped, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(rowName, RowFrozen, StringComparison.OrdinalIgnoreCase);
        }

        private enum ColumnKind
        {
            Text,
            Numeric,
            Date,
            Boolean,
            Unknown
        }

        private ColumnKind GetColumnKind(
            string columnName)
        {
            Type type =
                GetColumnDataType(columnName);

            if (type == null)
                return ColumnKind.Unknown;

            type =
                Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(byte) ||
                type == typeof(short) ||
                type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(float) ||
                type == typeof(double) ||
                type == typeof(decimal))
            {
                return ColumnKind.Numeric;
            }

            if (type == typeof(DateTime))
                return ColumnKind.Date;

            if (type == typeof(bool))
                return ColumnKind.Boolean;

            if (type == typeof(string) ||
                type == typeof(char))
            {
                return ColumnKind.Text;
            }

            return ColumnKind.Unknown;
        }

        private Type GetColumnDataType(
            string columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName) ||
                manager == null)
            {
                return null;
            }

            Type type =
                InvokeManagerColumnTypeMethod("GetColumnDataType", columnName);

            if (type != null)
                return type;

            if (InvokeManagerBooleanMethod("IsNumericColumn", columnName))
                return typeof(decimal);

            if (InvokeManagerBooleanMethod("IsDateColumn", columnName))
                return typeof(DateTime);

            if (InvokeManagerBooleanMethod("IsBooleanColumn", columnName))
                return typeof(bool);

            return null;
        }

        private Type InvokeManagerColumnTypeMethod(
            string methodName,
            string columnName)
        {
            try
            {
                MethodInfo method =
                    manager.GetType().GetMethod(
                        methodName,
                        BindingFlags.Instance |
                        BindingFlags.Public);

                if (method == null)
                    return null;

                object value =
                    method.Invoke(
                        manager,
                        new object[] { columnName });

                return value as Type;
            }
            catch
            {
                return null;
            }
        }

        private bool InvokeManagerBooleanMethod(
            string methodName,
            string columnName)
        {
            try
            {
                MethodInfo method =
                    manager.GetType().GetMethod(
                        methodName,
                        BindingFlags.Instance |
                        BindingFlags.Public);

                if (method == null)
                    return false;

                object value =
                    method.Invoke(
                        manager,
                        new object[] { columnName });

                if (value is bool)
                    return (bool)value;

                return false;
            }
            catch
            {
                return false;
            }
        }

        private string[] GetFormatOptions(
            ColumnKind kind)
        {
            if (kind == ColumnKind.Numeric)
                return new[] { DisplayAutomatic, "Identificador", "Número", "Moneda", "Porcentaje" };

            if (kind == ColumnKind.Date)
                return new[] { DisplayNotApplicable, "Fecha corta", "Fecha larga", "Fecha y hora" };

            return new[] { DisplayNotApplicable };
        }

        private string[] GetAlignmentOptions(
            ColumnKind kind)
        {
            if (kind == ColumnKind.Numeric)
                return new[] { "Derecha", "Izquierda", "Centro" };

            if (kind == ColumnKind.Date ||
                kind == ColumnKind.Boolean)
            {
                return new[] { "Centro", "Izquierda", "Derecha" };
            }

            return new[] { "Izquierda", "Centro", "Derecha" };
        }

        private string[] GetSummaryOptions(
            ColumnKind kind)
        {
            if (kind == ColumnKind.Numeric)
                return new[] { "Ninguno", "Suma", "Promedio", "Conteo", "Mínimo", "Máximo" };

            return new[] { "Ninguno", "Conteo" };
        }

        private string GetManagerColumnSortDirection(string columnName)
        {
            try
            {
                if (manager == null)
                    return "None";

                return manager.GetColumnSortDirection(columnName);
            }
            catch
            {
                return "None";
            }
        }

        private string GetManagerColumnFilterValue(string columnName)
        {
            try
            {
                if (manager == null)
                    return string.Empty;

                return manager.GetColumnFilterValue(columnName);
            }
            catch
            {
                return string.Empty;
            }
        }

        private string ToSortDisplay(string direction)
        {
            if (string.Equals(direction, "Ascending", StringComparison.OrdinalIgnoreCase))
                return "Ascendente";

            if (string.Equals(direction, "Descending", StringComparison.OrdinalIgnoreCase))
                return "Descendente";

            return "Vacío";
        }

        private string FromSortDisplay(string display)
        {
            if (string.Equals(display, "Ascendente", StringComparison.OrdinalIgnoreCase))
                return "Ascending";

            if (string.Equals(display, "Descendente", StringComparison.OrdinalIgnoreCase))
                return "Descending";

            return "None";
        }

        private string GetDecimalsDisplay(
            GenesysGridColumnProfile profile,
            ColumnKind kind)
        {
            if (kind != ColumnKind.Numeric)
                return DisplayNotApplicable;

            if (profile == null || string.IsNullOrWhiteSpace(profile.Format))
                return DisplayAutomatic;

            int precision;
            if (TryGetNumericPrecision(profile.Format, out precision))
                return Math.Max(0, Math.Min(4, precision)).ToString();

            return DisplayAutomatic;
        }

        private string[] GetDecimalsOptions()
        {
            return new[] { DisplayAutomatic, "0", "1", "2", "3", "4" };
        }

        private string GetWidthDisplay(
            GenesysGridColumnProfile profile,
            ColumnKind kind)
        {
            int current =
                SafeInt(profile.Width, 0);

            if (current > 0)
                return current.ToString();

            // No inventar un ancho sugerido al cargar el diseñador.
            // Si la vista no trae ancho, se deja vacío para no sobrescribir
            // el ancho real al aplicar/guardar cambios no relacionados.
            return string.Empty;
        }

        private int GetSuggestedWidth(
            ColumnKind kind)
        {
            if (kind == ColumnKind.Numeric)
                return 110;

            if (kind == ColumnKind.Date)
                return 115;

            if (kind == ColumnKind.Boolean)
                return 80;

            return 160;
        }

        private string ToFormatDisplayByKind(
            string format,
            ColumnKind kind)
        {
            if (kind == ColumnKind.Date)
            {
                if (string.IsNullOrWhiteSpace(format))
                    return DisplayNotApplicable;

                if (string.Equals(format, "d", StringComparison.OrdinalIgnoreCase))
                    return "Fecha corta";

                if (string.Equals(format, "D", StringComparison.OrdinalIgnoreCase))
                    return "Fecha larga";

                if (string.Equals(format, "g", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(format, "G", StringComparison.OrdinalIgnoreCase))
                {
                    return "Fecha y hora";
                }

                return DisplayNotApplicable;
            }

            if (kind == ColumnKind.Numeric)
            {
                if (string.IsNullOrWhiteSpace(format))
                    return DisplayAutomatic;

                return ToFormatDisplay(format);
            }

            return DisplayNotApplicable;
        }

        private string ToAlignmentDisplayByKind(
            string alignment,
            ColumnKind kind)
        {
            string display = ToAlignmentDisplay(alignment);

            // Si el campo es numérico y no trae una alineación explícita útil,
            // el diseñador debe sugerir Derecha como default.
            // En muchos grids el default técnico llega como Left aunque el usuario
            // nunca lo haya elegido, por eso se normaliza aquí.
            if (kind == ColumnKind.Numeric &&
                (string.IsNullOrWhiteSpace(alignment) ||
                 string.Equals(display, "Izquierda", StringComparison.OrdinalIgnoreCase)))
            {
                return "Derecha";
            }

            if (string.IsNullOrWhiteSpace(alignment))
            {
                if (kind == ColumnKind.Date ||
                    kind == ColumnKind.Boolean)
                {
                    return "Centro";
                }

                return "Izquierda";
            }

            return display;
        }

        private string ToSummaryDisplayByKind(
            string summary,
            ColumnKind kind)
        {
            if (kind == ColumnKind.Numeric)
                return ToSummaryDisplay(summary);

            if (string.Equals(summary, "Count", StringComparison.OrdinalIgnoreCase))
                return "Conteo";

            return "Ninguno";
        }


        private string ResolveDesignerFormat(
            GenesysGridColumnProfile original,
            string display,
            int decimals,
            ColumnKind kind)
        {
            if (kind == ColumnKind.Numeric)
            {
                // Format == "" significa Automatico. En este modo los decimales
                // son visuales de Syncfusion y no forman parte de la vista.
                if (IsAutomaticDisplay(display))
                    return string.Empty;

                return BuildFormatByKind(display, decimals, kind);
            }

            if (original == null)
                return BuildFormatByKind(display, decimals, kind);

            string originalFormat = original.Format ?? string.Empty;
            string originalDisplay = ToFormatDisplayByKind(originalFormat, kind);
            bool formatChanged =
                !string.Equals(
                    NormalizeDesignerText(display),
                    NormalizeDesignerText(originalDisplay),
                    StringComparison.OrdinalIgnoreCase);

            if (!formatChanged)
                return originalFormat;

            return BuildFormatByKind(display, decimals, kind);
        }

        private string NormalizeDesignerText(
            string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim();
        }

        private string BuildFormatByKind(
            string display,
            int decimals,
            ColumnKind kind)
        {
            if (kind == ColumnKind.Date)
            {
                if (string.Equals(display, "Fecha corta", StringComparison.OrdinalIgnoreCase))
                    return "d";

                if (string.Equals(display, "Fecha larga", StringComparison.OrdinalIgnoreCase))
                    return "D";

                if (string.Equals(display, "Fecha y hora", StringComparison.OrdinalIgnoreCase))
                    return "g";

                return string.Empty;
            }

            if (kind == ColumnKind.Numeric)
                return BuildFormat(display, decimals);

            return string.Empty;
        }

        private string BuildFormat(
            string display,
            int decimals)
        {
            if (IsAutomaticDisplay(display))
                return string.Empty;

            if (IsIdentifierDisplay(display))
            {
                // Identificador usa la familia F porque NO incluye separador de miles.
                // Por ahora se fuerza F0. Si más adelante se habilitan decimales para
                // identificadores, reemplazar el 0 por la precisión elegida:
                // return "F" + Math.Max(0, Math.Min(4, decimals));
                return "F0";
            }

            string prefix = "N";

            if (string.Equals(display, "Moneda", StringComparison.OrdinalIgnoreCase))
                prefix = "C";
            else if (string.Equals(display, "Porcentaje", StringComparison.OrdinalIgnoreCase))
                prefix = "P";

            if (decimals < 0)
                return prefix;

            return prefix + Math.Max(0, Math.Min(4, decimals));
        }

        private string ToFormatDisplay(
            string format)
        {
            if (string.IsNullOrWhiteSpace(format))
                return DisplayAutomatic;

            if (format.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                return "Moneda";

            if (format.StartsWith("P", StringComparison.OrdinalIgnoreCase))
                return "Porcentaje";

            if (format.StartsWith("F", StringComparison.OrdinalIgnoreCase))
                return "Identificador";

            return "Número";
        }

        private bool IsAutomaticDisplay(
            string display)
        {
            return string.IsNullOrWhiteSpace(display) ||
                   string.Equals(display, DisplayAutomatic, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(display, "Automático", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsIdentifierDisplay(
            string display)
        {
            return string.Equals(display, "Identificador", StringComparison.OrdinalIgnoreCase);
        }

        private int GetDesignerDecimals(
            string display)
        {
            if (string.IsNullOrWhiteSpace(display) ||
                string.Equals(display, DisplayAutomatic, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(display, "Automático", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(display, DisplayNotApplicable, StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }

            int value;
            if (!int.TryParse(display, out value))
                return -1;

            return Math.Max(0, Math.Min(4, value));
        }

        private bool TryGetNumericPrecision(
            string format,
            out int precision)
        {
            precision = -1;

            if (string.IsNullOrWhiteSpace(format))
                return false;

            string trimmed = format.Trim();

            if (trimmed.Length < 2)
                return false;

            char first = char.ToUpperInvariant(trimmed[0]);
            if (first != 'N' && first != 'C' && first != 'P' && first != 'F')
                return false;

            int parsed;
            if (!int.TryParse(trimmed.Substring(1), out parsed))
                return false;

            precision = parsed;
            return true;
        }

        private string ToAlignmentDisplay(
            string alignment)
        {
            if (string.Equals(alignment, "Center", StringComparison.OrdinalIgnoreCase))
                return "Centro";

            if (string.Equals(alignment, "Right", StringComparison.OrdinalIgnoreCase))
                return "Derecha";

            return "Izquierda";
        }

        private string FromAlignmentDisplay(
            string display)
        {
            if (string.Equals(display, "Centro", StringComparison.OrdinalIgnoreCase))
                return "Center";

            if (string.Equals(display, "Derecha", StringComparison.OrdinalIgnoreCase))
                return "Right";

            return "Left";
        }

        private string ToSummaryDisplay(
            string summary)
        {
            if (string.Equals(summary, "Sum", StringComparison.OrdinalIgnoreCase)) return "Suma";
            if (string.Equals(summary, "Average", StringComparison.OrdinalIgnoreCase)) return "Promedio";
            if (string.Equals(summary, "Count", StringComparison.OrdinalIgnoreCase)) return "Conteo";
            if (string.Equals(summary, "Minimum", StringComparison.OrdinalIgnoreCase)) return "Mínimo";
            if (string.Equals(summary, "Maximum", StringComparison.OrdinalIgnoreCase)) return "Máximo";

            return "Ninguno";
        }

        private string FromSummaryDisplay(
            string display)
        {
            if (string.Equals(display, "Suma", StringComparison.OrdinalIgnoreCase)) return "Sum";
            if (string.Equals(display, "Promedio", StringComparison.OrdinalIgnoreCase)) return "Average";
            if (string.Equals(display, "Conteo", StringComparison.OrdinalIgnoreCase)) return "Count";
            if (string.Equals(display, "Mínimo", StringComparison.OrdinalIgnoreCase)) return "Minimum";
            if (string.Equals(display, "Máximo", StringComparison.OrdinalIgnoreCase)) return "Maximum";

            return "None";
        }

        private bool ToBool(
            object value)
        {
            string text =
                Convert.ToString(value);

            return string.Equals(text, "Sí", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "Si", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "True", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "1", StringComparison.OrdinalIgnoreCase);
        }

        private int SafeInt(
            object value,
            int defaultValue)
        {
            int result;

            if (int.TryParse(Convert.ToString(value), out result))
                return result;

            return defaultValue;
        }

        private int SafeInt(
            double value,
            int defaultValue)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value) ||
                value <= 0)
            {
                return defaultValue;
            }

            return Convert.ToInt32(Math.Round(value));
        }

        private double SafeDouble(
            object value,
            double defaultValue)
        {
            double result;

            if (double.TryParse(Convert.ToString(value), out result))
                return result;

            return defaultValue;
        }

        private void SetFrozenColumnCount(
            int count)
        {
            try
            {
                var property =
                    designGrid.GetType().GetProperty("FrozenColumnCount");

                if (property != null && property.CanWrite)
                    property.SetValue(designGrid, count, null);
            }
            catch
            {
            }
        }
    }

}
