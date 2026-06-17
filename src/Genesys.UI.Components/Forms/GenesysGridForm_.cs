using Genesys.UI.Components.Controls.Filters;
using Genesys.UI.Components.Controls.GridViews;
using Genesys.UI.Components.Controls.GridViews.Vistas;
using Genesys.UI.Components.Controls.Messages;
using Genesys.UI.Components.Controls.Toolbar;
using Genesys.UI.Components.Forms.Services;
using Genesys.UI.Components.Visual;
using Syncfusion.WinForms.Controls;
using Syncfusion.WinForms.DataGrid;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Action = System.Action;

namespace Genesys.UI.Components.Forms
{
    public class GenesysGridForm : SfForm
    {
        // ─── Constantes ───────────────────────────────────────────────────────
        private const int ButtonsPanelHeight = 46;
        private const int FiltersPanelHeight = 65;
        private const int MessagesPanelHeight = 49;
        private const int InfoPanelHeight = 26;
        private const int ViewInfoPanelHeight = 32;
        private const int ToolbarConfigPanelWidth = 44;
        private const float ViewDesignerWidth = 320F;
        private const int DetailGridHeight = 200;
        private const int WmSetRedraw = 0x000B;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        // ─── Campos ───────────────────────────────────────────────────────────
        private readonly GenesysGridFilterPersistence filterPersistence;
        private readonly GenesysGridFormOptions options;
        private VistasController vistasController;
        private bool filtersRestored;
        private bool disposed;
        private GenesysGridFilterResult pendingFilterResult;
        private bool filterResultBindScheduled;

        private GenesysGridExportService exportService;
        private bool initialLoadStarted;
        private bool initialLoadCompletedRaised;
        private bool detailModeEnabled;
        private DataTable detailTable;
        private DataView detailView;
        
        // ─── Propiedades públicas ─────────────────────────────────────────────
        public Panel ButtonsPanel { get; private set; }
        public Panel FiltersPanel { get; private set; }
        public Panel MessagesPanel { get; private set; }
        public Panel GridPanel { get; private set; }
        public TableLayoutPanel GridWorkspacePanel { get; private set; }
        public Panel InfoPanel { get; private set; }
        public Panel ViewInfoPanel { get; private set; }
        public Label CurrentViewLabel { get; private set; }
        public VistasSelector ViewSelectorBar { get; private set; }
        public Panel ToolbarHostPanel { get; private set; }
        public Panel ToolbarConfigPanel { get; private set; }
        public Panel ViewDesignerHostPanel { get; private set; }
        public VistasDesignerPanel ViewDesigner { get; private set; }
        public GenesysToolbar Toolbar { get; private set; }
        public GenesysMessages Messages { get; private set; }
        public GenesysGridFiltersPanel Filters { get; private set; }
        public SfDataGrid Grid { get; private set; }
        public Panel DetailGridPanel { get; private set; }
        public SfDataGrid DetailGrid { get; private set; }
        
        public GridNavigator GridNavigator { get; private set; }

        protected VistasAdministrador VistasAdministrador => vistasController == null ? null : vistasController.Manager;

        /// <summary>
        /// Compatibilidad para código derivado que aún consulta GridViewManager.
        /// En código nuevo, usar VistasAdministrador.
        /// </summary>
        protected VistasAdministrador GridViewManager => VistasAdministrador;

        protected virtual string MasterDetailKeyField
        {
            get { return "CIDDOCUMENTO"; }
        }

        // ─── Constructor ──────────────────────────────────────────────────────
        public GenesysGridForm()
            : this(null)
        {
        }

        protected GenesysGridForm(GenesysGridFormOptions options)
        {
            this.options = options;
            filterPersistence = new GenesysGridFilterPersistence();
            Initialize();
        }

        /// <summary>
        /// Construye la estructura visual base del formulario y aplica la configuración recibida
        /// por constructor. La persistencia de filtros se restaura antes del primer pintado para
        /// evitar que los filtros se rellenen visualmente después de mostrar el formulario.
        /// </summary>
        protected virtual void Initialize()
        {
            KeyPreview = true;

            GenesysFormVisual.Apply(this);

            SuspendLayout();

            BuildPanels();
            BuildToolbar();
            ConfigureDefaultToolbar();

            BuildMessages();
            BuildFilters();

            ApplyOptions();

            // El combo y controles internos de filtros deben existir antes de aplicar persistencia.
            Filters.PerformLayout();
            RestoreFiltersState();

            BuildGrid();
            BuildDetailGrid();
            BuildGridNavigator();
            BuildVistas();
            AddDetailGridToWorkspace();

            AddMainPanelsToForm();


            ResumeLayout(true);
            PerformLayout();

            Filters.SearchCompleted -= Filters_SearchCompleted;
            Filters.SearchCompleted += Filters_SearchCompleted;
        }

