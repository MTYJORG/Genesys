using Syncfusion.Windows.Forms.Tools;
using Syncfusion.WinForms.Controls;
using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Forms
{
    public static class GenesysFormVisual
    {
        private static readonly Font _fontUI = new Font("Segoe UI", 9);
        private static readonly Color _cFore = Color.Black;
        private static readonly Color _cHoverBack = Color.FromArgb(204, 231, 242);
        private static readonly Color _cHoverFore = Color.Black;
        private static readonly Color _borderColor = Color.FromArgb(16, 110, 190);
        private static readonly Pen _penInactiveBorder = new Pen(Color.LightGray, 2);
        private static Icon _iconForma;

        public static void Apply(SfForm form)
        {
            //form.StartPosition = FormStartPosition.CenterScreen;
            //form.Font = new Font("Segoe UI", 9F);
            //form.AutoScaleMode = AutoScaleMode.Font;
            //form.MinimumSize = new Size(800, 500);
            //form.Padding = new Padding(0);
            //form.BackColor = Color.White;

            if (_iconForma == null)
                _iconForma = CargarIcono();

            form.Font = _fontUI;
            form.Icon = _iconForma;
            form.IconSize = new Size(40, 40);
            form.AllowRoundedCorners = true;
            //this.ShowToolTip = false;

            form.MinimumSize = new Size(900, 500);
            
            var tb = form.Style.TitleBar;
            tb.Font = _fontUI;
            tb.ForeColor = _cFore;
            tb.Height = 30;
            tb.BackColor = Color.White;
            tb.TextHorizontalAlignment = HorizontalAlignment.Left;
            tb.MinimizeButtonForeColor = _cFore;
            tb.MinimizeButtonHoverBackColor = _cHoverBack;
            tb.MinimizeButtonHoverForeColor = _cHoverFore;
            tb.MaximizeButtonForeColor = _cFore;
            tb.MaximizeButtonHoverBackColor = _cHoverBack;
            tb.MaximizeButtonHoverForeColor = _cHoverFore;
            tb.CloseButtonForeColor = _cFore;
            tb.CloseButtonHoverBackColor = Color.Red;
            tb.CloseButtonHoverForeColor = _cHoverFore;

            form.Style.Border = new Pen(_borderColor, 2);
            form.Style.InactiveBorder = _penInactiveBorder;
            form.Style.ShadowOpacity = 255;
            form.Style.InactiveShadowOpacity = 100;

            form.StartPosition = FormStartPosition.CenterScreen;
        }

        private static Icon CargarIcono()
        {
            var icon = GenesysUI.AppIcon ?? SystemIcons.Application;
            return new Icon(icon, 40, 40);
        }
    }
}