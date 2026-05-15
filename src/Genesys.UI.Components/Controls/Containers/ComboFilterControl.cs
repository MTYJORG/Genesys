// ==============================
// FILE: Controls/Filters/ComboFilterControl.cs
// ==============================

using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.Filters
{
    public class ComboFilterControl : Panel
    {
        private readonly Label lblTitle;

        private readonly ComboBox cboValue;

        public ComboFilterControl()
        {
            Width = 180;

            Height = 45;

            lblTitle = new Label
            {
                Text = "Filtro",

                Left = 0,

                Top = 0,

                Width = 170
            };

            cboValue = new ComboBox
            {
                Left = 0,

                Top = 20,

                Width = 170,

                DropDownStyle = ComboBoxStyle.DropDownList
            };

            Controls.Add(lblTitle);

            Controls.Add(cboValue);
        }
    }
}