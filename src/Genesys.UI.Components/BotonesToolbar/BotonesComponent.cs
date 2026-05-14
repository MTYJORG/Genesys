using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Genesys.UI.Components.Controls.Toolbar;

namespace Genesys.UI.Components
{
    [ToolboxItem(true)]
    [Description("Barra de botones estilo clásico Windows")]
    [DesignerCategory("Code")]
    public class BotonesComponent : UserControl
    {
        private ToolStrip toolStrip;
        private Label tituloLabel;
        private Panel lineaSeparadora;
        private ToolTip toolTip;

        private Dictionary<string, ToolStripButton> botonesDict;
        private Dictionary<string, Action> accionesDict;

        private ImageList imageList;

        private string titulo = "Catálogo de Empresas";
        private bool mostrarTitulo = true;

        #region PROPIEDADES

        [Category("Appearance")]
        [Description("Título del panel")]
        public string Titulo
        {
            get => titulo;
            set
            {
                titulo = value;
                tituloLabel.Text = value;
            }
        }

        [Category("Appearance")]
        [Description("Mostrar título")]
        [DefaultValue(true)]
        public bool MostrarTitulo
        {
            get => mostrarTitulo;
            set
            {
                mostrarTitulo = value;

                tituloLabel.Visible = value;
                lineaSeparadora.Visible = value;

                AjustarLayout();
            }
        }

        [Category("Images")]
        [Description("Lista de imágenes para botones")]
        public ImageList ImageList
        {
            get => imageList;
            set
            {
                imageList = value;

                if (imageList != null)
                {
                    foreach (var btn in botonesDict.Values)
                    {
                        if (btn.Tag is int index &&
                            index >= 0 &&
                            index < imageList.Images.Count)
                        {
                            btn.Image = imageList.Images[index];
                        }
                    }
                }
            }
        }

        #endregion

        public BotonesComponent()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            botonesDict = new Dictionary<string, ToolStripButton>();
            accionesDict = new Dictionary<string, Action>();

            toolTip = new ToolTip();

            // USERCONTROL
            this.BackColor = SystemColors.Control;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Height = 85;
            this.Dock = DockStyle.Top;

            // TITULO
            tituloLabel = new Label();
            tituloLabel.Text = titulo;
            tituloLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            tituloLabel.AutoSize = true;
            tituloLabel.Location = new Point(10, 8);

            // LINEA
            lineaSeparadora = new Panel();
            lineaSeparadora.Height = 1;
            lineaSeparadora.Dock = DockStyle.Top;
            lineaSeparadora.BackColor = Color.Silver;
            lineaSeparadora.Margin = new Padding(0);
            lineaSeparadora.Padding = new Padding(0);

            // TOOLSTRIP
            toolStrip = new ToolStrip();

            toolStrip.Dock = DockStyle.Bottom;

            toolStrip.Height = 55;

            toolStrip.BackColor = SystemColors.Control;

            toolStrip.GripStyle = ToolStripGripStyle.Hidden;

            toolStrip.RenderMode = ToolStripRenderMode.System;

            toolStrip.ImageScalingSize = new Size(48,48);

            toolStrip.Padding = new Padding(5, 2, 5, 2);

            // CONTROLES
            this.Controls.Add(toolStrip);
            this.Controls.Add(lineaSeparadora);
            this.Controls.Add(tituloLabel);

            AjustarLayout();
        }

        private void AjustarLayout()
        {
            if (mostrarTitulo)
            {
                tituloLabel.Visible = true;
                lineaSeparadora.Visible = true;

                tituloLabel.Location = new Point(10, 8);

                lineaSeparadora.Location = new Point(0, 28);
                lineaSeparadora.Width = this.Width;

                toolStrip.Location = new Point(0, 30);
                toolStrip.Height = 50;

                this.Height = 85;
            }
            else
            {
                tituloLabel.Visible = false;
                lineaSeparadora.Visible = false;

                toolStrip.Location = new Point(0, 0);
                toolStrip.Height = 55;

                this.Height = 60;
            }
        }

        #region BOTONES

