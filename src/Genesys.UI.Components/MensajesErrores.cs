using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Syncfusion.Drawing;

namespace Juguera
{
    public partial class MensajesErrores : UserControl
    {
        private List<string> listaErrores = new List<string>();
        private List<string> listaIndicadores = new List<string>();

        private const int AnchoIndicador = 24;

        private int anchoMaxTexto = 0;
        private int altoTotalTexto = 0;
        private int alturaPorLinea = 0;

        private bool actualizandoLayout = false;

        // =========================
        // Constructor
        // =========================

        public MensajesErrores()
        {
            InitializeComponent();
            ConfigurarControles();

            this.Resize += MensajesErrores_Resize;
        }

        private void ConfigurarControles()
        {
            lblErroresIndicador.ForeColor = Color.Red;
            lblErroresIndicador.Location = new Point(0, 0);
            lblErroresIndicador.Width = AnchoIndicador;
            lblErroresIndicador.TextAlign = ContentAlignment.MiddleCenter;

            lblErrores.Location = new Point(AnchoIndicador, 0);
            lblErrores.TextAlign = ContentAlignment.MiddleLeft;
            lblErrores.AutoSize = false;
            lblErrores.AutoEllipsis = false;
            lblErrores.UseCompatibleTextRendering = false;

            this.AutoScroll = true;
            this.HorizontalScroll.Enabled = true;
            this.VerticalScroll.Enabled = true;

            // Calcular altura por línea
            alturaPorLinea = lblErrores.Font.Height;
        }

        private void MensajesErrores_Resize(object sender, EventArgs e)
        {
            // Evitar recursión
            if (actualizandoLayout) return;

            // En resize SOLO se aplica layout liviano (NO mide textos)
            AplicarLayoutDinamico();
        }

        // =========================
        // API Pública
        // =========================

        public void Agregar(string error)
        {
            if (string.IsNullOrWhiteSpace(error)) return;

            listaErrores.Add(error);
            listaIndicadores.Add("👉");

            // Medir el nuevo texto
            Size size = TextRenderer.MeasureText( error, lblErrores.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding );

            // Actualizar ancho máximo si el nuevo texto es más ancho
            if (size.Width > anchoMaxTexto)
                anchoMaxTexto = size.Width;

            // Acumular altura total
            altoTotalTexto += alturaPorLinea;
        }

        public void Mostrar(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Limpiar();
                Agregar(error);
            }

            // Actualizar interfaz
            AplicarTexto();
            AplicarLayoutDinamico();

            // rojo
            lblErrores.BackgroundColor = new BrushInfo(GradientStyle.Horizontal,new Color[] {Color.Red, Color.Transparent, Color.Transparent, Color.Transparent, Color.Transparent });
        }

        public void Limpiar()
        {
            listaErrores.Clear();
            listaIndicadores.Clear();

            lblErrores.Text = "";
            lblErroresIndicador.Text = "";

            anchoMaxTexto = 0;
            altoTotalTexto = 0;

            this.AutoScrollPosition = new Point(0, 0);
            this.AutoScrollMinSize = Size.Empty;

            // celeste
            lblErrores.BackgroundColor = new BrushInfo( GradientStyle.Horizontal, new Color[] { Color.FromArgb(192, 255, 255), Color.Transparent, Color.Transparent, Color.Transparent, Color.Transparent });
        }

        public int Cuenta()
        {
            return listaErrores.Count;
        }

        // =========================
        // Métodos Privados
        // =========================

        private void AplicarTexto()
        {
            lblErrores.Text = string.Join(Environment.NewLine, listaErrores);
            lblErroresIndicador.Text = string.Join(Environment.NewLine, listaIndicadores);
        }

        // Layout DINÁMICO (solo posición y scroll)
        private void AplicarLayoutDinamico()
        {
            if (listaErrores.Count == 0) return;

            // Evitar recursión
            if (actualizandoLayout) return;

            actualizandoLayout = true;

            try
            {
                int alturaMaxVisible = alturaPorLinea * 3;

                // Calcular alto final
                int altoFinal = (altoTotalTexto < alturaMaxVisible)  ? alturaMaxVisible : altoTotalTexto  ;
                if (altoFinal < 20) altoFinal = 20;

                // Aplicar alturas
                lblErrores.Height = altoFinal;
                lblErroresIndicador.Height = altoFinal;
                
                this.Height = alturaMaxVisible + 4;

                // El label ocupa TODO el ancho del control
                lblErrores.Width = this.ClientSize.Width;
                if (lblErrores.Width < 50)  lblErrores.Width = 50;

                // Scroll horizontal
                int anchoVisible = this.ClientSize.Width - AnchoIndicador;
                int anchoFinal = Math.Max(anchoVisible, anchoMaxTexto);

                // Clave para el scroll
                this.AutoScrollMinSize = new Size(anchoFinal + AnchoIndicador, altoTotalTexto);
            }
            finally
            {
                actualizandoLayout = false;
            }
        }
    }
}