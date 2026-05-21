using Genesys.UI.Components.Controls.GridViews;
using Genesys.UI.Components.Controls.Messages;
using Genesys.UI.Components.Controls.Toolbar;
using Genesys.UI.Components.Controls.Filters;
using Genesys.UI.Components.Visual;
using Syncfusion.WinForms.DataGridConverter;
using Syncfusion.Windows.Forms.Tools;
using Syncfusion.WinForms.Controls;
using Syncfusion.WinForms.DataGrid;
using Syncfusion.XlsIO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Data;
using System.IO;
using System;
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
        private const int ToolbarConfigPanelWidth = 44;
        private const int ConfigButtonWidth = 40;
        private const int ConfigButtonHeight = 42;
        private const float ConfigButtonFontSize = 13F;
        private const float ViewDesignerWidth = 320F;

        // ─── Campos ───────────────────────────────────────────────────────────
        private readonly GenesysGridFilterPersistence filterPersistence;
        private readonly Dictionary<string, string> gridNumericFormats;
        private GenesysGridViewManager gridViewManager;
        private bool filtersRestored;
        private ToolStripEx configToolStrip;
        private ToolStripButton btnConfig;
        private Font configButtonFont;
        private bool disposed;

        // ─── Propiedades públicas ─────────────────────────────────────────────
        public Panel ButtonsPanel { get; private set; }
        public Panel FiltersPanel { get; private set; }
        public Panel MessagesPanel { get; private set; }
        public Panel GridPanel { get; private set; }
        public TableLayoutPanel GridWorkspacePanel { get; private set; }
        public Panel InfoPanel { get; private set; }
        public Panel ToolbarHostPanel { get; private set; }
        public Panel ToolbarConfigPanel { get; private set; }
        public Panel ViewDesignerHostPanel { get; private set; }
        public GenesysGridViewDesignerPanel ViewDesigner { get; private set; }
        public GenesysToolbar Toolbar { get; private set; }
        public GenesysMessages Messages { get; private set; }
        public GenesysGridFiltersPanel Filters { get; private set; }
        public SfDataGrid Grid { get; private set; }
        
        public GenesysGridNavigator GridNavigator { get; private set; }

        protected GenesysGridViewManager GridViewManager => gridViewManager;

        // ─── Constructor ──────────────────────────────────────────────────────
        public GenesysGridForm()
        {
            filterPersistence = new GenesysGridFilterPersistence();
            gridNumericFormats = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Initialize();
        }

        // ─── Inicialización ───────────────────────────────────────────────────

        /// <summary>
        /// Inicializa todos los controles visuales del formulario.
        /// Las subclases pueden sobreescribir para ajustar el orden o
        /// inyectar dependencias antes de que los Build* se ejecuten.
        /// </summary>
        protected virtual void Initialize()
        {
            KeyPreview = true;

            GenesysFormVisual.Apply(this);

            SuspendLayout();

            BuildPanels();
            BuildToolbar();
            BuildMessages();
            BuildFilters();
            BuildGrid();
            BuildGridNavigator();
            BuildViewDesigner();
            BuildGridViewManager();

            Controls.Add(GridWorkspacePanel);
            Controls.Add(InfoPanel);
            Controls.Add(FiltersPanel);
            Controls.Add(MessagesPanel);
            Controls.Add(ButtonsPanel);

            ConfigureDefaultToolbar();

            ResumeLayout(true);
            PerformLayout();

            Filters.SearchCompleted += Filters_SearchCompleted;
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
            GridPanel.TabStop = false;

            GenesysControlVisual.EnableDoubleBuffer(ButtonsPanel);
            GenesysControlVisual.EnableDoubleBuffer(FiltersPanel);
            GenesysControlVisual.EnableDoubleBuffer(MessagesPanel);
            GenesysControlVisual.EnableDoubleBuffer(InfoPanel);
            GenesysControlVisual.EnableDoubleBuffer(GridPanel);
        }

        private void BuildGridNavigator()
        {
            GridNavigator = new GenesysGridNavigator
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
            BuildConfigToolStrip();

            ButtonsPanel.Controls.Add(ToolbarHostPanel);
            ButtonsPanel.Controls.Add(ToolbarConfigPanel);
        }

        private void BuildConfigToolStrip()
        {
            configToolStrip = new ToolStripEx
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ForeColor = Color.MidnightBlue,
                ImageScalingSize = new Size(28, 28),
                LauncherStyle = LauncherStyle.Office2007,
                LayoutStyle = ToolStripLayoutStyle.Flow,
                ShowCaption = false,
                ShowItemToolTips = true,
                VisualStyle = ToolStripExStyle.Metro,
                Office12Mode = false,
                OfficeColorScheme = ToolStripEx.ColorScheme.Managed,
                ThemeName = "Metro",
                Padding = new Padding(0, 0, 1, 0),
                TabStop = false
            };

            configToolStrip.ThemeStyle.DropDownStyle.HoverItemBackColor =
                Color.FromArgb(230, 230, 230);

            configToolStrip.ThemeStyle.HoverItemBackColor =
                Color.FromArgb(218, 218, 218);

            // La línea superior se delega a la capa visual
            configToolStrip.Paint += ConfigToolStrip_Paint;

            configButtonFont = new Font("Segoe UI Symbol", ConfigButtonFontSize);

            btnConfig = new ToolStripButton
            {
                Name = "btnConfig",
                Text = "⋮",
                ToolTipText = "Vistas del grid",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                AutoSize = false,
                Width = ConfigButtonWidth,
                Height = ConfigButtonHeight,
                Font = configButtonFont,
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnConfig.Click += BtnConfig_Click;

            configToolStrip.Items.Add(btnConfig);
            ToolbarConfigPanel.Controls.Add(configToolStrip);
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
        }
        
        private void Grid_CellDoubleClick(object sender, Syncfusion.WinForms.DataGrid.Events.CellClickEventArgs e)
        {
            if (e.DataRow?.RowData == null) return;

            Modificar();
        }

        private void BuildViewDesigner()
        {
            ViewDesignerHostPanel = new Panel
            {
                Name = "ViewDesignerHostPanel",
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0),
                TabStop = false
            };

            ViewDesigner = new GenesysGridViewDesignerPanel
            {
                Dock = DockStyle.Fill
            };

            ViewDesigner.CloseRequested += ViewDesigner_CloseRequested;

            ViewDesignerHostPanel.Controls.Add(ViewDesigner);

            GridWorkspacePanel.Controls.Add(GridPanel, 0, 0);
            GridWorkspacePanel.Controls.Add(ViewDesignerHostPanel, 1, 0);
        }

        private void BuildGridViewManager()
        {
            gridViewManager = new GenesysGridViewManager(
                this,
                Grid,
                btnConfig,
                BuildGridViewKey());

            gridViewManager.Initialize();
            gridViewManager.DesignerRequested += GridViewManager_DesignerRequested;

            if (ViewDesigner != null)
                ViewDesigner.Attach(gridViewManager);
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

            Grid.NavigationMode =
                Syncfusion.WinForms.DataGrid.Enums.NavigationMode.Row;

            Grid.AutoSizeColumnsMode =
                Syncfusion.WinForms.DataGrid.Enums.AutoSizeColumnsMode.AllCells;

            // Registro del renderer personalizado para el renglon de Summary
            Grid.CellRenderers.Remove("TableSummary");
            Grid.CellRenderers.Add("TableSummary", new AlignedSummaryRenderer());
        }

        // ─── Formatos numéricos ───────────────────────────────────────────────

        protected void SetGridNumericFormat(string columnName, string format)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return;

            gridNumericFormats[columnName] = format;

            gridViewManager?.SetNumericFormat(columnName, format);
        }

        protected void SetGridNumericFormats(IDictionary<string, string> formats)
        {
            if (formats == null)
                return;

            foreach (var item in formats)
                SetGridNumericFormat(item.Key, item.Value);
        }

        protected virtual IDictionary<string, string> GetGridNumericFormats()
            => gridNumericFormats;

        // ─── Binding ──────────────────────────────────────────────────────────

        protected void BindGridDataTable(DataTable table)
        {
            if (table == null)
                return;

            GenesysGridConfigurator.BindDataTable(Grid, table, GetGridNumericFormats());

            gridViewManager?.ReapplyCurrentView();
            ViewDesigner?.ReloadColumns();

            GridNavigator?.NotifyDataBound();
        }

        // ─── Toolbar ──────────────────────────────────────────────────────────

        protected void AddToolbarButton(
            BotonTipo tipo,
            string texto,
            string tooltip,
            Action onClick)
        {
            Toolbar.AddBefore("ExportarExcel", tipo, texto, tooltip, onClick);
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
            if (Filters == null) return;

            Filters.ExecuteSearch();

            RefrescarRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void ExportarExcel()
        {
            if (Grid == null || Grid.View == null)
                return;

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Exportar a Excel";
                dialog.Filter = "Archivo Excel (*.xlsx)|*.xlsx";
                dialog.FileName = GetDefaultExportFileName("xlsx");

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                var options = new ExcelExportingOptions
                {
                    ExportMode = ExportMode.Text,
                    ExcelVersion = ExcelVersion.Excel2013,
                    ExportStackedHeaders = true
                };

                var excelEngine = Grid.ExportToExcel(Grid.View, options);
                var workbook = excelEngine.Excel.Workbooks[0];

                workbook.SaveAs(dialog.FileName);
                excelEngine.Dispose();
            }

            ExportarExcelRequested?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void ExportarPdf()
        {
            if (Grid == null || Grid.View == null)
                return;

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Exportar a PDF";
                dialog.Filter = "Archivo PDF (*.pdf)|*.pdf";
                dialog.FileName = GetDefaultExportFileName("pdf");

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                var options = new PdfExportingOptions
                {
                    ExportStackedHeaders = true
                };

                var document = Grid.ExportToPdf(options);

                document.Save(dialog.FileName);
                document.Close(true);
            }

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


        // ─── View Designer ────────────────────────────────────────────────────

        protected virtual string BuildGridViewKey()
            => GetType().FullName + "." + Grid.Name;

        protected virtual void ToggleViewDesigner()
        {
            if (ViewDesignerHostPanel == null || ViewDesigner == null || gridViewManager == null)
                return;

            if (ViewDesignerHostPanel.Visible)
                HideViewDesigner();
            else
                ShowViewDesigner();
        }

        protected virtual void ShowViewDesigner()
        {
            if (ViewDesignerHostPanel == null || ViewDesigner == null || gridViewManager == null)
                return;

            ViewDesigner.Attach(gridViewManager);
            ViewDesignerHostPanel.Visible = true;
            GridWorkspacePanel.ColumnStyles[1].Width = ViewDesignerWidth;
            GridWorkspacePanel.PerformLayout();
        }

        protected virtual void HideViewDesigner()
        {
            if (ViewDesignerHostPanel == null || GridWorkspacePanel == null)
                return;

            ViewDesignerHostPanel.Visible = false;
            GridWorkspacePanel.ColumnStyles[1].Width = 0F;
            GridWorkspacePanel.PerformLayout();
        }

        protected virtual void ShowGridConfigMenu()
        {
            if (gridViewManager != null)
            {
                gridViewManager.ShowMenu();
                return;
            }

            MessageBox.Show(
                "Configuración del grid",
                "",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // ─── Manejadores de eventos con nombre ────────────────────────────────

        private void BtnConfig_Click(object sender, EventArgs e)
            => ShowGridConfigMenu();

        private void ViewDesigner_CloseRequested(object sender, EventArgs e)
            => HideViewDesigner();

        private void GridViewManager_DesignerRequested(object sender, EventArgs e)
            => ToggleViewDesigner();

        private void ConfigToolStrip_Paint(object sender, PaintEventArgs e)
        {
            // La línea superior se pinta aquí; podría moverse a GenesysControlVisual
            // si más controles necesitan el mismo estilo.
            using (var pen = new Pen(Color.LightGray, 1))
            {
                e.Graphics.DrawLine(pen, 0, 0, configToolStrip.Width, 0);
            }
        }

        private void Filters_SearchCompleted(object sender, GenesysGridFilterResult e)
        {
            if (e.HasTable)
            {
                BindGridDataTable(e.Table);
                return;
            }

            if (e.HasDataSet && e.DataSet.Tables.Count > 0)
            {
                BindGridDataTable(e.DataSet.Tables[0]);
                return;
            }

            // Resultado vacío: notificar en lugar de fallar silenciosamente
            //Messages?.ShowInfo("La búsqueda no devolvió resultados.");
        }

        // ─── Ciclo de vida del formulario ─────────────────────────────────────

        private void CerrarFormulario() => Close();

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RestoreFiltersAndSearch();
        }

        private void RestoreFiltersAndSearch()
        {
            if (filtersRestored)
                return;

            filtersRestored = true;

            var state = filterPersistence.Load(Filters.PersistenceKey);

            if (state != null)
                Filters.ApplyState(state);

            Filters.ExecuteSearch();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (gridViewManager != null && !gridViewManager.ConfirmPendingChangesBeforeClose(this))
            {
                e.Cancel = true;
                return;
            }

            filterPersistence.Save(Filters.PersistenceKey, Filters.GetState());

            if (gridViewManager != null)
            {
                gridViewManager.PersistCurrentViewName();
                gridViewManager.Dispose();
                gridViewManager = null;
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

                    if (configToolStrip != null)
                        configToolStrip.Paint -= ConfigToolStrip_Paint;

                    if (btnConfig != null)
                        btnConfig.Click -= BtnConfig_Click;

                    if (Filters != null)
                        Filters.SearchCompleted -= Filters_SearchCompleted;

                    if (ViewDesigner != null)
                        ViewDesigner.CloseRequested -= ViewDesigner_CloseRequested;

                    if (gridViewManager != null)
                    {
                        gridViewManager.DesignerRequested -= GridViewManager_DesignerRequested;
                        gridViewManager.Dispose();
                        gridViewManager = null;
                    }

                    if (configButtonFont != null)
                        configButtonFont.Dispose();
                }

                disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}