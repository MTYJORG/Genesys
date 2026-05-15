using Syncfusion.Windows.Forms.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Action = System.Action;

namespace Genesys.UI.Components.Controls.Toolbar
{
    [ToolboxItem(false)]
    public class GenesysToolbar : UserControl
    {
        private readonly ToolStripEx toolStrip;
        private readonly Dictionary<string, ToolStripButton> botones;

        public bool HasItems => toolStrip.Items.Count > 0;

        public GenesysToolbar()
        {
            botones = new Dictionary<string, ToolStripButton>();

            toolStrip = new ToolStripEx
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ForeColor = Color.MidnightBlue,
                ImageScalingSize = new Size(28,28),
                LauncherStyle = LauncherStyle.Office2007, 
                LayoutStyle = ToolStripLayoutStyle.Flow,
                ShowCaption = false,
                ShowItemToolTips = true,
                VisualStyle = ToolStripExStyle.Metro,
                Office12Mode = false,
                OfficeColorScheme = ToolStripEx.ColorScheme.Managed,
                ThemeName = "Metro",
                Padding = new Padding(0, 0, 1, 0),

                ThemeStyle =
                {
                    DropDownStyle = { HoverItemBackColor = Color.FromArgb(230, 230, 230) },
                    HoverItemBackColor = Color.FromArgb(218, 218, 218)
                }
            };

            // Pinta una linea superior
            toolStrip.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.LightGray, 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, toolStrip.Width, 0);
                }
            };

            Controls.Add(toolStrip);
        }

        #region API PUBLICA

        // 👉 SIN padding
        public void Add(BotonTipo tipo, string texto, string tooltip, Action onClick)
        {
            Add(tipo, texto, tooltip, Padding.Empty, onClick);
        }

        // 👉 CON padding
        public void Add(BotonTipo tipo, string texto, string tooltip, Padding textPadding, Action onClick)
        {
            var info = BotonesCatalogo.Obtener(tipo);
            var btn = CrearBoton( tipo.ToString(), texto, info.Icono, tooltip, textPadding, onClick );

            AplicarReglasFramework(tipo, btn);

            toolStrip.Items.Add(btn);
            botones.Add(btn.Name, btn);
        }

        public void AddSeparator()
        {
            var sep = new ToolStripSeparator
            {
                AutoSize = false,
                Height = toolStrip.Height
            };

            toolStrip.Items.Add(sep);
        }

        public void SetVisible(string id, bool visible)
        {
            if (botones.TryGetValue(id, out var btn))
                btn.Visible = visible;
        }

        public void Clear()
        {
            toolStrip.Items.Clear();
            botones.Clear();
        }

        #endregion

        #region REGLAS FRAMEWORK

        private void AplicarReglasFramework(BotonTipo tipo, ToolStripButton btn)
        {
            if (tipo == BotonTipo.Cerrar)
            {
                // 🔥 regla automática
                btn.Padding = new Padding(80, 0, 0, 0);
                btn.Margin = new Padding(10, 0, 3, 0);
            }
        }

        #endregion

        #region INTERNOS

        private ToolStripButton CrearBoton( string id, string texto, Image icono, string tooltip, Padding textPadding, Action onClick)
        {
            var btn = new ToolStripButton
            {
                Name = id,
                Text = texto,
                Image = icono,
                ToolTipText = string.IsNullOrWhiteSpace(tooltip) ? texto : tooltip,
                TextImageRelation = TextImageRelation.ImageAboveText,
                DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                TextAlign = ContentAlignment.BottomCenter,
                ImageAlign = ContentAlignment.TopCenter,
                Padding = textPadding,
                //Margin = new Padding(0, 0, 0, 0)
            };

            btn.Click += (s, e) => onClick?.Invoke();

            return btn;
        }

        //protected override void OnPaint(PaintEventArgs e)
        //{
        //    base.OnPaint(e);

        //    using (var pen = new Pen(Color.LightGray, 1))
        //    {
        //        var rect = this.ClientRectangle;
        //        rect.Width -= 1;
        //        rect.Height -= 1;

        //        e.Graphics.DrawRectangle(pen, rect);
        //    }
        //}

        #endregion
    }
}