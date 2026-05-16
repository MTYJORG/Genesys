using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Forms
{
    public static class GenesysPanelFactory
    {
        public static Panel Create(
            string name,
            DockStyle dock,
            int height = 0,
            Color? backColor = null)
        {
            var panel = new Panel
            {
                Name = name,
                Dock = dock,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = backColor ?? Color.White
            };

            if (height > 0)
                panel.Height = height;

            return panel;
        }
    }
}