using Genesys.UI.Components.Controls.Messages;
using Genesys.UI.Components.Controls.Toolbar;
using Syncfusion.WinForms.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Forms
{
    public class GenesysGridForm : SfForm
    {
        public Panel ButtonsPanel { get; private set; }
        public Panel FiltersPanel { get; private set; }
        public Panel MessagesPanel { get; private set; }
        public Panel GridPanel { get; private set; }
        public Panel InfoPanel { get; private set; }

        public GenesysToolbar Toolbar { get; private set; }
        public GenesysMessages Messages { get; private set; }

        public GenesysGridForm()
        {
            SuspendLayout();

            GenesysFormVisual.Apply(this);

            ButtonsPanel = GenesysPanelFactory.Create(
                "ButtonsPanel",
                DockStyle.Top,
                height: 49,
                backColor: Color.White);

            FiltersPanel = GenesysPanelFactory.Create(
                "FiltersPanel",
                DockStyle.Top,
                height: 55,
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

            ButtonsPanel.Controls.Add(Toolbar);
            MessagesPanel.Controls.Add(Messages);

            Controls.Add(GridPanel);
            Controls.Add(InfoPanel);
            Controls.Add(FiltersPanel);
            Controls.Add(MessagesPanel);
            Controls.Add(ButtonsPanel);

            ConfigureDefaultToolbar();

            ResumeLayout(false);
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
                new Padding(80, 0, 0, 0),
                CerrarFormulario);
        }

        private void CerrarFormulario()
        {
            Close();
        }
    }
}