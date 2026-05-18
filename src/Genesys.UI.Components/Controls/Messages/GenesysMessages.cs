using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Genesys.UI.Components.Controls.Messages
{
    public partial class GenesysMessages : UserControl
    {
        private readonly List<string> listaErrores = new List<string>();

        private readonly List<string> listaIndicadores = new List<string>();

        private const int AnchoIndicador = 24;

        private int anchoMaxTexto = 0;

        private int altoTotalTexto = 0;

        private int alturaPorLinea = 0;

        private bool actualizandoLayout = false;

        public GenesysMessages()
        {
            TabStop = false;

            InitializeComponent();

            ConfigurarControles();

            Resize += MensajesErrores_Resize;
        }

        private void ConfigurarControles()
        {
            lblErroresIndicador.ForeColor = Color.Red;

            lblErroresIndicador.Location = new Point(0, 0);

            lblErroresIndicador.Width = AnchoIndicador;

            lblErroresIndicador.TextAlign = ContentAlignment.TopCenter;

            lblErrores.Location = new Point(AnchoIndicador, 0);

            lblErrores.TextAlign = ContentAlignment.TopLeft;

            lblErrores.AutoSize = false;

            AutoScroll = true;

            // IMPORTANTE:
            // Evita Font.GetHeight(Graphics)
            // que puede fallar durante creación temprana del control.

            alturaPorLinea =
                TextRenderer.MeasureText("A", lblErrores.Font).Height;
        }

        private void MensajesErrores_Resize(object sender, EventArgs e)
        {
            // IMPORTANTE:
            // Evita layouts durante creación temprana.

            if (!IsHandleCreated)
                return;

            if (actualizandoLayout)
                return;

            AplicarLayoutDinamico();
        }

        #region API Pública

        public void Agregar(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return;

            listaErrores.Add(error);

            listaIndicadores.Add("👉");

            Size size =
                TextRenderer.MeasureText(
                    error,
                    lblErrores.Font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.NoPadding);

            if (size.Width > anchoMaxTexto)
                anchoMaxTexto = size.Width;

            altoTotalTexto += alturaPorLinea;
        }

        public void Mostrar(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Limpiar();

                Agregar(error);
            }

            AplicarTexto();

            AplicarLayoutDinamico();

            lblErrores.ColorInicio = Color.White;

            lblErrores.ColorFin = Color.Red;
        }

        public void Limpiar()
        {
            listaErrores.Clear();

            listaIndicadores.Clear();

            lblErrores.Text = "";

            lblErroresIndicador.Text = "";

            anchoMaxTexto = 0;

            altoTotalTexto = 0;

            AutoScrollPosition = new Point(0, 0);

            AutoScrollMinSize = Size.Empty;

            lblErrores.ColorInicio = Color.White;

            lblErrores.ColorFin = Color.FromArgb(192, 255, 255);
        }

        public int Cuenta()
        {
            return listaErrores.Count;
        }

        #endregion

        #region Métodos Privados

        private void AplicarTexto()
        {
            lblErrores.Text =
                string.Join(Environment.NewLine, listaErrores);

            lblErroresIndicador.Text =
                string.Join(Environment.NewLine, listaIndicadores);
        }

        private void AplicarLayoutDinamico()
        {
            if (listaErrores.Count == 0)
                return;

            if (actualizandoLayout)
                return;

            if (ClientSize.Width <= 0)
                return;

            actualizandoLayout = true;

            try
            {
                int alturaMaxVisible = alturaPorLinea * 3;

                int altoFinal =
                    (altoTotalTexto < alturaMaxVisible)
                    ? alturaMaxVisible
                    : altoTotalTexto;

                if (altoFinal < 20)
                    altoFinal = 20;

                lblErrores.Height = altoFinal;

                lblErroresIndicador.Height = altoFinal;

                // IMPORTANTE:
                // NO cambiar Height del control aquí.
                // Evita recursive layout storms.

                lblErrores.Width =
                    ClientSize.Width - AnchoIndicador;

                if (lblErrores.Width < 50)
                    lblErrores.Width = 50;

                int anchoVisible =
                    ClientSize.Width - AnchoIndicador;

                int anchoFinal =
                    Math.Max(anchoVisible, anchoMaxTexto);

                AutoScrollMinSize =
                    new Size(
                        anchoFinal + AnchoIndicador,
                        altoTotalTexto);
            }
            finally
            {
                actualizandoLayout = false;
            }
        }

        #endregion
    }

    // =========================================================
    // GradientLabel
    // =========================================================

    public class GradientLabel : Label
    {
        public Color ColorInicio { get; set; } = Color.White;

        public Color ColorFin { get; set; } = Color.White;

        public LinearGradientMode GradientMode { get; set; }
            = LinearGradientMode.Horizontal;

        public GradientLabel()
        {
            DoubleBuffered = true;

            ResizeRedraw = true;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // IMPORTANTE:
            // Evita errores GDI cuando el control
            // todavía no tiene tamaño válido.

            if (ClientRectangle.Width <= 0 ||
                ClientRectangle.Height <= 0)
                return;

            using (LinearGradientBrush brush =
                new LinearGradientBrush(
                    ClientRectangle,
                    ColorInicio,
                    ColorFin,
                    GradientMode))
            {
                ColorBlend blend = new ColorBlend();

                blend.Colors = new[]
                {
                    ColorInicio,
                    ColorInicio,
                    ColorFin
                };

                blend.Positions = new[]
                {
                    0.0f,
                    0.80f,
                    1.0f
                };

                brush.InterpolationColors = blend;

                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }
    }
}