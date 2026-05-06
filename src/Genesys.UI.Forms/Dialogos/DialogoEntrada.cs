using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ComponentesComunes.Dialogos
{
    public partial class DialogoEntrada : Form
    {
        private List<string> _nombresExistentes;

        public string ValorIngresado => txtEntrada.Text;

        public DialogoEntrada()
        {
            InitializeComponent();
        }

        public void Configurar(string titulo, string indicacion, string valorPredeterminado = "", List<string> nombresExistentes = null)
        {
            this.Text = titulo;
            lblIndicacion.Text = indicacion;
            txtEntrada.Text = valorPredeterminado ?? "";
            _nombresExistentes = nombresExistentes ?? new List<string>();

            if (!string.IsNullOrEmpty(valorPredeterminado))
            {
                txtEntrada.SelectAll();
                VerificarDuplicado(valorPredeterminado);
            }

            txtEntrada.TextChanged += (s, e) => VerificarDuplicado(txtEntrada.Text);
            txtEntrada.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnAceptar.PerformClick(); };
            btnAceptar.Click += BtnAceptar_Click;
        }

        private void VerificarDuplicado(string nombre)
        {
            if (!string.IsNullOrWhiteSpace(nombre) && _nombresExistentes.Contains(nombre.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                lblAdvertencia.Text = $"⚠️ Ya existe una vista con el nombre '{nombre.Trim()}'.\nAl continuar, se sobrescribirá la vista existente.";
                lblAdvertencia.Visible = true;
                btnAceptar.BackColor = System.Drawing.Color.DarkOrange;
                btnAceptar.Text = "Sobrescribir";
            }
            else
            {
                lblAdvertencia.Visible = false;
                btnAceptar.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
                btnAceptar.Text = "Aceptar";
            }
        }

        private void BtnAceptar_Click(object sender, EventArgs e)
        {
            string nombre = txtEntrada.Text.Trim();

            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("Debe ingresar un nombre.", "Nombre Requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                txtEntrada.Focus();
                return;
            }

            if (_nombresExistentes.Contains(nombre, StringComparer.OrdinalIgnoreCase))
            {
                var resultado = MessageBox.Show($"Ya existe una vista con el nombre '{nombre}'.\n\n¿Desea sobrescribirla?",
                    "Vista Existente", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (resultado != DialogResult.Yes)
                {
                    this.DialogResult = DialogResult.None;
                    txtEntrada.SelectAll();
                    txtEntrada.Focus();
                    return;
                }
            }

            this.DialogResult = DialogResult.OK;
        }

        public static string Mostrar(string indicacion, string titulo, string valorPredeterminado = "", List<string> nombresExistentes = null)
        {
            using (var dialogo = new DialogoEntrada())
            {
                dialogo.Configurar(titulo, indicacion, valorPredeterminado, nombresExistentes);
                return dialogo.ShowDialog() == DialogResult.OK ? dialogo.ValorIngresado?.Trim() : null;
            }
        }
    }
}