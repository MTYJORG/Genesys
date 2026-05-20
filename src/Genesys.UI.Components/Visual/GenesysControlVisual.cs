using System.Reflection;
using System.Windows.Forms;

namespace Genesys.UI.Components.Visual
{
    public static class GenesysControlVisual
    {
        public static void EnableDoubleBuffer(Control control)
        {
            if (control == null)
                return;

            typeof(Control)
                .GetProperty(
                    "DoubleBuffered",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic)
                ?.SetValue(control, true, null);
        }
    }
}