        public void AgregarBoton( string id, string texto,int imagenIndex, Action onClick, string tooltip = "")
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID inválido");

            if (botonesDict.ContainsKey(id))
                throw new InvalidOperationException($"Ya existe un botón con ID '{id}'");

            ToolStripButton btn = new ToolStripButton();

            btn.Name = id;

            btn.Text = texto;

            btn.TextImageRelation = TextImageRelation.ImageAboveText;

            btn.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;

            btn.AutoSize = false;

            btn.Width = 75;

            btn.Height = 55;

            btn.Font = new Font("Microsoft Sans Serif", 8F);

            btn.TextAlign = ContentAlignment.BottomCenter;

            btn.ImageAlign = ContentAlignment.TopCenter;

            btn.Tag = imagenIndex;

            // IMAGEN
            if (imageList != null &&
                imagenIndex >= 0 &&
                imagenIndex < imageList.Images.Count)
            {
                btn.Image = imageList.Images[imagenIndex];
            }

            // CLICK
            btn.Click += (s, e) =>
            {
                onClick?.Invoke();
            };

            // TOOLTIP
            if (!string.IsNullOrWhiteSpace(tooltip))
            {
                btn.ToolTipText = tooltip;
            }

            // AGREGAR
            botonesDict.Add(id, btn);
            accionesDict.Add(id, onClick);

            toolStrip.Items.Add(btn);
        }

        public void AgregarBoton( BotonTipo tipo, Action onClick)
        {
            BotonInfo info = BotonesCatalogo.Obtener(tipo);

            ToolStripButton btn = new ToolStripButton();

            btn.Name = tipo.ToString();
            btn.Text = info.Texto;
            btn.ToolTipText = info.Tooltip;
            btn.Image = info.Icono;
            btn.TextImageRelation = TextImageRelation.ImageAboveText;
            btn.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            btn.AutoSize = false;
            btn.Width = 72;
            btn.Height = 50;
            btn.Font = new Font("Segoe UI", 8F);
            btn.TextAlign = ContentAlignment.BottomCenter;
            btn.ImageAlign = ContentAlignment.TopCenter;

            btn.Click += (s, e) =>
            {
                onClick?.Invoke();
            };

            toolStrip.Items.Add(btn);
        }

        public void AgregarSeparador()
        {
            ToolStripSeparator sep = new ToolStripSeparator();
            toolStrip.Items.Add(sep);
        }

        public void MostrarBoton(string id, bool visible)
        {
            if (botonesDict.TryGetValue(id, out ToolStripButton btn))
            {
                btn.Visible = visible;
            }
        }

        public void CambiarTexto(string id, string nuevoTexto)
        {
            if (botonesDict.TryGetValue(id, out ToolStripButton btn))
            {
                btn.Text = nuevoTexto;
            }
        }

        public void CambiarIcono(string id, int nuevoIndice)
        {
            if (botonesDict.TryGetValue(id, out ToolStripButton btn))
            {
                if (imageList != null &&
                    nuevoIndice >= 0 &&
                    nuevoIndice < imageList.Images.Count)
                {
                    btn.Image = imageList.Images[nuevoIndice];
                    btn.Tag = nuevoIndice;
                }
            }
        }

        public void EliminarBoton(string id)
        {
            if (botonesDict.TryGetValue(id, out ToolStripButton btn))
            {
                toolStrip.Items.Remove(btn);

                botonesDict.Remove(id);
                accionesDict.Remove(id);

                btn.Dispose();
            }
        }

        public void Limpiar()
        {
            toolStrip.Items.Clear();

            botonesDict.Clear();
            accionesDict.Clear();
        }

        public bool ExisteBoton(string id)
        {
            return botonesDict.ContainsKey(id);
        }

        public List<string> ObtenerIdsBotones()
        {
            return botonesDict.Keys.ToList();
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                toolTip?.Dispose();

                foreach (var btn in botonesDict.Values)
                {
                    btn.Dispose();
                }

                botonesDict.Clear();
                accionesDict.Clear();

                toolStrip?.Dispose();
                tituloLabel?.Dispose();
                lineaSeparadora?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}