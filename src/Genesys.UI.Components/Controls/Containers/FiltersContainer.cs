// ==============================
// FILE: Controls/Containers/FiltersContainer.cs
// ==============================

using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.Containers
{
    public class FiltersContainer : Panel
    {
        private int nextLeft = 10;

        public FiltersContainer()
        {
            Dock = DockStyle.Top;

            Height = 80;

            Padding = new Padding(10);

            BorderStyle = BorderStyle.FixedSingle;
        }

        public void AddFilter(Control control)
        {
            control.Left = nextLeft;

            control.Top = 10;

            Controls.Add(control);

            nextLeft += control.Width + 10;
        }
    }
}