        /// <summary>
        /// Agrega los paneles principales respetando el orden de Dock.
        /// </summary>
        private void AddMainPanelsToForm()
        {
            Controls.Add(GridWorkspacePanel);
            Controls.Add(InfoPanel);
            Controls.Add(ViewInfoPanel);
            Controls.Add(FiltersPanel);
            Controls.Add(MessagesPanel);
            Controls.Add(ButtonsPanel);
        }

        /// <summary>
        /// Aplica la configuración declarada por el formulario derivado mediante el
        /// constructor base(GenesysGridFormOptions).
        /// </summary>
        private void ApplyOptions()
        {
            if (options == null)
                return;

            if (!string.IsNullOrWhiteSpace(options.Title))
                Text = options.Title;

            if (Filters != null)
            {
                if (!string.IsNullOrWhiteSpace(options.FechaTitle))
                    Filters.SetFechaTitle(options.FechaTitle);

                if (!string.IsNullOrWhiteSpace(options.LookupTitle))
                    Filters.SetLookupTitle(options.LookupTitle);

                if (!string.IsNullOrWhiteSpace(options.ComboTitle))
                    Filters.SetComboTitle(options.ComboTitle);

                if (!string.IsNullOrWhiteSpace(options.LookupProvider))
                    Filters.SetLookupProvider(options.LookupProvider);

                if (!string.IsNullOrWhiteSpace(options.StoredProcedureName))
                    Filters.StoredProcedureName = options.StoredProcedureName;

                if (!string.IsNullOrWhiteSpace(options.TipoDeAccion))
                    Filters.TipoDeAccion = options.TipoDeAccion;

                if (!string.IsNullOrWhiteSpace(options.LookupParameterName))
                    Filters.LookupParameterName = options.LookupParameterName;

                if (!string.IsNullOrWhiteSpace(options.ComboParameterName))
                    Filters.ComboParameterName = options.ComboParameterName;

                if (options.ComboItems != null)
                    Filters.SetComboItems(options.ComboItems);
            }

            if (options.ToolbarButtons != null)
            {
                foreach (GenesysGridToolbarButtonOptions button in options.ToolbarButtons)
                {
                    if (button == null || button.OnClick == null)
                        continue;

                    AddToolbarButton(
                        button.Tipo,
                        button.Texto,
                        button.Tooltip,
                        delegate { button.OnClick(this); });
                }
            }
        }

