using Genesys.UI.Components.Controls.Messages;
using Genesys.UI.Components.Controls.Toolbar;
using Syncfusion.WinForms.Controls;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Genesys.UI.Components.Forms
{
    internal sealed class GenesysFormShell
    {
        private static readonly Font _fontUI = new Font("Segoe UI", 9);
        private static readonly Color _cFore = Color.Black;
        private static readonly Color _cHoverBack = Color.FromArgb(204, 231, 242);
        private static readonly Color _cHoverFore = Color.Black;
        private static readonly Color _borderColor = Color.FromArgb(16, 110, 190);
        private static readonly Pen _penInactiveBorder = new Pen(Color.LightGray, 2);
        private static Icon _iconForma;

        public Panel ToolbarPanel { get; private set; }
        public Panel MessagesPanel { get; private set; }
        public Panel BodyPanel { get; private set; }

        public GenesysToolbar Toolbar { get; private set; }
        public GenesysMessages Messages { get; private set; }

        private readonly SfForm form;

        public GenesysFormShell(SfForm form)
        {
            this.form = form ?? throw new ArgumentNullException(nameof(form));

            ApplyVisualStyle();
            InitializeLayout();
            InitializeToolbar();
        }

        private static Icon CargarIcono()
        {
            var icon = GenesysUI.AppIcon ?? SystemIcons.Application;
            return new Icon(icon, 40, 40);
        }

        private void ApplyVisualStyle()
        {
            if (_iconForma == null)
                _iconForma = CargarIcono();

            form.Font = _fontUI;
            form.Icon = _iconForma;
            form.IconSize = new Size(40, 40);
            form.AllowRoundedCorners = true;
            form.StartPosition = FormStartPosition.CenterScreen;

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
        }

        private void InitializeLayout()
        {
            ToolbarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 49,
                BackColor = Color.White
            };

            MessagesPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 49,
                BackColor = Color.White
            };

            BodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            Messages = new GenesysMessages
            {
                Dock = DockStyle.Fill
            };

            MessagesPanel.Controls.Add(Messages);

            form.Controls.Add(BodyPanel);
            form.Controls.Add(MessagesPanel);
            form.Controls.Add(ToolbarPanel);
        }

        private void InitializeToolbar()
        {
            Toolbar = new GenesysToolbar
            {
                Dock = DockStyle.Fill
            };

            ToolbarPanel.Controls.Add(Toolbar);
        }

        public void AddCloseButton()
        {
            if (Toolbar.HasItems)
                Toolbar.AddSeparator();

            Toolbar.Add(
                BotonTipo.Cerrar,
                "Cerrar",
                "Cerrar el formulario",
                () => form.Close());
        }
    }
}