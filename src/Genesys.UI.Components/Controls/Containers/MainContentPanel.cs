// ==============================
// FILE: Controls/Containers/MainContentPanel.cs
// ==============================

using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.Containers
{
    public class MainContentPanel : Panel
    {
        public MainContentPanel()
        {
            Dock = DockStyle.Fill;

            Padding = new Padding(10);
        }
    }
}