        private void BuildPanels()
        {
            ButtonsPanel = GenesysPanelFactory.Create(
                "ButtonsPanel",
                DockStyle.Top,
                height: ButtonsPanelHeight,
                backColor: Color.White);

            FiltersPanel = GenesysPanelFactory.Create(
                "FiltersPanel",
                DockStyle.Top,
                height: FiltersPanelHeight,
                backColor: Color.WhiteSmoke);

            MessagesPanel = GenesysPanelFactory.Create(
                "MessagesPanel",
                DockStyle.Top,
                height: MessagesPanelHeight,
                backColor: Color.White);

            InfoPanel = GenesysPanelFactory.Create(
                "InfoPanel",
                DockStyle.Bottom,
                height: InfoPanelHeight,
                backColor: Color.AliceBlue);

            ViewInfoPanel = GenesysPanelFactory.Create(
                "ViewInfoPanel",
                DockStyle.Top,
                height: ViewInfoPanelHeight,
                backColor: Color.White);

            GridPanel = GenesysPanelFactory.Create(
                "GridPanel",
                DockStyle.Fill,
                backColor: Color.White);

            DetailGridPanel = GenesysPanelFactory.Create(
                "DetailGridPanel",
                DockStyle.Fill,
                backColor: Color.White);
            DetailGridPanel.Visible = false;

            GridWorkspacePanel = new TableLayoutPanel
            {
                Name = "GridWorkspacePanel",
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                BackColor = Color.White,
                Padding = new Padding(0),
                Margin = new Padding(0),
                TabStop = false
            };

            GridWorkspacePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            GridWorkspacePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));
            GridWorkspacePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            GridWorkspacePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));

            ButtonsPanel.TabStop = false;
            FiltersPanel.TabStop = false;
            MessagesPanel.TabStop = false;
            InfoPanel.TabStop = false;
            ViewInfoPanel.TabStop = false;
            GridPanel.TabStop = false;
            DetailGridPanel.TabStop = false;

            GenesysControlVisual.EnableDoubleBuffer(ButtonsPanel);
            GenesysControlVisual.EnableDoubleBuffer(FiltersPanel);
            GenesysControlVisual.EnableDoubleBuffer(MessagesPanel);
            GenesysControlVisual.EnableDoubleBuffer(InfoPanel);
            GenesysControlVisual.EnableDoubleBuffer(ViewInfoPanel);
            GenesysControlVisual.EnableDoubleBuffer(GridPanel);
            GenesysControlVisual.EnableDoubleBuffer(DetailGridPanel);
            GenesysControlVisual.EnableDoubleBuffer(GridWorkspacePanel);
        }

        private void BuildGridNavigator()
        {
            GridNavigator = new GridNavigator
            {
                Dock = DockStyle.Fill,
                Alignment = NavigatorAlignment.Right
            }
            .Attach(Grid)
            .EnableMoveFirstAfterBind()
            .EnableSelectionStatus();

            InfoPanel.Controls.Add(GridNavigator);
        }

        private void BuildToolbar()
        {
            ToolbarHostPanel = new Panel
            {
                Name = "ToolbarHostPanel",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                TabStop = false
            };

            ToolbarConfigPanel = new Panel
            {
                Name = "ToolbarConfigPanel",
                Dock = DockStyle.Right,
                Width = ToolbarConfigPanelWidth,
                BackColor = Color.White,
                TabStop = false
            };

            Toolbar = new GenesysToolbar
            {
                Dock = DockStyle.Fill,
                TabStop = false
            };

            ToolbarHostPanel.Controls.Add(Toolbar);

            ButtonsPanel.Controls.Add(ToolbarHostPanel);
            ButtonsPanel.Controls.Add(ToolbarConfigPanel);
        }

        private void BuildMessages()
        {
            Messages = new GenesysMessages
            {
                Dock = DockStyle.Fill,
                TabStop = false
            };

            MessagesPanel.Controls.Add(Messages);
        }

        private void BuildFilters()
        {
            Filters = new GenesysGridFiltersPanel
            {
                Dock = DockStyle.Fill,
                PersistenceKey = GetType().FullName,
                TabStop = true
            };

            FiltersPanel.Controls.Add(Filters);
        }

        private void BuildGrid()
        {
            Grid = new SfDataGrid
            {
                Name = "Grid",
                Dock = DockStyle.Fill,
                TabStop = true
            };

            ConfigureGridFeatures();

            Grid.CellDoubleClick += Grid_CellDoubleClick;
            Grid.SelectionChanged += Grid_SelectionChanged;

            GridPanel.Controls.Add(Grid);

            exportService = new GenesysGridExportService(this, Grid);
        }

        private void BuildDetailGrid()
        {
            DetailGrid = new SfDataGrid
            {
                Name = "DetailGrid",
                Dock = DockStyle.Fill,
                TabStop = true
            };

            ConfigureReadOnlyGridFeatures(DetailGrid);

            DetailGridPanel.Controls.Add(DetailGrid);
        }

        private void AddDetailGridToWorkspace()
        {
            if (GridWorkspacePanel == null || DetailGridPanel == null)
                return;

            if (!GridWorkspacePanel.Controls.Contains(DetailGridPanel))
            {
                GridWorkspacePanel.Controls.Add(DetailGridPanel, 0, 1);
                GridWorkspacePanel.SetColumnSpan(DetailGridPanel, 1);
            }
        }

        private void Grid_CellDoubleClick(object sender, Syncfusion.WinForms.DataGrid.Events.CellClickEventArgs e)
        {
            if (e.DataRow?.RowData == null) return;

            Modificar();
        }

        /// <summary>
        /// Construye el módulo de vistas y conserva referencias públicas por compatibilidad.
        /// Toda la lógica propia de vistas queda delegada a VistasController.
        /// </summary>
        private void BuildVistas()
        {
            vistasController = new VistasController(
                this,
                Grid,
                Filters,
                ToolbarConfigPanel,
                GridWorkspacePanel,
                GridPanel,
                ViewInfoPanel,
                ViewDesignerWidth);

            vistasController.Initialize();

            ViewSelectorBar = vistasController.ViewSelectorBar;
            ViewDesignerHostPanel = vistasController.ViewDesignerHostPanel;
            ViewDesigner = vistasController.ViewDesigner;
        }

        // ─── Configuración del grid ───────────────────────────────────────────

        /// <summary>
        /// Configura las características base del grid.
        /// Las subclases pueden sobreescribir para ajustar comportamiento
        /// sin duplicar toda la configuración.
        /// </summary>
        protected virtual void ConfigureGridFeatures()
        {
            ConfigureReadOnlyGridFeatures(Grid);

            // Registro del renderer personalizado para el renglon de Summary
            Grid.CellRenderers.Remove("TableSummary");
            Grid.CellRenderers.Add("TableSummary", new GridSummaryCellRenderer());
        }

        private void ConfigureReadOnlyGridFeatures(SfDataGrid grid)
        {
            if (grid == null)
                return;

            grid.AllowFiltering = true;
            grid.AllowSorting = true;
            grid.AllowTriStateSorting = true;
            grid.AllowGrouping = true;
            grid.ShowGroupDropArea = true;
            grid.AllowDraggingColumns = true;
            grid.AllowResizingColumns = true;
            grid.AllowEditing = false;
            grid.AllowDeleting = false;

            grid.SelectionMode =
                Syncfusion.WinForms.DataGrid.Enums.GridSelectionMode.Extended;

            //grid.NavigationMode =
            //    Syncfusion.WinForms.DataGrid.Enums.NavigationMode.Row;
            grid.NavigationMode =
                Syncfusion.WinForms.DataGrid.Enums.NavigationMode.Cell;

            grid.AutoSizeColumnsMode =
                Syncfusion.WinForms.DataGrid.Enums.AutoSizeColumnsMode.AllCells;
        }

        // ─── Binding ──────────────────────────────────────────────────────────

        /// <summary>
        /// Enlaza el DataTable al grid aplicando, cuando exista, la vista activa persistida.
        /// La vista se pasa directamente al configurador para evitar el doble pintado
        /// Predeterminada → Vista activa.
        /// </summary>
        protected void BindGridDataTable(DataTable table)
        {
            BindGridDataTable(table, null);
        }

        protected void BindGridDataTable(DataTable table, DataTable detail)
        {
            if (table == null)
                return;

            SetRedraw(GridWorkspacePanel, false);
            SuspendLayout();
            GridWorkspacePanel?.SuspendLayout();
            GridPanel?.SuspendLayout();
            DetailGridPanel?.SuspendLayout();
            DetailGrid?.SuspendLayout();

            try
            {
                SetDetailMode(detail != null);

                if (vistasController != null)
                    vistasController.BindGrid(table);
                else
                    GridConfigurator.BindDataTable(Grid, table);

                GridNavigator?.NotifyDataBound();
                BindDetailGridDataTable(detail);
            }
            finally
            {
                DetailGrid?.ResumeLayout(true);
                DetailGridPanel?.ResumeLayout(true);
                GridPanel?.ResumeLayout(true);
                GridWorkspacePanel?.ResumeLayout(true);
                ResumeLayout(true);
                SetRedraw(GridWorkspacePanel, true);
            }
        }

        private static void SetRedraw(Control control, bool enabled)
        {
            if (control == null || control.IsDisposed || !control.IsHandleCreated)
                return;

            SendMessage(
                control.Handle,
                WmSetRedraw,
                enabled ? new IntPtr(1) : IntPtr.Zero,
                IntPtr.Zero);

            if (!enabled)
                return;

            control.Invalidate(true);
            control.Update();
        }

        private void BindDetailGridDataTable(DataTable table)
        {
            detailTable = table;
            detailView = null;

            if (DetailGrid == null)
                return;

            if (table == null)
            {
                DetailGrid.DataSource = null;
                return;
            }

            detailView = new DataView(table);
            ApplyDetailFilterFromSelectedMasterRow(showNoRowsWhenNoSelection: true);

            bool restoreVisible = DetailGrid.Visible;
            DetailGrid.Visible = false;

            try
            {
                // GridConfigurator necesita el DataTable para crear columnas y formatos.
                // Se oculta el grid durante este paso para evitar que se vea la tabla completa
                // antes de aplicar el filtro del detalle.
                GridConfigurator.BindDataTable(DetailGrid, table);
                DetailGrid.DataSource = detailView;

                // 20260609 JR Oculto las columnas en el grid Detalle
                GridConfigurator.ApplyDefaultHiddenColumns(DetailGrid, detailTable);
            }
            finally
            {
                DetailGrid.Visible = restoreVisible;
            }
        }

        private void SetDetailMode(bool enabled)
        {
            if (detailModeEnabled == enabled)
                return;

            detailModeEnabled = enabled;

            SuspendLayout();
            GridWorkspacePanel?.SuspendLayout();

            try
            {
                if (DetailGridPanel != null)
                    DetailGridPanel.Visible = enabled;

                if (GridWorkspacePanel != null && GridWorkspacePanel.RowStyles.Count > 1)
                {
                    // El formulario conserva su tamaño original.
                    // Cuando hay detalle, la segunda fila toma 200 px y la fila maestra
                    // reduce su altura automáticamente porque la primera fila sigue en 100%.
                    GridWorkspacePanel.RowStyles[1].Height = enabled ? DetailGridHeight : 0F;
                }
            }
            finally
            {
                GridWorkspacePanel?.ResumeLayout(true);
                ResumeLayout(true);
            }
        }

        private void Grid_SelectionChanged(object sender, Syncfusion.WinForms.DataGrid.Events.SelectionChangedEventArgs e)
        {
            ApplyDetailFilterFromSelectedMasterRow(showNoRowsWhenNoSelection: true);
        }

        private void ApplyDetailFilterFromSelectedMasterRow(bool showNoRowsWhenNoSelection)
        {
            if (!detailModeEnabled || detailView == null || detailTable == null)
                return;

            if (!HasMasterDetailKey(detailTable))
            {
                detailView.RowFilter = string.Empty;
                return;
            }

            System.Data.DataRow row = GetSelectedDataRow();

            if (row == null || !row.Table.Columns.Contains(MasterDetailKeyField) || row.IsNull(MasterDetailKeyField))
            {
                detailView.RowFilter = showNoRowsWhenNoSelection ? "1 = 0" : string.Empty;
                return;
            }

            detailView.RowFilter = BuildMasterDetailRowFilter(
                detailTable.Columns[MasterDetailKeyField],
                row[MasterDetailKeyField]);
        }

        private bool HasMasterDetailKey(DataTable table)
        {
            return table != null && table.Columns.Contains(MasterDetailKeyField);
        }

        private static string BuildMasterDetailRowFilter(DataColumn column, object value)
        {
            string columnName = "[" + column.ColumnName.Replace("]", "]]") + "]";

            if (value == null || value == DBNull.Value)
                return "1 = 0";

            Type dataType = Nullable.GetUnderlyingType(column.DataType) ?? column.DataType;

            if (dataType == typeof(string) || dataType == typeof(Guid))
                return columnName + " = '" + EscapeRowFilterValue(Convert.ToString(value)) + "'";

            if (dataType == typeof(DateTime))
                return columnName + " = #" + ((DateTime)value).ToString("MM/dd/yyyy HH:mm:ss") + "#";

            if (dataType == typeof(bool))
                return columnName + " = " + (((bool)value) ? "true" : "false");

            return columnName + " = " + Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string EscapeRowFilterValue(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "''");
        }


        // ─── Toolbar ──────────────────────────────────────────────────────────

        protected void AddToolbarButton( BotonTipo tipo, string texto, string tooltip, Action onClick)
        {
            Toolbar.AddBefore( BotonTipo.Refrescar.ToString(), tipo, texto, tooltip, onClick);
        }

        private void ConfigureDefaultToolbar()
        {
            Toolbar.Add(BotonTipo.Nuevo, "Nuevo", "Nuevo registro", Nuevo);
            Toolbar.Add(BotonTipo.Modificar, "Modificar", "Modificar registro", Modificar);
            Toolbar.Add(BotonTipo.Refrescar, "Refrescar", "Refrescar información", Refrescar);
            Toolbar.Add(BotonTipo.ExportarExcel, "Excel", "Exportar a Excel", ExportarExcel);
            Toolbar.Add(BotonTipo.ExportarPdf, "PDF", "Exportar a PDF", ExportarPdf);

            Toolbar.AddSeparator("sepCerrar");

            Toolbar.Add(
                BotonTipo.Cerrar,
                "Cerrar",
                "Cerrar formulario",
                new Padding(55, 0, 0, 0),
                CerrarFormulario);
        }

        // ─── Acciones virtuales ───────────────────────────────────────────────
        // Expuestas como eventos para que las subclases puedan suscribirse
        // en lugar de sobreescribir, evitando olvidar llamar a base.*().

        public event EventHandler NuevoRequested;
        public event EventHandler ModificarRequested;
        public event EventHandler RefrescarRequested;
        public event EventHandler ExportarExcelRequested;
        public event EventHandler ExportarPdfRequested;

        protected virtual void Nuevo()
        {
            var form = CreateNuevoForm();

            if (form == null)
            {
                NuevoRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            using (form)
            {
                form.ShowDialog(this);
            }

            Refrescar();
        }

        protected virtual Form CreateNuevoForm()
        {
            return null;
        }

        protected virtual void Modificar()
        {
            var row = GetSelectedDataRow();

            if (row == null)
            {
                MessageBox.Show( "Seleccione un registro para modificar.", "Modificar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var form = CreateModificarForm(row);

            if (form == null)
            {
                ModificarRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            using (form)
            {
                form.ShowDialog(this);
            }

            Refrescar();
        }

        protected virtual Form CreateModificarForm(System.Data.DataRow row)
        {
            return null;
        }

        protected System.Data.DataRow GetSelectedDataRow()
        {
            if (Grid?.SelectedItem is DataRowView rowView)  return rowView.Row;

            return null;
        }
        // 20260609 JR obtener el renglon seleccionado del detalle
        protected System.Data.DataRow GetSelectedDetailDataRow()
        {
            if (DetailGrid?.SelectedItem is DataRowView rowView)
                return rowView.Row;

            return null;
        }

        protected virtual void Refrescar()
        {
            if (Filters == null)
                return;

            UseWaitCursor = true;
            Cursor.Current = Cursors.WaitCursor;

            BeginInvoke(new Action(delegate
            {
                try
                {
                    Filters.RefreshSearch();
                    //RefrescarRequested?.Invoke(this, EventArgs.Empty);
                }
                catch
                {
                    UseWaitCursor = false;
                    Cursor.Current = Cursors.Default;
                    throw;
                }
            }));
        }

        protected virtual void ExportarExcel()
        {
            exportService?.ExportarExcel(
                GetDefaultExportFileName("xlsx"));

            ExportarExcelRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void ExportarPdf()
        {
            exportService?.ExportarPdf(
                GetDefaultExportFileName("pdf"));

            ExportarPdfRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual string GetDefaultExportFileName(string extension)
        {
            var title = string.IsNullOrWhiteSpace(Text)
                ? GetType().Name
                : Text;

            foreach (var invalidChar in Path.GetInvalidFileNameChars())
                title = title.Replace(invalidChar, '_');

            return $"{title}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";
        }


        // ─── Resultados de búsqueda / binding ──────────────────────────────

        public event EventHandler InitialLoadCompleted;

        private void Filters_SearchCompleted(object sender, GenesysGridFilterResult e)
        {
            pendingFilterResult = e;

            if (filterResultBindScheduled)
                return;

            filterResultBindScheduled = true;

            BeginInvoke(new Action(delegate
            {
                filterResultBindScheduled = false;

                GenesysGridFilterResult result = pendingFilterResult;
                pendingFilterResult = null;

                ProcessFilterResult(result);
            }));
        }

        private void ProcessFilterResult(GenesysGridFilterResult result)
        {
            try
            {
                if (result == null)
                    return;

                if (result.HasDataSet && result.DataSet.Tables.Count > 0)
                {
                    DataTable detail = result.DataSet.Tables.Count > 1
                        ? result.DataSet.Tables[1]
                        : null;

                    BindGridDataTable(result.DataSet.Tables[0], detail);
                    return;
                }

                if (result.HasTable)
                {
                    BindGridDataTable(result.Table);
                    return;
                }

                // Resultado vacío: notificar en lugar de fallar silenciosamente
                //Messages?.ShowInfo("La búsqueda no devolvió resultados.");
            }
            finally
            {
                UseWaitCursor = false;
                Cursor.Current = Cursors.Default;

                RaiseInitialLoadCompleted();
            }
        }

        private void RaiseInitialLoadCompleted()
        {
            if (initialLoadCompletedRaised)
                return;

            initialLoadCompletedRaised = true;

            InitialLoadCompleted?.Invoke(this, EventArgs.Empty);
        }


        // ─── Ciclo de vida del formulario ─────────────────────────────────────

        private void CerrarFormulario() => Close();

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (initialLoadStarted)
                return;

            initialLoadStarted = true;

            BeginInvoke(new Action(delegate
            {
                if (IsDisposed)
                    return;

                UseWaitCursor = true;
                Cursor.Current = Cursors.WaitCursor;
                Filters.RefreshSearch();
            }));
        }

        /// <summary>
        /// Restaura una sola vez el estado persistido de los filtros superiores.
        /// Debe ejecutarse después de cargar los items del combo y antes del primer pintado.
        /// </summary>
        private void RestoreFiltersState()
        {
            if (filtersRestored)
                return;

            filtersRestored = true;

            GenesysGridFilterState state =
                filterPersistence.Load(Filters.PersistenceKey);

            if (state != null)
                Filters.ApplyState(state);
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {

            if (vistasController != null && !vistasController.ConfirmPendingChangesBeforeClose())
            {
                e.Cancel = true;
                return;
            }

            filterPersistence.Save(Filters.PersistenceKey, Filters.GetState());

            if (vistasController != null)
            {
                vistasController.PersistCurrentViewName();
                vistasController.Dispose();
                vistasController = null;
            }

            base.OnFormClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (GridNavigator != null && Grid != null && Grid.ContainsFocus)
            {
                if (keyData == Keys.Home)
                {
                    GridNavigator.MoveFirst();
                    return true;
                }

                if (keyData == Keys.End)
                {
                    GridNavigator.MoveLast();
                    return true;
                }

                if (keyData == (Keys.Control | Keys.Left))
                {
                    GridNavigator.MovePrevious();
                    return true;
                }

                if (keyData == (Keys.Control | Keys.Right))
                {
                    GridNavigator.MoveNext();
                    return true;
                }
            }

            bool isTab = keyData == Keys.Tab;
            bool isShiftTab = keyData == (Keys.Shift | Keys.Tab);

            if (isTab || isShiftTab)
            {
                bool forward = isTab;

                if (Filters != null && Filters.ContainsFilterFocus())
                {
                    if (Filters.MoveFilterFocus(forward))
                        return true;

                    if (Grid != null)
                    {
                        Grid.Focus();
                        return true;
                    }
                }

                if (Grid != null && Grid.ContainsFocus)
                    return forward
                        ? Filters.FocusFirstFilter()
                        : Filters.FocusLastFilter();
            }

            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ─── Dispose ──────────────────────────────────────────────────────────

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (Grid != null)
                    {
                        Grid.CellDoubleClick -= Grid_CellDoubleClick;
                        Grid.SelectionChanged -= Grid_SelectionChanged;
                    }

                    if (Filters != null)
                        Filters.SearchCompleted -= Filters_SearchCompleted;

                    if (vistasController != null)
                    {
                        vistasController.Dispose();
                        vistasController = null;
                    }

                }

                disposed = true;
            }

            base.Dispose(disposing);
        }
    }
    
    #region **********   Clases Auxiliares  ***********************************************************
    public class GenesysGridFormOptions
    {
        public string Title { get; set; }

        public string FechaTitle { get; set; }
        public string LookupTitle { get; set; }
        public string ComboTitle { get; set; }

        public string LookupProvider { get; set; }

        public string StoredProcedureName { get; set; }
        public string TipoDeAccion { get; set; }

        public string LookupParameterName { get; set; }
        public string ComboParameterName { get; set; }

        public GenesysComboFilterItem[] ComboItems { get; set; }

        public GenesysGridToolbarButtonOptions[] ToolbarButtons { get; set; }

    }

    public class GenesysGridToolbarButtonOptions
    {
        public BotonTipo Tipo { get; set; }
        public string Texto { get; set; }
        public string Tooltip { get; set; }

        // Se usa Action<GenesysGridForm> para que la configuración pueda declarar
        // acciones contra la instancia real del formulario.
        public System.Action<GenesysGridForm> OnClick { get; set; }
    }

    #endregion
}