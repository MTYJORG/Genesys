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
        
        public GridNavigator GridNavigator { get; private set; }

        protected VistasAdministrador VistasAdministrador => vistasController == null ? null : vistasController.Manager;

        /// <summary>
        /// Compatibilidad para código derivado que aún consulta GridViewManager.
        /// En código nuevo, usar VistasAdministrador.
        /// </summary>
        protected VistasAdministrador GridViewManager => VistasAdministrador;

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
            BuildGridNavigator();
            BuildVistas();

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

        /// <summary>
        /// Prepara el layout antes del primer Show. Puede llamarse después de construir
        /// el formulario y antes de entrar a Application.Run(form) cuando el host quiera
        /// forzar el primer layout.
        /// </summary>
        internal void PrepareForFirstDisplay()
        {
            SuspendLayout();

            try
            {
                PerformLayout();

                ButtonsPanel?.PerformLayout();
                MessagesPanel?.PerformLayout();
                FiltersPanel?.PerformLayout();
                ViewInfoPanel?.PerformLayout();
                GridWorkspacePanel?.PerformLayout();
                GridPanel?.PerformLayout();
                InfoPanel?.PerformLayout();
            }
            finally
            {
                ResumeLayout(true);
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

            GridWorkspacePanel = new TableLayoutPanel
            {
                Name = "GridWorkspacePanel",
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                Padding = new Padding(0),
                Margin = new Padding(0),
                TabStop = false
            };

            GridWorkspacePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            GridWorkspacePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0F));
            GridWorkspacePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            ButtonsPanel.TabStop = false;
            FiltersPanel.TabStop = false;
            MessagesPanel.TabStop = false;
            InfoPanel.TabStop = false;
            ViewInfoPanel.TabStop = false;
            GridPanel.TabStop = false;

            GenesysControlVisual.EnableDoubleBuffer(ButtonsPanel);
            GenesysControlVisual.EnableDoubleBuffer(FiltersPanel);
            GenesysControlVisual.EnableDoubleBuffer(MessagesPanel);
            GenesysControlVisual.EnableDoubleBuffer(InfoPanel);
            GenesysControlVisual.EnableDoubleBuffer(ViewInfoPanel);
            GenesysControlVisual.EnableDoubleBuffer(GridPanel);
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

            GridPanel.Controls.Add(Grid);

            exportService = new GenesysGridExportService(this, Grid);
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
            Grid.AllowFiltering = true;
            Grid.AllowSorting = true;
            Grid.AllowTriStateSorting = true;
            Grid.AllowGrouping = true;
            Grid.ShowGroupDropArea = true;
            Grid.AllowDraggingColumns = true;
            Grid.AllowResizingColumns = true;
            Grid.AllowEditing = false;
            Grid.AllowDeleting = false;

            Grid.SelectionMode =
                Syncfusion.WinForms.DataGrid.Enums.GridSelectionMode.Extended;

            //Grid.NavigationMode =
            //    Syncfusion.WinForms.DataGrid.Enums.NavigationMode.Row;
            Grid.NavigationMode =
                Syncfusion.WinForms.DataGrid.Enums.NavigationMode.Cell;

            Grid.AutoSizeColumnsMode =
                Syncfusion.WinForms.DataGrid.Enums.AutoSizeColumnsMode.AllCells;

            // Registro del renderer personalizado para el renglon de Summary
            Grid.CellRenderers.Remove("TableSummary");
            Grid.CellRenderers.Add("TableSummary", new GridSummaryCellRenderer());
        }

        // ─── Binding ──────────────────────────────────────────────────────────

        /// <summary>
        /// Enlaza el DataTable al grid aplicando, cuando exista, la vista activa persistida.
        /// La vista se pasa directamente al configurador para evitar el doble pintado
        /// Predeterminada → Vista activa.
        /// </summary>
        protected void BindGridDataTable(DataTable table)
        {
            if (table == null)
                return;

            if (vistasController != null)
                vistasController.BindGrid(table);
            else
                GridConfigurator.BindDataTable(Grid, table);

            GridNavigator?.NotifyDataBound();
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
                    RefrescarRequested?.Invoke(this, EventArgs.Empty);
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

                if (result == null)
                {
                    return;
                }

                try
                {
                    if (result.HasTable)
                    {
                        BindGridDataTable(result.Table);
                        return;
                    }

                    if (result.HasDataSet && result.DataSet.Tables.Count > 0)
                    {
                        BindGridDataTable(result.DataSet.Tables[0]);
                        return;
                    }

                    // Resultado vacío: notificar en lugar de fallar silenciosamente
                    //Messages?.ShowInfo("La búsqueda no devolvió resultados.");
                }
                finally
                {
                    UseWaitCursor = false;
                    Cursor.Current = Cursors.Default;
                }
            }));
        }


        // ─── Ciclo de vida del formulario ─────────────────────────────────────

        private void CerrarFormulario() => Close();

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (initialLoadStarted)
                return;

            initialLoadStarted = true;

            BeginInvoke(new Action(StartInitialLoadAfterFirstPaint));
        }

        /// <summary>
        /// Ejecuta la búsqueda inicial después del primer ciclo visual del formulario.
        /// Los filtros persistidos ya fueron restaurados durante Initialize.
        /// </summary>
        private void StartInitialLoadAfterFirstPaint()
        {
            if (IsDisposed)
                return;

            // Deja el primer layout/pintado del formulario
            PerformLayout();

            ButtonsPanel?.PerformLayout();
            MessagesPanel?.PerformLayout();
            FiltersPanel?.PerformLayout();
            ViewInfoPanel?.PerformLayout();
            GridWorkspacePanel?.PerformLayout();
            GridPanel?.PerformLayout();
            InfoPanel?.PerformLayout();

            Invalidate(true);
            Update();

            BeginInvoke(new Action(() =>
            {
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
                        Grid.CellDoubleClick -= Grid_CellDoubleClick;

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