using Genesys.UI.Components.Controls.Filters;
using Genesys.UI.Components.Controls.Messages;
using Genesys.UI.Components.Controls.Toolbar;
using Syncfusion.WinForms.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;
using Syncfusion.WinForms.DataGrid;

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

        public GenesysToolbar Toolbar { get; private set; }
        public GenesysMessages Messages { get; private set; }
        public GenesysGridFiltersPanel Filters { get; private set; }

        public SfDataGrid Grid { get; private set; }

        public GenesysGridForm()
        {
            SuspendLayout();

            KeyPreview = true;

            filterPersistence = new GenesysGridFilterPersistence();

            GenesysFormVisual.Apply(this);

            ButtonsPanel = GenesysPanelFactory.Create(
                "ButtonsPanel",
                DockStyle.Top,
                height: 49,
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

            Toolbar = new GenesysToolbar
            {
                Dock = DockStyle.Fill
            };

            Messages = new GenesysMessages
            {
                Dock = DockStyle.Fill
            };

            Filters = new GenesysGridFiltersPanel
            {
                Dock = DockStyle.Fill,
                PersistenceKey = GetType().FullName
            };

            // Panel para el Grid
            Grid = new SfDataGrid
            {
                Dock = DockStyle.Fill
            };
            GridPanel.Controls.Add(Grid);

            ButtonsPanel.Controls.Add(Toolbar);
            MessagesPanel.Controls.Add(Messages);
            FiltersPanel.Controls.Add(Filters);

            Controls.Add(GridPanel);
            Controls.Add(InfoPanel);
            Controls.Add(FiltersPanel);
            Controls.Add(MessagesPanel);
            Controls.Add(ButtonsPanel);

            ConfigureDefaultToolbar();

            ResumeLayout(false);

            Filters.SearchCompleted += Filters_SearchCompleted;
        }

        protected void AddToolbarButton(
            BotonTipo tipo,
            string texto,
            string tooltip,
            Action onClick)
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

            if (e.HasDataSet)
            {
                if (e.DataSet.Tables.Count > 0)
                {
                    Grid.DataSource = e.DataSet.Tables[0];
                }
            }
        }
    }
}