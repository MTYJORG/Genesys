// ==============================
// FILE: Controls/Filters/FilterControlBase.cs
// ==============================

using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.Filters
{
    public class FilterControlBase : Panel
    {
        public FilterControlBase()
        {
            Width = 190;

            Height = 50;

            Margin = new Padding(5);
        }
    }
}