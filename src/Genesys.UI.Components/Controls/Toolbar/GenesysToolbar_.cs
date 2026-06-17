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

        private int maxButtonWidth = 0;

        private const int ButtonMinWidth = 65;
        private const int ButtonHeight = 45;
        private const int ClosePaddingLeft = 55;

        public bool HasItems
        {
            get { return toolStrip.Items.Count > 0; }
        }

        public GenesysToolbar()
        {
            botones = new Dictionary<string, ToolStripButton>();

            toolStrip = new ToolStripEx
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                ForeColor = Color.MidnightBlue,
                ImageScalingSize = new Size(20, 20),
                LauncherStyle = LauncherStyle.Office2007,
                LayoutStyle = ToolStripLayoutStyle.Flow,
                ShowCaption = false,
                ShowItemToolTips = true,
                VisualStyle = ToolStripExStyle.Metro,
                Office12Mode = false,
                OfficeColorScheme = ToolStripEx.ColorScheme.Managed,
                ThemeName = "Metro",
                Padding = new Padding(0, 1, 1, 0),

                ThemeStyle =
                {
                    DropDownStyle = { HoverItemBackColor = Color.FromArgb(230, 230, 230) },
                    HoverItemBackColor = Color.FromArgb(218, 218, 218)
                }
            };

            toolStrip.Paint += delegate (object s, PaintEventArgs e)
            {
                using (var pen = new Pen(Color.LightGray, 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, toolStrip.Width, 0);
                }
            };

            Controls.Add(toolStrip);

            TabStop = false;
            toolStrip.TabStop = false;
        }

        #region API PUBLICA

        public void Add(BotonTipo tipo, string texto, string tooltip, Action onClick)
        {
            Add(tipo, texto, tooltip, Padding.Empty, onClick);
        }

        public void Add(BotonTipo tipo, string texto, string tooltip, Padding textPadding, Action onClick)
        {
            var info = BotonesCatalogo.Obtener(tipo);

            var btn = CrearBoton(
                tipo.ToString(),
                texto,
                info.Icono,
                tooltip,
                textPadding,
                onClick);

            toolStrip.Items.Add(btn);
            botones.Add(btn.Name, btn);

            UpdateButtonsWidth();
        }

        public void AddBefore(
            string beforeId,
            BotonTipo tipo,
            string texto,
            string tooltip,
            Action onClick)
        {
            AddBefore(beforeId, tipo, texto, tooltip, Padding.Empty, onClick);
        }

        public void AddBefore(
            string beforeId,
            BotonTipo tipo,
            string texto,
            string tooltip,
            Padding textPadding,
            Action onClick)
        {
            var info = BotonesCatalogo.Obtener(tipo);

            var btn = CrearBoton(
                tipo.ToString(),
                texto,
                info.Icono,
                tooltip,
                textPadding,
                onClick);

            int index = toolStrip.Items.IndexOfKey(beforeId);

            if (index >= 0)
                toolStrip.Items.Insert(index, btn);
            else
                toolStrip.Items.Add(btn);

            botones.Add(btn.Name, btn);

            UpdateButtonsWidth();
        }

        public void AddSeparator(string name = null)
        {
            var sep = new ToolStripSeparator
            {
                Name = name,
                AutoSize = false,
                Height = toolStrip.Height
            };

            toolStrip.Items.Add(sep);
        }

        public void SetVisible(string id, bool visible)
        {
            ToolStripButton btn;

            if (botones.TryGetValue(id, out btn))
                btn.Visible = visible;
        }

        public void SetText(string id, string text)
        {
            ToolStripButton btn;

            if (botones.TryGetValue(id, out btn))
            {
                btn.Text = text;

                UpdateButtonsWidth();
            }
        }
        public void SetTooltip(string id, string tooltip)
        {
            ToolStripButton btn;

            if (botones.TryGetValue(id, out btn))
                btn.ToolTipText = tooltip;
        }
        public void SetEnabled(string id, bool enabled)
        {
            ToolStripButton btn;

            if (botones.TryGetValue(id, out btn))
                btn.Enabled = enabled;
        }
        public void Clear()
        {
            toolStrip.Items.Clear();
            botones.Clear();
            maxButtonWidth = 0;
        }

        #endregion

        #region INTERNOS

        private ToolStripButton CrearBoton(
            string id,
            string texto,
            Image icono,
            string tooltip,
            Padding textPadding,
            Action onClick)
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
                Padding = textPadding
            };

            btn.Click += delegate
            {
                if (onClick != null)
                    onClick.Invoke();
            };

            return btn;
        }

        private void UpdateButtonsWidth()
        {
            ////20260609 JR cambio a ancho minimo
            ////maxButtonWidth = 0;

            //foreach (ToolStripItem item in toolStrip.Items)
            //{
            //    var btn = item as ToolStripButton;

            //    if (btn == null)
            //        continue;

            //    if (btn.Name == BotonTipo.Cerrar.ToString())
            //        continue;

            //    int textWidth =
            //        TextRenderer.MeasureText(btn.Text, btn.Font).Width;

            //    int proposedWidth = textWidth + 10;

            //    if (proposedWidth > maxButtonWidth)
            //        maxButtonWidth = proposedWidth;
            //}

            //if (maxButtonWidth < ButtonMinWidth)
            //    maxButtonWidth = ButtonMinWidth;

            //foreach (ToolStripItem item in toolStrip.Items)
            //{
            //    var btn = item as ToolStripButton;

            //    if (btn == null)
            //        continue;

            //    btn.AutoSize = false;

            //    if (btn.Name == BotonTipo.Cerrar.ToString())
            //    {
            //        btn.Size = new Size(
            //            maxButtonWidth + ClosePaddingLeft,
            //            ButtonHeight);
            //    }
            //    else
            //    {
            //        btn.Size = new Size(
            //            maxButtonWidth,
            //            ButtonHeight);
            //    }
            //}
            foreach (ToolStripItem item in toolStrip.Items)
            {
                var btn = item as ToolStripButton;

                if (btn == null)
                    continue;

                int textWidth = TextRenderer.MeasureText(btn.Text, btn.Font).Width;
                int buttonWidth = Math.Max(ButtonMinWidth, textWidth + 10);

                btn.AutoSize = false;

                if (btn.Name == BotonTipo.Cerrar.ToString())
                {
                    btn.Size = new Size(
                        buttonWidth + ClosePaddingLeft,
                        ButtonHeight);
                }
                else
                {
                    btn.Size = new Size(
                        buttonWidth,
                        ButtonHeight);
                }
            }
        }

        #endregion
    }
}