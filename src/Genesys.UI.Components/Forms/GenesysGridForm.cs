using Genesys.UI.Components.Controls.Filters;
using Genesys.UI.Components.Controls.Messages;
using Genesys.UI.Components.Controls.Toolbar;
using Syncfusion.Windows.Forms.Tools;
using Syncfusion.WinForms.Controls;
using Syncfusion.WinForms.DataGrid;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Forms
{
    public class GenesysGridForm : SfForm
    {
        private readonly GenesysGridFilterPersistence filterPersistence;
        private bool filtersRestored;

        public Panel ButtonsPanel { get; private set; }
        public Panel FiltersPanel { get; private set; }
        public Panel MessagesPanel { get; private set; }
        public Panel GridPanel { get; private set; }
        public Panel InfoPanel { get; private set; }

        public Panel ToolbarHostPanel { get; private set; }
        public Panel ToolbarConfigPanel { get; private set; }

        public GenesysToolbar Toolbar { get; private set; }
        public GenesysMessages Messages { get; private set; }
        public GenesysGridFiltersPanel Filters { get; private set; }
        public SfDataGrid Grid { get; private set; }

        private ToolStripEx configToolStrip;
        private ToolStripButton btnConfig;

        public GenesysGridForm()
        {
            SuspendLayout();

            KeyPreview = true;
            filterPersistence = new GenesysGridFilterPersistence();

            GenesysFormVisual.Apply(this);

            BuildPanels();
            BuildToolbar();
            BuildMessages();
            BuildFilters();
            BuildGrid();

            Controls.Add(GridPanel);
            Controls.Add(InfoPanel);
            Controls.Add(FiltersPanel);
            Controls.Add(MessagesPanel);
            Controls.Add(ButtonsPanel);

            ConfigureDefaultToolbar();

            ResumeLayout(false);

            Filters.SearchCompleted += Filters_SearchCompleted;
        }

        private void BuildPanels()
        {
            ButtonsPanel = GenesysPanelFactory.Create(
                "ButtonsPanel",
                DockStyle.Top,
                height: 46,
                backColor: Color.White);

            FiltersPanel = GenesysPanelFactory.Create(
                "FiltersPanel",
                DockStyle.Top,
                height: 70,
                backColor: Color.WhiteSmoke);

            MessagesPanel = GenesysPanelFactory.Create(
                "MessagesPanel",
                DockStyle.Top,
                height: 49,
                backColor: Color.White);

            InfoPanel = GenesysPanelFactory.Create(
                "InfoPanel",
                DockStyle.Bottom,
                height: 35,
                backColor: Color.AliceBlue);

            GridPanel = GenesysPanelFactory.Create(
                "GridPanel",
                DockStyle.Fill,
                backColor: Color.White);

            ButtonsPanel.TabStop = false;
            FiltersPanel.TabStop = false;
            MessagesPanel.TabStop = false;
            InfoPanel.TabStop = false;
            GridPanel.TabStop = false;
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
                Width = 44,
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

            configToolStrip.Paint += delegate (object sender, PaintEventArgs e)
            {
                using (var pen = new Pen(Color.LightGray, 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, configToolStrip.Width, 0);
                }
            };

            btnConfig = new ToolStripButton
            {
                Name = "btnConfig",
                Text = "⋮",
                ToolTipText = "Configuración",
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                AutoSize = false,
                Width = 40,
                Height = 42,
                Font = new Font("Segoe UI Symbol", 13F),
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnConfig.Click += delegate
            {
                ShowGridConfigMenu();
            };

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
                Dock = DockStyle.Fill,
                TabStop = true
            };

            ConfigureGridFeatures();

            GridPanel.Controls.Add(Grid);
        }

        protected void AddToolbarButton(
            BotonTipo tipo,
            string texto,
            string tooltip,
            System.Action onClick)
        {
            Toolbar.AddBefore(
                "sepCerrar",
                tipo,
                texto,
                tooltip,
                onClick);
        }

        protected virtual void Nuevo()
        {
        }

        protected virtual void Modificar()
        {
        }

        protected virtual void Refrescar()
        {
        }

        protected virtual void ExportarExcel()
        {
        }

        protected virtual void ExportarPdf()
        {
        }

        protected virtual void ShowGridConfigMenu()
        {
            MessageBox.Show(
                "Configuración del grid",
                "",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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

        private void CerrarFormulario()
        {
            Close();
        }

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

            GenesysGridFilterState state =
                filterPersistence.Load(Filters.PersistenceKey);

            if (state != null)
                Filters.ApplyState(state);

            Filters.ExecuteSearch();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            filterPersistence.Save(
                Filters.PersistenceKey,
                Filters.GetState());

            base.OnFormClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
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
                {
                    if (forward)
                        return Filters.FocusFirstFilter();

                    return Filters.FocusLastFilter();
                }
            }

            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Filters_SearchCompleted(object sender, GenesysGridFilterResult e)
        {
            if (e.HasTable)
            {
                Grid.DataSource = e.Table;
                return;
            }

            if (e.HasDataSet && e.DataSet.Tables.Count > 0)
            {
                Grid.DataSource = e.DataSet.Tables[0];
            }
        }

        private void ConfigureGridFeatures()
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
        }
    }
}