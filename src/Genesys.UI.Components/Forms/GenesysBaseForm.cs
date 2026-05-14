using Genesys.UI.Components.Controls.Toolbar;
using Syncfusion.WinForms.Controls;
using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Forms
{
    public partial class GenesysBaseForm : SfForm
    {
        private static readonly Font _fontUI = new Font("Segoe UI", 9);
        private static readonly Color _cFore = Color.Black;
        private static readonly Color _cHoverBack = Color.FromArgb(204, 231, 242);
        private static readonly Color _cHoverFore = Color.Black;
        private static readonly Color _borderColor = Color.FromArgb(16, 110, 190);
        private static readonly Pen _penInactiveBorder = new Pen(Color.LightGray, 2);
        private static Icon _iconForma;

        protected Panel ToolbarPanel { get; private set; }
        protected Panel MessagesPanel { get; private set; }
        protected Panel MainContentPanel { get; private set; }

        protected GenesysToolbar Toolbar { get; private set; }

        private static Icon CargarIcono()
        {
            var icon = GenesysUI.AppIcon ?? SystemIcons.Application;
            return new Icon(icon, 40, 40);
        }

        public GenesysBaseForm()
        {
            InitializeComponent();

            // Presentación estandar de los formularios
            ApplyVisualStyle();

            // Define los paneles del formulario
            InitializeLayout();

            // Manejo de Botones
            InitializeToolbar();
            ConfigureToolbar();     // 👈 Agrega primero botones del hijo
            AddCloseButton();       // 👈 luego agrega el boton "cerrar"
        }
        private void ApplyVisualStyle()
        {
            if (_iconForma == null)
                _iconForma = CargarIcono();

            this.Font = _fontUI;
            this.Icon = _iconForma;
            this.IconSize = new Size(40,40);
            this.AllowRoundedCorners = true;
            //this.ShowToolTip = false;

            var tb = this.Style.TitleBar;
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

            this.Style.Border = new Pen(_borderColor, 2);
            this.Style.InactiveBorder = _penInactiveBorder;
            this.Style.ShadowOpacity = 255;
            this.Style.InactiveShadowOpacity = 100;
        }

        private void InitializeLayout()
        {
            // ToolbarPanel
            ToolbarPanel = new Panel  { Dock = DockStyle.Top, Height = 53, BackColor = Color.White };

            // MessagesPanel
            MessagesPanel = new Panel { Dock = DockStyle.Top,Height = 49, BackColor = Color.White };

            // MainContentPanel 
            MainContentPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            // Orden IMPORTANTE (WinForms respeta orden de agregado)
            this.Controls.Add(MainContentPanel);
            this.Controls.Add(MessagesPanel);
            this.Controls.Add(ToolbarPanel);
        }

        private void InitializeToolbar()
        {
            Toolbar = new GenesysToolbar
            {
                Dock = DockStyle.Fill
            };

            ToolbarPanel.Controls.Add(Toolbar);
        }

        private void AddCloseButton()
        {
            // separador solo si ya hay botones
            if (Toolbar.HasItems)
                Toolbar.AddSeparator();

            Toolbar.Add( BotonTipo.Cerrar,"Cerrar", "Cerrar el formulario", () => this.Close() );
        }

        protected virtual void ConfigureToolbar()
        {
            // el form hijo agrega botones aquí desde sus formularios, no quitar
        }
    }
}