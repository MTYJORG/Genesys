using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Genesys.UI.Forms.Dialogos
{
    public partial class DialogoSeleccion : Form
    {
        private List<string> _opciones;
        private string _titulo;

        public string OpcionSeleccionada => lstOpciones.SelectedItem?.ToString();

        public DialogoSeleccion(List<string> opciones, string titulo)
        {
            InitializeComponent();

            _opciones = opciones;
            _titulo = titulo;

            ConfigurarUI();
        }

        private void ConfigurarUI()
        {
            this.Text = _titulo;

            foreach (var opcion in _opciones)
                lstOpciones.Items.Add(opcion);

            if (lstOpciones.Items.Count > 0)
                lstOpciones.SelectedIndex = 0;

            lstOpciones.DoubleClick += (s, e) => btnAceptar.PerformClick();
        }

        public static string Mostrar(List<string> opciones, string titulo)
        {
            if (opciones == null || opciones.Count == 0)
                return null;

            using (var dialogo = new DialogoSeleccion(opciones, titulo))
            {
                return dialogo.ShowDialog() == DialogResult.OK ? dialogo.OpcionSeleccionada : null;
            }
        }
    }
}