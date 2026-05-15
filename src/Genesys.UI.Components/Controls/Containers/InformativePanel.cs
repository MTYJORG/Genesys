// ==============================
// FILE: Controls/Containers/InformativePanel.cs
// ==============================

using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.Containers
{
    public class InformativePanel : Panel
    {
        public InformativePanel()
        {
            Dock = DockStyle.Bottom;

            Height = 32;

            Padding = new Padding(10, 5, 10, 5);

            BorderStyle = BorderStyle.FixedSingle;
        }
    }
}