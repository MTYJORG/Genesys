using System.Windows.Forms;
using Syncfusion.WinForms.Controls;

namespace Genesys.UI.Components.Forms
{
    public class GenesysSimpleForm : SfForm
    {
        public Panel MainPanel { get; }

        public GenesysSimpleForm()
        {
            SuspendLayout();

            GenesysFormVisual.Apply(this);

            MainPanel = GenesysPanelFactory.Create(
                "MainPanel",
                DockStyle.Fill);

            Controls.Add(MainPanel);

            ResumeLayout(false);
        }
    }
}