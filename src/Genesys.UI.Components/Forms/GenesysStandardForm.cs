using Genesys.UI.Components.Controls.Messages;
using Genesys.UI.Components.Controls.Toolbar;
using Syncfusion.WinForms.Controls;
using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Forms
{
    public class GenesysStandardForm : SfForm
    {
        public Panel ButtonsPanel { get; private set; }
        public Panel MessagesPanel { get; private set; }

        public GenesysToolbar Toolbar { get; private set; }
        public GenesysMessages Messages { get; private set; }

        public GenesysStandardForm()
        {
            SuspendLayout();

            GenesysFormVisual.Apply(this);

            ButtonsPanel = GenesysPanelFactory.Create(
                "ButtonsPanel",
                DockStyle.Top,
                height: 49,
                backColor: Color.White);

            MessagesPanel = GenesysPanelFactory.Create(
                "MessagesPanel",
                DockStyle.Top,
                height: 49,
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

            Controls.Add(MessagesPanel);
            Controls.Add(ButtonsPanel);

            ConfigureDefaultToolbar();

            ResumeLayout(false);
        }

        private void ConfigureDefaultToolbar()
        {
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